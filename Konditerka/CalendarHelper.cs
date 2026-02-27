using Npgsql;
using System.Data;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Konditerka
{
    // Конвертер теперь живет здесь
    public class DateToDescriptionConverter : IValueConverter
    {
        public static CalendarHelper Helper { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime date && Helper != null)
            {
                return Helper.GetOrderSummaryForDate(date);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
    public class CalendarHelper
    {
        private class OrderInfo
        {
            public DateTime DateZak { get; set; }
            public string LastName { get; set; }
        }

        public class TimeSlot
        {
            public DateTime Time { get; set; }
            public string DisplayTime => Time.ToString("HH:mm");
            public string StatusColor { get; set; } // Цвет фона или текста
            public bool IsEnabled { get; set; }    // Можно ли нажать на этот слот
        }

        // Коллекция данных должна быть доступна на уровне экземпляра класса, а не статически
        private List<OrderInfo> _orderDates { get; set; } = new List<OrderInfo>();
        // Добавьте поле для кэша
        private Dictionary<DateTime, string> _cachedSummaries = new Dictionary<DateTime, string>();

        public void LoadCalendar()
        {
            string query = @"SELECT zakazy.date_ispoln, klients.lastname FROM zakazy
INNER JOIN klients ON zakazy.id_klient=klients.id_klient";
            try
            {
                using (NpgsqlConnection connection = Metods.Source.OpenConnection())
                {
                    //connection.Open();
                    NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);

                    // *** Заполнение коллекции данными ***
                    _orderDates.Clear();
                    foreach (DataRow row in dataTable.Rows)
                    {
                        if (row["date_ispoln"] is DateTime date && row["lastname"] is string lastName)
                        {
                            _orderDates.Add(new OrderInfo
                            {
                                DateZak = date, // Теперь сохраняем полное время из БД
                                //DateZak = date.Date, // Используем только дату, без времени
                                LastName = lastName
                            });
                        }
                    }
                    // После заполнения _orderDates, создаем кэш:
                    _cachedSummaries = _orderDates
                        .GroupBy(o => o.DateZak.Date)
                        .ToDictionary(
                            g => g.Key,
                            g => "Заказы:\n" + string.Join("\n", g.OrderBy(o => o.DateZak).Select(o => $"{o.DateZak:HH:mm} - {o.LastName}"))
                        );
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки (вместо Console.WriteLine или MessageBox)
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке данных: {ex.Message}");
                // Переброс исключения выше, чтобы UI мог его обработать
                throw new Exception("Не удалось загрузить данные календаря", ex);
            }
        }

        //метод отсечения занятых времен
        public List<DateTime> GetBookedTimesForDate(DateTime selectedDate)
        {
            // Фильтруем коллекцию _orderDates по дате и выбираем только время
            var bookedTimes = _orderDates
                .Where(o => o.DateZak.Date == selectedDate.Date) // Сравниваем только календарную дату
                .Select(o => o.DateZak)                         // Выбираем полное DateTime с точным временем
                .ToList();

            return bookedTimes;
        }

        // Метод генерации интервалов времени (каждые полчаса с 5:00 до 23:30)
        public void GenerateTimeIntervals(DateTime date, ListBox listBox, List<DateTime> bookedTimes)
        {
            List<TimeSlot> slots = new List<TimeSlot>();
            // Начало и конец рабочего дня
            DateTime currentTime = new DateTime(date.Year, date.Month, date.Day, 5, 0, 0);
            DateTime endTime = new DateTime(date.Year, date.Month, date.Day, 23, 30, 0);

            // Список всех занятых дат из базы
            var allOrders = _orderDates.Select(o => o.DateZak).ToList();

            while (currentTime <= endTime)
            {
                var slot = new TimeSlot { Time = currentTime, IsEnabled = true, StatusColor = "Transparent" };

                // ПРАВИЛО 1: Менее 48 часов от текущего момента (Блокировка прошлого и ближайшего будущего)
                if ((currentTime - DateTime.Now).TotalHours < 48)
                {
                    slot.StatusColor = "#FF4C4C";
                    slot.IsEnabled = false;
                }
                // ПРАВИЛО 3 (поднимаем выше): Слот занят ровно в это время
                else if (allOrders.Any(t => Math.Abs((t - currentTime).TotalMinutes) < 1))
                {
                    slot.StatusColor = "Yellow";
                    slot.IsEnabled = false;
                }
                // ПРАВИЛО 2: Проверка буферной зоны 6 часов (и до, и после заказа)
                else if (allOrders.Any(booked => Math.Abs((booked - currentTime).TotalHours) < 6))
                {
                    slot.StatusColor = "#FFC0CB";
                    slot.IsEnabled = false;
                }

                slots.Add(slot);
                currentTime = currentTime.AddMinutes(30);
            }
            listBox.ItemsSource = slots;
        }

        // Метод для ToolTip (используется конвертером)
        // Теперь этот метод работает мгновенно
        public string GetOrderSummaryForDate(DateTime date)
        {
            if (_cachedSummaries.TryGetValue(date.Date, out string summary))
            {
                return summary;
            }
            return null;
        }

    }
}
