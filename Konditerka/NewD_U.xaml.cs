using MahApps.Metro.Controls;
using Npgsql;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Konditerka.Metods;

namespace Konditerka
{
    /// <summary>
    /// Логика взаимодействия для NewDecor.xaml
    /// </summary>
    public partial class NewDecor : MetroWindow
    {
        private int options;
        DataTable dataTable = new DataTable();
        private List<string> search_name = new List<string>(); // Для хранения списка
        public event Action DataUpDecor;
        public event Action DataUpUpak;
        public event Action DataUpdatedZakup;

        public NewDecor(int opt)
        {
            options = opt;
            InitializeComponent();
            Config();
        }

        private void Config()
        {
            switch (options)
            {
                //для декора
                case 1:
                    this.Title = "Добавление декора";
                    LoadData(queryDecor);
                    //InitDecor();
                    Search(searchDecor, nameDecor);
                    D_U1.Visibility = Visibility.Visible;
                    D_U2.Visibility = Visibility.Visible;
                    DecorLabel.Visibility = Visibility.Visible;
                    SaveDecor.Visibility = Visibility.Visible;
                    break;

                //для упаковки
                case 2:
                    this.Title = "Добавление упаковки";
                    LoadData(queryUpak);
                    //InitUpak();
                    Search(searchUpak, nameUpak);
                    D_U1.Visibility = Visibility.Visible;
                    D_U2.Visibility = Visibility.Visible;
                    UpakLabel.Visibility = Visibility.Visible;
                    SaveUpak.Visibility = Visibility.Visible;
                    break;

                //для закупки
                case 3:
                    this.Title = "Добавление ингредиентов";
                    LoadData(queryZakup);
                    Search(searchIngr, nameIngr);
                    Z1.Visibility = Visibility.Visible;
                    Z2.Visibility = Visibility.Visible;
                    ZakupLabel.Visibility = Visibility.Visible;
                    SaveZakup.Visibility = Visibility.Visible;
                    DateGrid.Visibility = Visibility.Visible;
                    Zakup.Visibility = Visibility.Visible;
                    datePicker1.SelectedDate = DateTime.Today;
                    break;
            }
        }

        string queryUpak = "SELECT Id_upak,	Nazvanie FROM TypeUpak";
        string queryDecor = "SELECT id_decor, name_decor FROM type_decor";
        string queryZakup = "SELECT id_ingrid, nameingrid FROM ingridients";

        private void LoadData(string query)
        {
            try
            {
                using (NpgsqlConnection connection = Metods.Source.OpenConnection())
                {
                    NpgsqlDataAdapter dataAdapterUpak = new NpgsqlDataAdapter(query, connection);

                    DataTable dataTable = new DataTable();
                    dataAdapterUpak.Fill(dataTable);

                    DG.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                                "Ошибка подключения",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                Console.WriteLine("{ ex.Message}");
            }
        }
        string searchDecor = "SELECT id_decor, name_decor FROM type_decor WHERE name_decor ILIKE @name_decor";
        string searchUpak = "SELECT id_upak, nazvanie FROM typeupak WHERE nazvanie ILIKE @nazvanie";
        string searchIngr = "SELECT id_ingrid, nameingrid FROM ingridients WHERE nameingrid ILIKE @nameingrid";
        string nameDecor = "name_decor";
        string nameUpak = "nazvanie";
        string nameIngr = "nameingrid";

        private void Search(string querySearch, string inDBname)
        {
            using (NpgsqlConnection connection = Metods.Source.OpenConnection())
            {
                string name = TextBoxName.Text;

                NpgsqlCommand command = new NpgsqlCommand(querySearch, connection);
                command.Parameters.AddWithValue(inDBname, "%" + name + "%");

                NpgsqlDataAdapter dataAdapterDecor = new NpgsqlDataAdapter(command);

                dataTable.Clear();
                dataAdapterDecor.Fill(dataTable);
            }
            search_name = dataTable.AsEnumerable().Select(row => row.Field<string>(inDBname)).Where(name => name != null).Distinct().ToList();
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Проверяем, что выбран именно объект (строка)
            if (List.SelectedItem != null)
            {
                // Получаем строку из выбранного элемента
                string selectedName = List.SelectedItem.ToString();

                // Отключаем событие, чтобы изменение текста не вызывало повторный поиск в БД
                TextBoxName.TextChanged -= TextBoxName_TextChanged;

                // Подставляем ОРИГИНАЛЬНОЕ название из списка
                TextBoxName.Text = selectedName;

                // Ставим курсор в конец текста
                TextBoxName.CaretIndex = TextBoxName.Text.Length;

                // Скрываем список
                List.Visibility = Visibility.Collapsed;

                // Включаем событие обратно
                TextBoxName.TextChanged += TextBoxName_TextChanged;
            }
        }
        private void TextBoxName_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = TextBoxName.Text.Trim(); // Не переводим в ToLower здесь

            if (string.IsNullOrWhiteSpace(searchText))
            {
                List.Visibility = Visibility.Collapsed;
                return;
            }

            switch (options)
            {
                case 1:
                    Search(searchDecor, nameDecor);
                    break;
                case 2:
                    Search(searchUpak, nameUpak);
                    break;
                case 3:
                    Search(searchIngr, nameIngr);
                    break;
            }

            // Используем StringComparison.OrdinalIgnoreCase для поиска без учета регистра
            var filter = search_name
                .Where(s => s != null && s.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (filter.Count > 0)
            {
                List.ItemsSource = filter;
                List.Visibility = Visibility.Visible;
            }
            else
            {
                List.Visibility = Visibility.Collapsed;
                EdIzm.Visibility = Visibility.Visible;
            }
        }

        private void SaveDecor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (NpgsqlConnection connection = Metods.Source.OpenConnection())
                using (var transaction = connection.BeginTransaction()) // Начинаем транзакцию
                {
                    string queryD = "SELECT id_decor FROM type_decor WHERE name_decor = @name";
                    string queryDUP = @"INSERT INTO type_decor(name_decor, price_decor, ostatok) 
                                        VALUES (@name, 0, 0) RETURNING id_decor;";

                    if (string.IsNullOrWhiteSpace(TextBoxName.Text))
                    {
                        MessageBox.Show("Введите название!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    string name = TextBoxName.Text;
                    int id = Metods.GetCodeD_U(queryD, queryDUP, name);

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
                    //стоимость единицы
                    decimal newprice = Math.Round(price / count, 2, MidpointRounding.AwayFromZero);

                    //MessageBox.Show($"{name}, {id}, кол-во:{count}, цена:{price}, за ед:{newprice}");

                    string queryUpDecor = "SELECT ostatok FROM type_decor WHERE id_decor = @id";
                    int ost = 0;
                    using (NpgsqlCommand command = new NpgsqlCommand(queryUpDecor, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        int ostatok = Convert.ToInt32(command.ExecuteScalar());
                        ost += ostatok;
                    }
                    int newost = ost + count;
                    //MessageBox.Show($"{count}, {ost}, {newost}");

                    string queryUpDec = @"UPDATE type_decor SET price_decor = @newprice, ostatok = @newost WHERE id_decor = @id;";

                    using (NpgsqlCommand command = new NpgsqlCommand(queryUpDec, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@newprice", newprice);
                        command.Parameters.AddWithValue("@newost", newost);
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit(); // Подтверждаем изменения в БД
                    Cleaning();
                    DataUpDecor?.Invoke();
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
        private void SaveUpak_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (NpgsqlConnection connection = Metods.Source.OpenConnection())
                using (var transaction = connection.BeginTransaction()) // Начинаем транзакцию
                {
                    string queryUp = "SELECT id_upak FROM typeupak WHERE nazvanie = @name";
                    string queryUpUP = @"INSERT INTO typeupak(nazvanie, priceupak, ostatok) 
                                        VALUES (@name, 0, 0) RETURNING id_upak;";

                    if (string.IsNullOrWhiteSpace(TextBoxName.Text))
                    {
                        MessageBox.Show("Введите название!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    string name = TextBoxName.Text;
                    int id = Metods.GetCodeD_U(queryUp, queryUpUP, name);

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
                    //стоимость единицы
                    decimal newprice = Math.Round(price / count, 2, MidpointRounding.AwayFromZero);

                    string queryUpUpak = "SELECT ostatok FROM typeupak WHERE id_upak = @id";
                    int ost = 0;
                    using (NpgsqlCommand command = new NpgsqlCommand(queryUpUpak, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        int ostatok = Convert.ToInt32(command.ExecuteScalar());
                        ost += ostatok;
                    }
                    int newost = ost + count;

                    string queryUpDec = @"UPDATE typeupak SET priceupak = @newprice, ostatok = @newost WHERE id_upak = @id;";

                    using (NpgsqlCommand command = new NpgsqlCommand(queryUpDec, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@newprice", newprice);
                        command.Parameters.AddWithValue("@newost", newost);
                        command.ExecuteNonQuery();
                    }
                    transaction.Commit(); // Подтверждаем изменения в БД
                    Cleaning();
                    DataUpUpak?.Invoke();
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

        private void SaveZak_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (NpgsqlConnection connection = Metods.Source.OpenConnection())
                using (var transaction = connection.BeginTransaction()) // Начинаем транзакцию
                {
                    if (string.IsNullOrWhiteSpace(TextBoxName.Text))
                    {
                        MessageBox.Show("Введите ингредиент!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    string ingr = TextBoxName.Text;

                    // Логика "Умного поиска" ед.изм
                    int idIndrid = Metods.GetCodeIngr(ingr);
                    // Если ингредиента нет, проверяем выбран ли ComboBox
                    if (idIndrid <= 0)
                    {
                        if (EdIzm.SelectedItem == null)
                        {
                            MessageBox.Show("Новый ингредиент! Выберите единицы измерения.");
                            EdIzm.Visibility = Visibility.Visible;
                            return;
                        }
                        string edStr = (EdIzm.SelectedItem as ComboBoxItem)?.Content.ToString();
                        IngridEd edEnum = (IngridEd)Enum.Parse(typeof(IngridEd), edStr);
                        idIndrid = Metods.GetCodeIngr(ingr, edEnum);
                    }

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

                    //стоимость единицы продукта
                    decimal priceingrid = price / count;
                    // Записываем закупку
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

                    // Обновляем ингредиент (Цена и Остаток одним запросом)
                    string queryUpdate = @"UPDATE ingridients SET priceingrid = @priceingrid, ostatok = ostatok + @count 
                                                                WHERE id_ingrid = @idIndrid;";
                    using (var cmd = new NpgsqlCommand(queryUpdate, connection))
                    {
                        cmd.Parameters.AddWithValue("priceingrid", priceingrid);
                        cmd.Parameters.AddWithValue("count", count);
                        cmd.Parameters.AddWithValue("idIndrid", idIndrid);
                        cmd.ExecuteNonQuery();
                    }
                    transaction.Commit(); // Подтверждаем изменения в БД
                    //MessageBox.Show($"Ингр.: {ingr} / {idIndrid}, Кол-во: {count}, Стоимость: {price}, Дата: {datepokup}, ЕдинИзм: {edizmerEnum}, За единицу:{priceingrid}, Был остаток:{ost}, НовыйОст:{newost}");
                    Metods.DataUpdatedPriceOfOne(idIndrid);
                    Cleaning();
                    EdIzm.Visibility = Visibility.Collapsed;
                    // Вызываем событие перед закрытием формы
                    DataUpdatedZakup?.Invoke();
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

        private void Cleaning()
        {
            TextBoxName.Text = "";
            Price.Text = "";
            Count.Text = "";
            // Сброс поисковика
            List.SelectedItem = null;          // Снимаем выделение, чтобы SelectionChanged сработал в следующий раз
            List.ItemsSource = null;           // Убираем старые результаты из списка
            List.Visibility = Visibility.Collapsed; // Скрываем сам контрол
            search_name?.Clear();              // Очищаем кэш имен, если это необходимо
        }

        private void TextBoxName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back) { Cleaning(); }
        }
    }
}
