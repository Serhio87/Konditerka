using MahApps.Metro.Controls;
using Npgsql;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using static Konditerka.Metods;

namespace Konditerka
{
    /// <summary>
    /// Логика взаимодействия для Zakupka.xaml
    /// </summary>
    public partial class Zakupka : MetroWindow
    {
        private List<string> ingr = new List<string>(); // Для хранения ингр
        DataTable dataTableZakupIngr = new DataTable();
        // Определяем событие
        public event Action DataUpdatedZakup;
        public event Action DataUpdatedPriceOfOne;

        public Zakupka()
        {
            InitializeComponent();
            LoadData();
            InitIngr();
            TextIngr.Focus();
        }

        private void LoadData()
        {
            try
            {
                using (NpgsqlConnection connection = Metods.Source.OpenConnection())
                {
                    //connection.Open();

                    NpgsqlDataAdapter dataAdapterZakup = new NpgsqlDataAdapter(Metods.queryZakup, connection);

                    DataTable dataTableZakup = new DataTable();
                    dataAdapterZakup.Fill(dataTableZakup);

                    DGIngr.ItemsSource = dataTableZakup.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных для 'Состав заказа': {ex.Message}",
                                "Ошибка подключения",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                Console.WriteLine("{ ex.Message}");
            }
        }

        private void SearchIngr()
        {
            using (NpgsqlConnection connection = Metods.Source.OpenConnection())
            {
                string ingr = TextIngr.Text;

                NpgsqlCommand command = new NpgsqlCommand(Metods.queryZakupIngr, connection);
                command.Parameters.AddWithValue("nameingrid", "%" + ingr + "%");

                NpgsqlDataAdapter dataAdapterZakupIngr = new NpgsqlDataAdapter(command);

                dataTableZakupIngr.Clear();
                dataAdapterZakupIngr.Fill(dataTableZakupIngr);
                //DGIngr.DataContext = dataTableZakupIngr;
            }
        }

        private void InitIngr()
        {
            SearchIngr();

            ingr = dataTableZakupIngr.AsEnumerable()
        .Select(row => row.Field<string>("nameingrid"))
        .Where(name => name != null)
        .Distinct()
        .ToList();
        }

        private void IngrList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Проверяем, что выбран именно объект (строка)
            if (IngrList.SelectedItem != null)
            {
                // Получаем строку из выбранного элемента
                string selectedName = IngrList.SelectedItem.ToString();

                // Отключаем событие, чтобы изменение текста не вызывало повторный поиск в БД
                TextIngr.TextChanged -= TextIngr_TextChanged;

                // Подставляем ОРИГИНАЛЬНОЕ название из списка
                TextIngr.Text = selectedName;

                // Ставим курсор в конец текста
                TextIngr.CaretIndex = TextIngr.Text.Length;

                // Скрываем список
                IngrList.Visibility = Visibility.Collapsed;

                // Включаем событие обратно
                TextIngr.TextChanged += TextIngr_TextChanged;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (NpgsqlConnection connection = Metods.Source.OpenConnection())
                using (var transaction = connection.BeginTransaction()) // Начинаем транзакцию
                {
                    //connection.Open();

                    if (EdIzm.SelectedItem == null)
                    {
                        MessageBox.Show("Выберите единицы измерения!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    // Получаем строку из ComboBox 
                    string edizmerString = (EdIzm.SelectedItem as ComboBoxItem)?.Content.ToString();

                    // Преобразуем строку в тип ENUM C#
                    IngridEd edizmerEnum = (IngridEd)Enum.Parse(typeof(IngridEd), edizmerString);

                    if (string.IsNullOrWhiteSpace(TextIngr.Text))
                    {
                        MessageBox.Show("Введите ингредиент!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    string ingr = TextIngr.Text;
                    int idIndrid = Metods.GetCodeIngr(ingr, edizmerEnum);

                    DateTime? datepokup = datePicker1.SelectedDate?.Date;

                    int count;
                    if (string.IsNullOrWhiteSpace(Count.Text) || !int.TryParse(Count.Text, out count))
                    {
                        MessageBox.Show("Введите количество!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    decimal price;
                    string priceText = Price.Text.Replace('.', ',');
                    if (string.IsNullOrWhiteSpace(priceText) || !decimal.TryParse(priceText, out price))
                    {
                        MessageBox.Show("Введите стоимость!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    // Дополнительная проверка на адекватность числа
                    if (price <= 0)
                    {
                        MessageBox.Show("Стоимость должна быть больше нуля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    MessageBox.Show($"{price} + {priceText}");
                    //стоимость единицы продукта
                    decimal priceingrid = price / count;

                    string queryZakupInsert = @"INSERT INTO zakupka (id_ing, kolich_pokup, date_pok, price_pokup)
	                                                 VALUES (@idIndrid, @count, @datepokup, @price);";

                    using (NpgsqlCommand command = new NpgsqlCommand(queryZakupInsert, connection))
                    {
                        command.Parameters.AddWithValue("@idIndrid", idIndrid);
                        command.Parameters.AddWithValue("@count", count);
                        command.Parameters.AddWithValue("@datepokup", datepokup);
                        command.Parameters.AddWithValue("@price", price);
                        command.ExecuteNonQuery();
                    }

                    string queryIngrIdOst = @"SELECT Ostatok FROM Ingridients WHERE id_ingrid = @idIndrid";

                    int ost = 0;
                    using (NpgsqlCommand command = new NpgsqlCommand(queryIngrIdOst, connection))
                    {
                        command.Parameters.AddWithValue("@idIndrid", idIndrid);
                        //Безопасное получение результата
                        int ostatok = Convert.ToInt32(command.ExecuteScalar());
                        ost += ostatok;
                    }

                    int newost = ost + count;
                    string queryIngrUpdate = @"UPDATE ingridients
	                                           SET priceingrid = @priceingrid, ostatok = @newost
	                                           WHERE id_ingrid = @idIndrid;";

                    using (NpgsqlCommand command = new NpgsqlCommand(queryIngrUpdate, connection))
                    {
                        command.Parameters.AddWithValue("@idIndrid", idIndrid);
                        command.Parameters.AddWithValue("@priceingrid", priceingrid);
                        command.Parameters.AddWithValue("@newost", newost);
                        command.ExecuteNonQuery();
                    }

                    //MessageBox.Show($"Ингр.: {ingr} / {idIndrid}, Кол-во: {count}, Стоимость: {price}, Дата: {datepokup}, ЕдинИзм: {edizmerEnum}, За единицу:{priceingrid}, Был остаток:{ost}, НовыйОст:{newost}");

                    TextIngr.Text = "";
                    Count.Text = "";
                    Price.Text = "";
                    // Вызываем событие перед закрытием формы
                    DataUpdatedZakup?.Invoke();
                    DataUpdatedPriceOfOne?.Invoke();
                    transaction.Commit(); // Подтверждаем изменения в БД
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}",
                                "Ошибка",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                Console.WriteLine($"{ex.Message}");
            }
        }

        private void TextIngr_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = TextIngr.Text.Trim(); // Не переводим в ToLower здесь

            if (string.IsNullOrWhiteSpace(searchText))
            {
                IngrList.Visibility = Visibility.Collapsed;
                return;
            }

            InitIngr();

            // Используем StringComparison.OrdinalIgnoreCase для поиска без учета регистра
            var filterFam = ingr
                .Where(s => s != null && s.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filterFam.Count > 0)
            {
                IngrList.ItemsSource = filterFam;
                IngrList.Visibility = Visibility.Visible;
            }
            else
            {
                IngrList.Visibility = Visibility.Collapsed;
            }
        }
    }
}
