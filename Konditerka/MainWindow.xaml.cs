using MahApps.Metro.Controls;
using Npgsql;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Threading;
using static Konditerka.Metods;

namespace Konditerka
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private DateTime SelectedDateTime;
        //Создаем экземпляр хелпера
        private CalendarHelper _calendarHelper = new CalendarHelper();
        private int id_zak_komm;
        private string oldstatzak;
        private int id_ingrid;
        private int id_kli;
        private int id_decor;
        private int id_upak;
        private int id_recip;
        private string nameOfRecip;

        public MainWindow()
        {
            InitializeComponent();
            // 1. Загружаем данные в хелпер
            _calendarHelper.LoadCalendar();
            // 2. СВЯЗЫВАЕМ КОНВЕРТЕР С ХЕЛПЕРОМ
            DateToDescriptionConverter.Helper = _calendarHelper;
            LoadData();
        }

        private void LoadData()
        {
            Metods.LoadGrid(Metods.queryKlients, DataGridKlients);
            Metods.LoadGrid(Metods.queryZakazy, DataGridZakazy);
            Metods.LoadGrid(Metods.queryRec, DataGridRecipes);
            Metods.LoadGrid(Metods.queryIngr, DataGridIngr);
            Metods.LoadGrid(Metods.queryUpak, DataGridUpak);
            Metods.LoadGrid(Metods.queryZakup, DataGridZakup);
            Metods.LoadGrid(Metods.queryDecor, DataGridDecor);
        }



        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Скрытие столбцов
            // Получаем имя текущего столбца (оно совпадает с именем поля в БД/DataTable)
            string headerName = e.Column.Header.ToString();

            // Проверяем, является ли текущий столбец тем, который мы хотим скрыть
            if (headerName == "id_recipes" || headerName == "id_ingrid" ||
                headerName == "id_upak" || headerName == "id_zakup" || headerName == "id_decor")
            {
                // Если да, устанавливаем его видимость в Скрыто (Collapsed)
                e.Column.Visibility = Visibility.Collapsed;
            }

            // 2. Добавляем перенос текста и стиль выравнивания
            if (headerName == "Описание" || headerName == "Инструкция" ||
                headerName == "Название" || headerName == "Категория")
            {
                // *** ИСПОЛЬЗУЕМ ElementStyle ВМЕСТО CellStyle ***
                // Явно приводим столбец к типу DataGridTextColumn, чтобы получить ElementStyle
                var textColumn = e.Column as DataGridTextColumn;

                if (textColumn != null)
                {
                    Style elementStyle = new Style(typeof(TextBlock));
                    elementStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
                    elementStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Top));

                    textColumn.ElementStyle = elementStyle;
                }
            }

            // Увеличение/уменьшение ширины столбцов
            if (headerName == "Описание" || headerName == "Инструкция" || headerName == "Ингредиент" || headerName == "Наименование")
            {
                // Если вы хотите, чтобы эти столбцы были шире других по умолчанию
                e.Column.Width = new DataGridLength(2.5, DataGridLengthUnitType.Star);
            }
            else if (headerName == "Необходимое количество" || headerName == "ID клиента" || headerName == "Номер заказа")
            {
                //уменьшение ширины столбца
                e.Column.Width = new DataGridLength(0.5, DataGridLengthUnitType.Star);
            }
            else
            {
                // Все остальные столбцы будут иметь стандартную пропорцию ширины (1*)
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            }

            //изменение формата времени
            if (headerName == "Дата выполнения заказа")
            {
                // Приводим колонку к текстовому типу (обычно это DataGridTextColumn)
                var column = e.Column as DataGridTextColumn;
                if (column != null)
                {
                    // Устанавливаем формат: HH:mm — это 24-часовой формат
                    // {0:dd.MM.yyyy HH:mm} если нужна дата + время
                    // {0:HH:mm} если нужно только время
                    column.Binding.StringFormat = "{0:dd.MM.yyyy HH:mm}";
                }
            }

            //изменение формата времени
            if (headerName == "Дата заказа")
            {
                // Приводим колонку к текстовому типу (обычно это DataGridTextColumn)
                var column = e.Column as DataGridTextColumn;
                if (column != null)
                {
                    // Устанавливаем формат: HH:mm — это 24-часовой формат
                    // {0:dd.MM.yyyy HH:mm} если нужна дата + время
                    // {0:HH:mm} если нужно только время
                    column.Binding.StringFormat = "{0:dd.MM.yyyy}";
                }
            }
            // 3. Также можно настроить ширину столбцов, если нужно (опционально)
            //if (headerName == "Описание" || headerName == "Инструкция")
            //{
            //    e.Column.Width = new DataGridLength(200, DataGridLengthUnitType.Pixel);
            //}

            // 4. Выравнивание числовых/денежных данных по правому краю (опциональное улучшение форматирования)
            //if (headerName == "Стоимость" || headerName == "Цена за единицу" || headerName == "Количество" || headerName == "Предоплата")
            //{
            //    e.Column.CellStyle = new Style(typeof(DataGridCell))
            //    {
            //        Setters = {
            //            new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right)
            //        }
            //    };
        }

        // Метод срабатывает при выборе даты в календаре
        private void MainCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainCalendar.SelectedDate.HasValue)
            {
                DateTime selectedDate = MainCalendar.SelectedDate.Value;

                // 1. Показываем ListBox (он у вас изначально Collapsed)
                TimeListBox.Visibility = Visibility.Visible;

                // 2. Получаем список уже занятого времени из БД через хелпер
                var bookedTimes = _calendarHelper.GetBookedTimesForDate(selectedDate);

                // 3. Генерируем интервалы времени в ListBox
                _calendarHelper.GenerateTimeIntervals(selectedDate, TimeListBox, bookedTimes);

                // --- РЕШЕНИЕ ПРОБЛЕМЫ ФОКУСА ---

                // 1. Убираем захват мыши с календаря, чтобы он не "крал" фокус обратно
                Mouse.Capture(null);

                // 2. Выполняем фокус после завершения всех событий клика
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Устанавливаем фокус на сам ListBox
                    TimeListBox.Focus();

                    if (TimeListBox.Items.Count > 0)
                    {
                        TimeListBox.SelectedIndex = 0;

                        // Фокусируем конкретный ListBoxItem, чтобы он сразу понимал нажатие Enter/клик
                        var container = (ListBoxItem)TimeListBox.ItemContainerGenerator.ContainerFromIndex(0);
                        container?.Focus();
                    }
                }), System.Windows.Threading.DispatcherPriority.Background); // Важен приоритет Background
            }
        }

        // Метод для кнопки "Оформить"
        private void Оформить_Click(object sender, RoutedEventArgs e)
        {
            if (TimeListBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите время заказа!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedSlot = (CalendarHelper.TimeSlot)TimeListBox.SelectedItem;
            bool redZak = false; // По умолчанию заказ обычный

            // ПРОВЕРКА: Если интервал помечен как недоступный (красный/розовый)
            if (!selectedSlot.IsEnabled)
            {
                // Определяем причину для текста сообщения
                string reason = (selectedSlot.Time - DateTime.Now).TotalHours < 48
                    ? "до этого времени осталось менее 48 часов"
                    : "это время находится в 6-часовом интервале от другого заказа";

                // Показываем окно с вопросом
                MessageBoxResult result = MessageBox.Show(
                    $"Внимание! Выбранное время не рекомендуется для бронирования, так как {reason}.\n\n" +
                    "Вы всё равно хотите продолжить оформление заказа на это время?",
                    "Предупреждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                // Если пользователь нажал "Нет", выходим из метода
                if (result == MessageBoxResult.No) return;
                redZak = true; // Пользователь согласился, ставим маркер
            }

            // Если время валидно ИЛИ пользователь нажал "Да" в MessageBox:
            DateTime selectedFullDate = selectedSlot.Time;
            SelectedDateTime = selectedFullDate;

            CreateZak cz = new CreateZak(SelectedDateTime, redZak);
            cz.DataUpdatedZak += DataUpdatedZak;
            cz.Show();
        }

        //обновлние после оформления закупки
        private void DataUpdatedZakup()
        {
            Metods.LoadGridwithoutClear(Metods.queryIngr, DataGridIngr);
            Metods.LoadGridwithoutClear(Metods.queryZakup, DataGridZakup);
        }

        //обновлние после оформления
        private void DataUpdatedZak()
        {
            MainCalendar.SelectedDate = null;
            TimeListBox.Visibility = Visibility.Collapsed;

            Metods.LoadGridwithoutClear(Metods.queryKlients, DataGridKlients);
            Metods.LoadGridwithoutClear(Metods.queryRec, DataGridRecipes);
            Metods.LoadGridwithoutClear(Metods.queryZakazy, DataGridZakazy);
            Metods.LoadGridwithoutClear(Metods.queryIngr, DataGridIngr);
            Metods.LoadGridwithoutClear(Metods.queryUpak, DataGridUpak);
            Metods.LoadGridwithoutClear(Metods.queryDecor, DataGridDecor);
            _calendarHelper.LoadCalendar();
            DateToDescriptionConverter.Helper = _calendarHelper;
        }

        //обновление после добавления декора
        private void DataUpDecor()
        {
            Metods.LoadGridwithoutClear(Metods.queryDecor, DataGridDecor);
        }

        //обновление после добавления упаковки
        private void DataUpUpak()
        {
            Metods.LoadGridwithoutClear(Metods.queryUpak, DataGridUpak);
        }

        //обновление после редактирования клиента
        private void DataUpKli()
        {
            Metods.LoadGridwithoutClear(Metods.queryKlients, DataGridKlients);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        //двойной клик на одной вкладке - перемещение на сост_заказа
        private void DataGridZakazy_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataGridZakazy.SelectedItem is DataRowView row)
            {
                // Показываем фрейм, скрываем таблицу
                SostZakFrame.Visibility = Visibility.Visible;
                DataGridZakazy.Visibility = Visibility.Collapsed;

                SostZak SZ = new SostZak(row);
                SostZakFrame.Navigate(SZ);
            }
        }
        private void SostZakFrame_Navigated(object sender, NavigationEventArgs e)
        {
            // Если контент стал пустой (мы нажали Назад), возвращаем таблицу на экран
            if (e.Content == null)
            {
                DataGridZakazy.Visibility = Visibility.Visible;
                SostZakFrame.Visibility = Visibility.Collapsed;
            }
        }

        //настройки переходов на страницу сост_рец
        private void SostRecipFrame_Navigated(object sender, NavigationEventArgs e)
        {
            // Если контент стал пустой (мы нажали Назад), возвращаем таблицу на экран
            if (e.Content == null)
            {
                DataGridRecipes.Visibility = Visibility.Visible;
                SostRecipFrame.Visibility = Visibility.Collapsed;
            }
        }
        private void DataGridRecipes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataGridRecipes.SelectedItem is DataRowView row)
            {
                // Показываем фрейм, скрываем таблицу
                SostRecipFrame.Visibility = Visibility.Visible;
                DataGridRecipes.Visibility = Visibility.Collapsed;

                SostRecip SR = new SostRecip(row);
                SostRecipFrame.Navigate(SR);
            }
        }

        //скрытие кнопки/ очистка страницы
        private void TabContr_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Проверяем, что событие пришло именно от TabControl (а не от вложенных списков)
            if (e.Source is TabControl tabControl)
            {
                // Если сейчас выбрана НЕ секретная вкладка
                if (tabControl.SelectedItem != Recipes)
                {
                    SostRecipFrame.Content = null;
                    // Скрываем её обратно
                    NewRec.Visibility = Visibility.Collapsed;
                }
                if (tabControl.SelectedItem == Recipes)
                {
                    NewRec.Visibility = Visibility.Visible;
                }
                if (tabControl.SelectedItem != Zakazy)
                {
                    SostZakFrame.Content = null;
                }
            }
        }

        //добавление комментария к заказу
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            System.Data.DataRowView selectedRow = (System.Data.DataRowView)DataGridZakazy.SelectedItem;
            id_zak_komm = Convert.ToInt32(selectedRow["Номер заказа"]);
            EditPopup.IsOpen = true;
            EditTextBox.Focus();
            //MessageBox.Show(komment);
        }
        private void SaveKomment_Click(object sender, RoutedEventArgs e)
        {
            string newkomm = EditTextBox.Text;
            using (NpgsqlConnection connection = Metods.Source.OpenConnection())
            {
                string query = "UPDATE zakazy SET zametka = @newkomm WHERE id_zakaz = @id_zak_komm;";
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@newkomm", newkomm); // Новый комм
                    command.Parameters.AddWithValue("@id_zak_komm", id_zak_komm);  // id_заказа
                    command.ExecuteNonQuery();
                }
            }
            EditPopup.IsOpen = false;

            // СОВЕТ: Обнови строку в DataTable вручную, чтобы не делать Select заново
            if (DataGridZakazy.SelectedItem is DataRowView row)
            {
                row["Комментарии"] = newkomm; // Укажи здесь точное имя колонки из DataTable
            }
            EditTextBox.Clear();
        }

        //изменение статуса заказа
        private void Status_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridZakazy.SelectedItem is DataRowView selectedRow)
            {
                id_zak_komm = Convert.ToInt32(selectedRow["Номер заказа"]);

                if (!(sender is MenuItem menuItem) || menuItem.Tag == null) return;

                StatusZak status = (StatusZak)menuItem.Tag;
                // Подготавливаем строку для БД (заменяем _ на пробел)
                string statusStringForDb = status.ToString().Replace("_", " ");

                using (NpgsqlConnection connection = Metods.Source.OpenConnection())
                {
                    // Добавляем ::order_status в текст запроса
                    string query = "UPDATE zakazy SET status = @status::order_status WHERE id_zakaz = @id_zak_komm;";
                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@status", statusStringForDb);
                        command.Parameters.AddWithValue("@id_zak_komm", id_zak_komm);
                        command.ExecuteNonQuery();
                    }
                }
                // Обновляем UI
                selectedRow["Статус заказа"] = statusStringForDb;
            }
        }

        //обработчик ПКМ контекстного меню
        private void DataGridZakazy_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (DataGridZakazy.SelectedItem is DataRowView row)
            {
                string currentStatus = row["Статус заказа"].ToString();

                // Сначала показываем все пункты
                New.Visibility = Visibility.Visible;
                InW.Visibility = Visibility.Visible;
                Got.Visibility = Visibility.Visible;
                izm_st.Visibility = Visibility.Visible;

                // Скрываем ненужное в зависимости от текущего статуса
                if (currentStatus == "Новый")
                    New.Visibility = Visibility.Collapsed;

                else if (currentStatus == "В работе")
                {
                    New.Visibility = Visibility.Collapsed;
                    InW.Visibility = Visibility.Collapsed;
                }
                else if (currentStatus == "Готов к выдаче")
                {
                    New.Visibility = Visibility.Collapsed;
                    InW.Visibility = Visibility.Collapsed;
                    Got.Visibility = Visibility.Collapsed;
                }
                else if (currentStatus == "Выдан" || currentStatus == "Отменен")
                {
                    izm_st.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void NewDecor_Click(object sender, RoutedEventArgs e)
        {
            NewDecor newDec = new NewDecor(1);
            newDec.DataUpDecor += DataUpDecor;
            newDec.Show();
        }

        private void NewUpak_Click(object sender, RoutedEventArgs e)
        {
            NewDecor newDec = new NewDecor(2);
            newDec.DataUpUpak += DataUpUpak;
            newDec.Show();
        }

        //добавление закупки
        private void Postuplenie_Click(object sender, RoutedEventArgs e)
        {
            NewDecor newDec = new NewDecor(3);
            newDec.DataUpdatedZakup += DataUpdatedZakup;
            newDec.Show();
        }

        private void DataUpdatedNewRecipe()
        {
            Metods.LoadGridwithoutClear(Metods.queryRec, DataGridRecipes);
        }



        //новый рецепт
        private void NewRec_Click(object sender, RoutedEventArgs e)
        {
            NewRecipe NewR = new NewRecipe();
            NewR.DataUpdatedNewRecipe += DataUpdatedNewRecipe;
            NewR.Show();
        }

        private void DataGridKlients_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            //для обработки ПКМ
        }

        //ПКМ редактор клиента
        private void EditKl_Click(object sender, RoutedEventArgs e)
        {
            CleanEditKli();
            // Используем универсальный метод:
            Metods.AnimatePanel(EditKli, EditPanelKli, show: true);

            System.Data.DataRowView selectedRow = (System.Data.DataRowView)DataGridKlients.SelectedItem;
            id_kli = Convert.ToInt32(selectedRow["ID клиента"]);
            Name.Text = selectedRow["Имя"].ToString();
            Fam.Text = selectedRow["Фамилия"].ToString();
            Tel.Text = selectedRow["Номер телефона"].ToString();
            Adr.Text = selectedRow["Адрес"].ToString();
            Zamet.Text = selectedRow["Заметки"].ToString();
        }

        private void SaveEditKli_Click(object sender, RoutedEventArgs e)
        {
            int id_klient = id_kli;
            string firstname = Name.Text;
            string lastname = Fam.Text;
            string phone = Tel.Text;
            string adress = Adr.Text;
            string zametka = Zamet.Text;

            using (NpgsqlConnection connection = Metods.Source.OpenConnection())
            {
                string query = @"UPDATE klients SET firstname = @firstname, lastname = @lastname, phone = @phone, adress = @adress, zametka = @zametka
                                WHERE id_klient = @id_klient;";
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@firstname", firstname);
                    command.Parameters.AddWithValue("@lastname", lastname);
                    command.Parameters.AddWithValue("@phone", phone);
                    command.Parameters.AddWithValue("@adress", adress);
                    command.Parameters.AddWithValue("@zametka", zametka);
                    command.Parameters.AddWithValue("@id_klient", id_klient);  // id_кли
                    command.ExecuteNonQuery();
                }
            }
            Metods.AnimatePanel(EditKli, EditPanelKli, show: false);
            CleanEditKli();
            DataUpKli();
        }

        private void CancelKli_Click(object sender, RoutedEventArgs e)
        {
            Metods.AnimatePanel(EditKli, EditPanelKli, show: false);
            CleanEditKli();
        }

        public void CleanEditKli()
        {
            Name.Text = "";
            Fam.Text = "";
            Tel.Text = "";
            Adr.Text = "";
            Zamet.Text = "";
        }

        private void KommCancel_Click(object sender, RoutedEventArgs e)
        {
            EditTextBox.Text = "";
            EditPopup.IsOpen = false;
        }

        //изменение количества на складе
        private void EditKolich_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridIngr.SelectedItem == null) return;
            System.Data.DataRowView selectedRow = (System.Data.DataRowView)DataGridIngr.SelectedItem;
            id_ingrid = Convert.ToInt32(selectedRow["Id_Ingrid"]);
            string name = selectedRow["Ингредиент"].ToString();
            ChangeName.Text = $"{name},";
            Change.Text = $"действительное количество:";
            EditKolichPopup.IsOpen = true;
            EditKolichBox.Focus();
        }
        private void EditKolichUpak_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridUpak.SelectedItem == null) return;
            System.Data.DataRowView selectedRow = (System.Data.DataRowView)DataGridUpak.SelectedItem;
            id_upak = Convert.ToInt32(selectedRow["Id_upak"]);
            string name = selectedRow["Наименование"].ToString();
            ChangeNameUpak.Text = $"{name},";
            ChangeUpak.Text = $"действительное количество:";
            EditKolichPopupUpak.IsOpen = true;
            EditKolichBoxUpak.Focus();
        }
        private void EditKolichDec_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridDecor.SelectedItem == null) return;
            System.Data.DataRowView selectedRow = (System.Data.DataRowView)DataGridDecor.SelectedItem;
            id_decor = Convert.ToInt32(selectedRow["Id_decor"]);
            string name = selectedRow["Название"].ToString();
            ChangeNameDec.Text = $"{name},";
            ChangeDec.Text = $"действительное количество:";
            EditKolichPopupDec.IsOpen = true;
            EditKolichBoxDec.Focus();
        }

        //сохраение нового кол-ва
        string queryNewIngr = "UPDATE ingridients SET ostatok = @newKolich WHERE id_ingrid = @id;";
        string queryNewDec = "UPDATE type_decor SET ostatok = @newKolich WHERE id_decor = @id;";
        string queryNewUp = "UPDATE typeupak SET ostatok = @newKolich WHERE id_upak = @id;";

        private void SaveKolich_Click(object sender, RoutedEventArgs e)
        {
            Metods.ChangeOst(EditKolichBox, queryNewIngr, id_ingrid, EditKolichPopup, DataGridIngr);
        }
        private void SaveKolichDec_Click(object sender, RoutedEventArgs e)
        {
            Metods.ChangeOst(EditKolichBoxDec, queryNewDec, id_decor, EditKolichPopupDec, DataGridDecor);
        }
        private void SaveKolichUpak_Click(object sender, RoutedEventArgs e)
        {
            Metods.ChangeOst(EditKolichBoxUpak, queryNewUp, id_upak, EditKolichPopupUpak, DataGridUpak);
        }
        //обработчик кнопки Enter
        private void EditKolichBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { Metods.ChangeOst(EditKolichBox, queryNewIngr, id_ingrid, EditKolichPopup, DataGridIngr); }
        }
        private void EditKolichBoxDec_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { Metods.ChangeOst(EditKolichBoxDec, queryNewDec, id_decor, EditKolichPopupDec, DataGridDecor); }
        }
        private void EditKolichBoxUpak_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { Metods.ChangeOst(EditKolichBoxUpak, queryNewUp, id_upak, EditKolichPopupUpak, DataGridUpak); }
        }
        //обработчик отмены
        private void KolichOtmena(object sender, RoutedEventArgs e)
        {
            EditKolichBox.Clear();
            EditKolichPopup.IsOpen = false;
        }
        private void KolichOtmenaDec(object sender, RoutedEventArgs e)
        {
            EditKolichBoxDec.Clear();
            EditKolichPopupDec.IsOpen = false;
        }
        private void KolichOtmenaUpak(object sender, RoutedEventArgs e)
        {
            EditKolichBoxUpak.Clear();
            EditKolichPopupUpak.IsOpen = false;
        }

        private void DataGridRecipes_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            //пустой обработчик ПКМ в DataGrid
        }

        private void OpenEditPopup(string fieldName, string headerText)
        {
            var selectedRow = (System.Data.DataRowView)DataGridRecipes.SelectedItem;
            if (selectedRow == null) return;

            id_recip = Convert.ToInt32(selectedRow["id_recipes"]);
            nameOfRecip = fieldName; // Запоминаем, что правим

            TextBlock.Text = headerText;
            EditBox.Text = selectedRow[fieldName].ToString();

            EditRecipPopup.IsOpen = true;
            EditBox.Focus();
        }
        // Теперь обработчики кнопок стали крошечными:
        private void EditOpis_Click(object sender, RoutedEventArgs e) => OpenEditPopup("Описание", "Описание:");
        private void EditInstr_Click(object sender, RoutedEventArgs e) => OpenEditPopup("Инструкция", "Инструкция:");

        private void SaveOI_Click(object sender, RoutedEventArgs e)
        {
            string newText = EditBox.Text;

            using (NpgsqlConnection connection = Metods.Source.OpenConnection())
            {
                string query = null;
                if (nameOfRecip == "Описание")
                {
                    query = @"UPDATE recipes SET opisanie = @text WHERE id_recipes = @id";
                }
                if (nameOfRecip == "Инструкция")
                {
                    query = @"UPDATE recipes SET instruction = @text WHERE id_recipes = @id";
                }

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@text", newText);
                    command.Parameters.AddWithValue("@id", id_recip);
                    command.ExecuteNonQuery();
                }
            }

            // обновить данные в самом DataGrid, чтобы изменения сразу были видны:
            System.Data.DataRowView selectedRow = (System.Data.DataRowView)DataGridRecipes.SelectedItem;
            selectedRow[nameOfRecip] = newText;
            EditBox.Clear();
            EditRecipPopup.IsOpen = false;
        }

        private void CancelOI_Click(object sender, RoutedEventArgs e)
        {
            EditBox.Clear();
            EditRecipPopup.IsOpen = false;
        }

        private void Dop_Click(object sender, RoutedEventArgs e)
        {
            Raskhody r = new Raskhody();
            r.Show();
        }
    }
}