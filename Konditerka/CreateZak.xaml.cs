using MahApps.Metro.Controls;
using Npgsql;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Konditerka
{
    // 1. Класс, описывающий структуру данных в БД (таблица type_decor)
    public class TypeDecor
    {
        public int Id_decor { get; set; }
        public string name_decor { get; set; }
        public int ostatok { get; set; }
        public decimal price_decor { get; set; }
    }

    public class TypeUpak
    {
        public int id_upak { get; set; }
        public string nazvanie { get; set; }
        public int ostatok { get; set; }
        public decimal priceupak { get; set; }
    }

    // 2. Класс, описывающий ОДНУ СТРОКУ в твоем списке декора на экране
    public class Decor : INotifyPropertyChanged
    {
        private string _name;
        private string _kolich;
        private int _id;
        private decimal _price_decor;
        private TypeDecor _selectedType;

        // ID декора (скрытое поле, нужно для сохранения в БД)
        public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }
        // Название декора (для отображения)
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        // Количество (строка, так как вводится в TextBox)
        public string Kolich { get => _kolich; set { _kolich = value; OnPropertyChanged(); } }
        // Цена (обновляется автоматически при выборе в ComboBox)
        public decimal Price
        {
            get => _price_decor;
            set { _price_decor = value; OnPropertyChanged(); }
        }

        // ВАЖНО: Сюда WPF положит весь объект, который юзер выберет в ComboBox
        public TypeDecor SelectedType
        {
            get => _selectedType;
            set
            {
                _selectedType = value;
                if (_selectedType != null)
                {
                    // Если юзер выбрал декор, "раскидываем" его данные по свойствам этой строки
                    this.Id = _selectedType.Id_decor;
                    this.Name = _selectedType.name_decor;
                    this.Price = _selectedType.price_decor;
                }
                OnPropertyChanged(); // Уведомляем UI, что выбор изменился
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class Upak : INotifyPropertyChanged
    {
        private string _nazvanie;
        private string _kolich_up;
        private int _id_upak;
        private decimal _priceupak;
        private TypeUpak _selectedTypeUpak;

        // ID (скрытое поле, нужно для сохранения в БД)
        public int Id_up { get => _id_upak; set { _id_upak = value; OnPropertyChanged(); } }
        // Название (для отображения)
        public string Name_up { get => _nazvanie; set { _nazvanie = value; OnPropertyChanged(); } }
        // Количество (строка, так как вводится в TextBox)
        public string Kolich_up { get => _kolich_up; set { _kolich_up = value; OnPropertyChanged(); } }
        // Цена (обновляется автоматически при выборе в ComboBox)
        public decimal Price_up
        {
            get => _priceupak;
            set { _priceupak = value; OnPropertyChanged(); }
        }

        // ВАЖНО: Сюда WPF положит весь объект, который юзер выберет в ComboBox
        public TypeUpak SelectedTypeUpak
        {
            get => _selectedTypeUpak;
            set
            {
                _selectedTypeUpak = value;
                if (_selectedTypeUpak != null)
                {
                    // Если юзер выбрал упаковку, "раскидываем" его данные по свойствам этой строки
                    this.Id_up = _selectedTypeUpak.id_upak;
                    this.Name_up = _selectedTypeUpak.nazvanie;
                    this.Price_up = _selectedTypeUpak.priceupak;
                }
                OnPropertyChanged(); // Уведомляем UI, что выбор изменился
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class Pokup
    {
        public int Id_Klient { get; set; }
        public string FirstName { get; set; }
        public string Adress { get; set; }
        public string Phone { get; set; }

        public override string ToString()
        {
            return $"{FirstName,-10} | {Phone,-10} -- {Adress,-10}"; // -20 и -15 для выравнивания влево
        }
    }

    public class Naimen
    {
        public int Id { get; set; }
        public string namecake { get; set; }
        public string category { get; set; }
        public string opisanie { get; set; }
        public decimal priceofone { get; set; }
        public string edprice { get; set; }

        public override string ToString()
        {
            return $"{namecake,-10} | {priceofone} за {edprice,-5}";
        }
    }

    public class ChangeIngrid
    {
        public int Id_Ingrid { get; set; }
        public string NameIngrid { get; set; }

        public override string ToString()
        {
            return $"{NameIngrid}";
        }
    }
    /// <summary>
    /// Логика взаимодействия для CreateZak.xaml
    /// </summary>
    public partial class CreateZak : MetroWindow
    {
        private bool _redZak;
        public event Action DataUpdatedZak;
        private DateTime dateZak;
        private decimal AllPrice = 0;
        private int id_ingrid;
        private int id_recip;
        private decimal ingrid_price;
        private int pokup_id;
        // Коллекция строк декора. ItemsControl будет следить за ней.
        // Если добавишь в неё элемент — в UI появится новая строка.
        public ObservableCollection<Decor> DecorsCollection { get; set; } = new ObservableCollection<Decor>();
        public ObservableCollection<Upak> UpsCollection { get; set; } = new ObservableCollection<Upak>();

        public CreateZak(DateTime dateZakaza, bool redZak = false)
        {
            InitializeComponent();

            dateZak = dateZakaza;
            // Вывод для пользователя (просто текст)
            LabelDate.Text = dateZak.ToString("dd.MM.yyyy HH:mm");

            LoadPokup();
            LoadNaimen();

            this._redZak = redZak;
            if (_redZak) { komment.Text = "СРОЧНО/ВНЕ ОЧЕРЕДИ"; }
            ;

            // Привязываем ItemsControl к нашей коллекции
            DecorsList.ItemsSource = DecorsCollection;
            // ПРАВИЛЬНО: Обращаемся к коллекции (DecorsCollection), а не к классу (Decor)
            DecorsCollection.Add(new Decor());

            // Привязываем ItemsControl к нашей коллекции
            UpList.ItemsSource = UpsCollection;
            // Обращаемся к коллекции 
            UpsCollection.Add(new Upak());
        }

        private void ChangeTime_Click(object sender, RoutedEventArgs e)
        {
            EditDateBox.Text = dateZak.ToString("dd.MM.yyyy HH:mm");
            ChangeTime.Visibility = Visibility.Collapsed;
            komment.Visibility = Visibility.Collapsed;
            kommb.Visibility = Visibility.Collapsed;
            // Используем универсальный метод:
            Metods.AnimatePanel(EditPanel, EditPanelTransform, show: true);
        }

        // Сохранить изменения
        private void SaveTime_Click(object sender, RoutedEventArgs e)
        {
            if (DateTime.TryParse(EditDateBox.Text, out DateTime newDate))
            {
                dateZak = newDate;
                LabelDate.Text = dateZak.ToString("dd.MM.yyyy HH:mm");
                CloseEdit();
            }
            else
            {
                MessageBox.Show("Неверный формат! Используйте: ДД.ММ.ГГГГ ЧЧ:ММ");
            }
            komment.Visibility = Visibility.Visible;
            kommb.Visibility = Visibility.Visible;
        }

        // Отмена
        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            CloseEdit();
        }

        private void CloseEdit()
        {
            // Используем универсальный метод:
            Metods.AnimatePanel(EditPanel, EditPanelTransform, show: false);
            komment.Visibility = Visibility.Visible;
            kommb.Visibility = Visibility.Visible;
            ChangeTime.Visibility = Visibility.Visible;
        }

        private void LoadPokup()
        {
            string query = "SELECT Id_Klient, FirstName, Adress, Phone FROM Klients";
            Metods.LoadCombo(query, Klients, reader =>
            new Pokup
            {
                Id_Klient = Convert.ToInt32(reader["Id_Klient"]),
                FirstName = reader["FirstName"].ToString(),
                Adress = reader["Adress"].ToString(),
                Phone = reader["Phone"].ToString()
            });
        }

        private void LoadNaimen()
        {
            string query = "SELECT * FROM recipes";
            Metods.LoadCombo(query, NaimCombo, reader =>
            new Naimen
            {
                Id = Convert.ToInt32(reader["id_recipes"]),
                namecake = reader["namecake"].ToString(),
                category = reader["category"].ToString(),
                opisanie = reader["opisanie"].ToString(),
                priceofone = Convert.ToDecimal(reader["priceofone"]),
                edprice = reader["edprice"].ToString()
            });
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            New.Visibility = Visibility.Collapsed;
            // Используем универсальный метод:
            Metods.AnimatePanel(EditKli, EditPanelTransform, show: true);
        }

        private void SaveKli_Click(object sender, RoutedEventArgs e)
        {
            string fname = Name.Text;
            string adress = Adr.Text;
            string digits = Tel.Text;

            //Очищаем всё, кроме цифр
            string onlyDigits = Regex.Replace(digits, @"[^0-9]", "");
            if (onlyDigits.Length != 12)
            {
                MessageBox.Show("Проверьте номер телефона", "ОШИБКА!", MessageBoxButton.OK, MessageBoxImage.Error);
                //Tel.Clear();
                return; // Выход из метода, если ввод некорректный
            }
            // Собираем итоговый формат для БД
            string phoneForDB = "+" + onlyDigits;
            MessageBox.Show($"{fname}, {phoneForDB}, {adress}");

            using (NpgsqlConnection connection = Metods.Source.OpenConnection())
            {
                //connection.Open();
                string query = @"INSERT INTO klients(firstname, phone, adress)
	                                VALUES (@fname, @phoneForDB, @adress);";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@fname", fname);
                    command.Parameters.AddWithValue("@phoneForDB", phoneForDB);
                    command.Parameters.AddWithValue("@adress", adress);
                    command.ExecuteNonQuery();
                }
            }
            Klients.Items.Clear();
            Name.Clear();
            Tel.Clear();
            Adr.Clear();
            LoadPokup();
            Metods.AnimatePanel(EditKli, EditPanelTransform, show: false);
            Klients.SelectedIndex = Klients.Items.Count - 1;
            New.Visibility = Visibility.Visible;
        }

        private void CancelKli_Click(object sender, RoutedEventArgs e)
        {
            Name.Clear();
            Adr.Clear();
            Tel.Clear();
            // Используем универсальный метод:
            Metods.AnimatePanel(EditKli, EditPanelTransform, show: false);

            New.Visibility = Visibility.Visible;
        }

        private void NaimCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VesTorta.Clear();
            //int id_recip = 0;
            BorderOpis.Visibility = Visibility.Visible;
            // Проверяем, выбран ли элемент, чтобы не вылетело по ошибке
            if (NaimCombo.SelectedItem is Naimen selectedNaimen)
            {
                Opis.Visibility = Visibility.Visible;
                Opis.Text = selectedNaimen.opisanie;
            }
            Naimen selectedNaim = (Naimen)NaimCombo.SelectedItem;
            id_recip = selectedNaim.Id;

            Grid_Ingr.Visibility = Visibility.Visible;

            using (NpgsqlConnection connection = Metods.Source.OpenConnection())
            {
                string query = @"SELECT 
sostrecipes.id_recip,
ingridients.id_ingrid,
ingridients.nameingrid AS ""Наименование"",
sostrecipes.kolich AS ""Необходимо"",
ingridients.ostatok AS ""В остатке"",
(ingridients.ostatok - sostrecipes.kolich) AS ""Разница"",
ingridients.edizmer::text AS ""Ед Измер"",
sostrecipes.kolich AS ""base_kolich""
FROM sostrecipes
INNER JOIN 
ingridients ON sostrecipes.id_ingr = ingridients.id_ingrid
WHERE id_recip = @id_recip;";

                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@id_recip", id_recip);
                NpgsqlDataAdapter dataAdapterIng = new NpgsqlDataAdapter(command);
                DataTable dataTableIng = new DataTable();
                dataAdapterIng.Fill(dataTableIng);
                DataGrid_Ingr.ItemsSource = dataTableIng.DefaultView;
            }
            DecBord.Visibility = Visibility.Visible;
            DecBox.Visibility = Visibility.Visible;
            UpBord.Visibility = Visibility.Visible;
            UpBox.Visibility = Visibility.Visible;
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string headerName = e.Column.Header.ToString();

            // Проверяем, является ли текущий столбец тем, который мы хотим скрыть
            if (headerName == "id_recip" || headerName == "base_kolich" || headerName == "id_ingrid")
            {
                // Если да, устанавливаем его видимость в Скрыто (Collapsed)
                e.Column.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DataUpdatedZak?.Invoke();
            AllPrice = 0;
        }

        private void VesTorta_KeyUp(object sender, KeyEventArgs e)
        {
            // Используем TryParse, чтобы не было вылетов при пустом поле или буквах
            if (!decimal.TryParse(VesTorta.Text, out decimal ves)) return;

            DataView dv = (DataView)DataGrid_Ingr.ItemsSource;
            if (dv == null) return;

            foreach (DataRowView rowView in dv)
            {
                DataRow row = rowView.Row;

                // Всегда считаем от базового значения (рецепт на 1 единицу)
                decimal baseCount = Convert.ToDecimal(row["base_kolich"]);

                // Рассчитываем новое значение
                decimal newcount = ves * baseCount;

                // Записываем обратно в DataTable
                row["Необходимо"] = newcount;

                // Обновляем разницу
                decimal ostatok = Convert.ToDecimal(row["В остатке"]);
                row["Разница"] = ostatok - newcount;

                //rowView.EndEdit();
            }
        }

        private void Change_Click(object sender, RoutedEventArgs e)
        {
            System.Data.DataRowView selectedRow = (System.Data.DataRowView)DataGrid_Ingr.SelectedItem;
            id_ingrid = Convert.ToInt32(selectedRow["Id_Ingrid"]);
            ChangeRecipPopup.IsOpen = true;
            EditBox.Items.Clear();
            EditBox.Focus();
            LoadComboBox();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string newIng = null;
            int newIdIngr = 0;
            int newOst = 0;
            if (EditBox.SelectedItem == null)
            {
                MessageBox.Show("Вы ничего не выбрали на замену! Ингредиент остался прежним.", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Error);
                ChangeRecipPopup.IsOpen = false;
                return;
            }
            // Приводим SelectedItem к типу ChangeIngrid и забираем свойство Id_Ingrid
            if (EditBox.SelectedItem is ChangeIngrid selectedIngrid)
            {
                newIdIngr = selectedIngrid.Id_Ingrid;
                newIng = selectedIngrid.NameIngrid;
                string query = $"SELECT ostatok FROM ingridients WHERE id_ingrid = {newIdIngr}";
                newOst = GetOstFromDb(query);
                //MessageBox.Show($"{newIdIngr} - {newIng}");
            }
            if (DataGrid_Ingr.SelectedItem is DataRowView row) //обновления ингр. в таблице
            {
                row["Наименование"] = newIng;
                row["Id_ingrid"] = newIdIngr;
                row["В остатке"] = newOst;
            }
            //UpdatePrice();
            ChangeRecipPopup.IsOpen = false;
            EditBox.SelectedIndex = 0;
            // Снимаем выделение со строки
            DataGrid_Ingr.SelectedItem = null;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ChangeRecipPopup.IsOpen = false;
            EditBox.SelectedIndex = 0;
        }

        private void LoadComboBox()
        {
            string query = "SELECT Id_ingrid, nameingrid FROM ingridients;";
            Metods.LoadCombo(query, EditBox, reader =>
            new ChangeIngrid
            {
                Id_Ingrid = Convert.ToInt32(reader["Id_ingrid"]),
                NameIngrid = reader["nameingrid"].ToString(),
            });
        }

        //обновление себестоимости при смене ингр
        private void UpdatePrice()
        {
            decimal price = 0;

            // Проходим по всем строкам DataTable, привязанной к DataGrid
            foreach (var item in DataGrid_Ingr.Items)
            {
                // 2. ОБЯЗАТЕЛЬНАЯ ПРОВЕРКА для WPF: пропускаем строку ввода новой записи
                if (item == System.Windows.Data.CollectionView.NewItemPlaceholder) continue;

                // 3. Приводим элемент к DataRowView
                DataRowView row = (DataRowView)item;

                // Получаем значения (лучше по именам колонок, чем по индексам)
                int id = Convert.ToInt32(row["Id_ingrid"]);
                int kolich = Convert.ToInt32(row["Необходимо"]);

                string query = $"SELECT priceingrid FROM ingridients WHERE Id_ingrid = {id}";
                decimal priceIngr = GetPriceFromDb(query);

                //вычитание из таблицы ингр
                string queryIngrMinus = $"SELECT ostatok FROM ingridients WHERE Id_ingrid = {id}";
                int ost = GetOstFromDb(queryIngrMinus);
                int newOts = ost - kolich;
                string queryNewOst = $"UPDATE ingridients SET ostatok = {newOts} WHERE Id_ingrid = {id}";
                Metods.ExecuteNonQuery(queryNewOst);

                price += Math.Round(priceIngr * kolich, 2, MidpointRounding.AwayFromZero);
            }
            // Выводим результат
            //MessageBox.Show($"Вы изменили состав рецепта! Новая себестоимость = {price} руб.", "Внимание!", MessageBoxButton.OK, MessageBoxImage.Warning );

            AllPrice += price;
            ingrid_price = price;
            //MessageBox.Show($"{AllPrice}");
            sebest.Text = price.ToString();
        }

        //получение стоимости ингр
        private decimal GetPriceFromDb(string query)
        {
            // Пример запроса. Подставь свои имена таблиц и колонок.
            //string query = $"SELECT priceingrid FROM ingridients WHERE Id_ingrid = {id}";

            // Используй свой класс Metods для получения одного значения (Scalar)
            object result = Metods.ExecuteScalar(query);
            return result != null ? Convert.ToDecimal(result) : 0;
        }

        //получение остатка ингр
        private int GetOstFromDb(string query)
        {
            object result = Metods.ExecuteScalar(query);
            return result != null ? Convert.ToInt32(result) : 0;
        }

        private void ComboDecor_Loaded(object sender, RoutedEventArgs e)
        {
            // sender — это именно тот ComboBox, который только что отрисовался в строке
            if (sender is ComboBox combo)
            {
                // 1. Добавляем цену в запрос (обязательно!)
                string query = "SELECT id_decor, name_decor, ostatok, price_decor FROM type_decor;";

                Metods.LoadCombo(query, combo, reader =>
                    new TypeDecor
                    {
                        Id_decor = Convert.ToInt32(reader["id_decor"]),
                        name_decor = reader["name_decor"].ToString(),
                        ostatok = Convert.ToInt32(reader["ostatok"]),
                        // 2. Сразу сохраняем цену в объект
                        price_decor = Convert.ToDecimal(reader["price_decor"])
                    });
            }
        }

        //метод суммирования для декора
        private void CalculateTotalDecor()
        {
            decimal total = 0;

            foreach (var item in DecorsCollection)
            {
                // Проверяем, выбрано ли что-то в комбобоксе и введено ли количество
                if (item.SelectedType != null && decimal.TryParse(item.Kolich, out decimal count))
                {
                    // Просто берем цену, которая уже загружена в SelectedType
                    total += item.SelectedType.price_decor * count;
                    int id_dec = item.SelectedType.Id_decor;
                    //вычитание
                    //string queryOst = $"UPDATE type_decor SET ostatok = ostatok - {count} WHERE id_decor = {id_dec};";
                    //Metods.ExecuteNonQuery(queryOst);
                }
            }
            // Выводим результат (например, в какой-то TextBlock)
            //MessageBox.Show($"Стоимость декора {total}");
            //ResultDecorLabel.Text = $"Декор на сумму: {total} руб.";
            AllPrice += total;
            //MessageBox.Show($"{AllPrice}");
            dec.Text = total.ToString();
        }

        //метод суммирования для упаковки
        private void CalculateTotalUpak()
        {
            decimal totalup = 0;

            foreach (var item in UpsCollection)
            {
                // Проверяем, выбрано ли что-то в комбобоксе и введено ли количество
                if (item.SelectedTypeUpak != null && int.TryParse(item.Kolich_up, out int count))
                {
                    // Просто берем цену, которая уже загружена в SelectedType
                    totalup += item.SelectedTypeUpak.priceupak * count;
                    int id_upak = item.SelectedTypeUpak.id_upak;
                    //вычитание
                    //string queryOst = $"UPDATE typeupak SET ostatok = ostatok - {count} WHERE id_upak = {id_upak};";
                    //Metods.ExecuteNonQuery(queryOst);
                }
            }
            AllPrice += totalup;
            up.Text = totalup.ToString();
        }

        // Обработчик кнопки "✕" (удаление)
        private void RemoveDec_Click(object sender, RoutedEventArgs e)
        {
            // В XAML у кнопки должно быть Tag="{Binding}"
            if (sender is Button btn && btn.Tag is Decor itemToRemove)
            {
                DecorsCollection.Remove(itemToRemove);
            }
        }
        // Обработчик кнопки "+"
        private void AddDec_Click(object sender, RoutedEventArgs e)
        {
            //CalculateTotalDecor();
            DecorsCollection.Add(new Decor()); // Просто добавляем новый объект, UI сам создаст строку
        }

        private void ComboUp_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox combo)
            {
                // 1. Добавляем цену в запрос (обязательно!)
                //string query = "SELECT id_upak, nazvanie, ostatok, priceupak FROM typeupak;";

                Metods.LoadCombo(Metods.queryUpak, combo, reader =>
                    new TypeUpak
                    {
                        id_upak = Convert.ToInt32(reader["id_upak"]),
                        nazvanie = reader["Наименование"].ToString(),
                        ostatok = Convert.ToInt32(reader["Остаток"]),
                        // 2. Сразу сохраняем цену в объект
                        priceupak = Convert.ToDecimal(reader["Стоимость за единицу"])
                    });
            }
        }

        private void RemoveUp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Upak itemToRemove)
            {
                UpsCollection.Remove(itemToRemove);
            }
        }

        private void AddUp_Click(object sender, RoutedEventArgs e)
        {
            //CalculateTotalUpak();
            UpsCollection.Add(new Upak());
        }

        //расчет стоимости работы от времени приготовления
        private void WorkPrice()
        {
            decimal wp = 0;
            string query = $"SELECT (recipes.time_create * naklad.price_nakl) FROM naklad, recipes WHERE id_nakl = 10 AND id_recipes = {id_recip};";
            // Используй свой класс Metods для получения одного значения (Scalar)
            object result = Metods.ExecuteScalar(query);
            wp = Math.Round(Convert.ToDecimal(result), 2, MidpointRounding.AwayFromZero);
            AllPrice += wp;
            //MessageBox.Show($"{wp} / {AllPrice}");
            time.Text = wp.ToString();
        }

        private void NaklZatr()
        {
            //decimal nakl = ingrid_price * 0.2m; // 20% от стоимости ингр
            decimal nakl = Math.Round(Convert.ToDecimal(ingrid_price * 0.2m), 2, MidpointRounding.AwayFromZero);
            AllPrice += nakl;
            //MessageBox.Show($"{nakl} / {AllPrice}");
            naklad.Text = nakl.ToString();
        }

        private void CreateNewZak()
        {
            try
            {
                using (NpgsqlConnection conn = Metods.Source.OpenConnection())
                using (var transaction = conn.BeginTransaction()) // Начинаем транзакцию
                {
                    int klient = (int)Klients.SelectedValue;
                    //для табл Заказы
                    DateTime date_zakaza = DateTime.Today;
                    DateTime date_ispoln = dateZak;
                    //статус заказа "Новый" по умолчанию
                    //string predopl = pred.Text.Replace('.', ',');
                    decimal predoplata = Convert.ToDecimal(pred.Text.Replace('.', ','));
                    //поля для заметки
                    string komm = komment.Text;

                    //для табл СостЗак
                    int id_zak;
                    int id_re = (int)NaimCombo.SelectedValue;
                    decimal ves = Convert.ToDecimal(VesTorta.Text);


                    string queryNewZak = @"INSERT INTO zakazy(id_klient, date_zakaza, date_ispoln, predoplata, zametka)
	                                VALUES (@klient, @date_zakaza, @date_ispoln, @predoplata, @komm) RETURNING id_zakaz;";
                    using (NpgsqlCommand comm = new NpgsqlCommand(queryNewZak, conn))
                    {
                        comm.Parameters.AddWithValue("@klient", klient);
                        comm.Parameters.AddWithValue("@date_zakaza", date_zakaza);
                        comm.Parameters.AddWithValue("@date_ispoln", date_ispoln);
                        comm.Parameters.AddWithValue("@predoplata", predoplata);
                        comm.Parameters.AddWithValue("@komm", komm);
                        //comm.ExecuteNonQuery(); НЕ ТРЕБУЕТСЯ, если надо что-то вернуть
                        id_zak = Convert.ToInt32(comm.ExecuteScalar());
                    }

                    string queryNewSZ = @"INSERT INTO sostavzakaza(id_zak, id_recip, finishprice, count_zak)
	                                        VALUES (@id_zak, @id_recip, @finishprice, @count_zak) RETURNING id_sost;";
                    int id_sost;
                    using (NpgsqlCommand comm = new NpgsqlCommand(queryNewSZ, conn))
                    {
                        comm.Parameters.AddWithValue("@id_zak", id_zak);
                        comm.Parameters.AddWithValue("@id_recip", id_re);
                        comm.Parameters.AddWithValue("@finishprice", AllPrice);
                        comm.Parameters.AddWithValue("@count_zak", ves);
                        id_sost = Convert.ToInt32(comm.ExecuteScalar());
                    }

                    // Проходим циклом по коллекции упаковок и сохраняем каждую
                    foreach (var item in UpsCollection)
                    {
                        // Проверяем, что упаковка выбрана и количество введено корректно
                        if (item.SelectedTypeUpak != null && int.TryParse(item.Kolich_up, out int count))
                        {
                            int id_upak = item.SelectedTypeUpak.id_upak;
                            // Запрос на вставку в связующую таблицу
                            // Поля: id_zakaza (внешний ключ), id_upak (какая упаковка), kolichestvo (сколько)
                            string queryInsertUpak = @"INSERT INTO sostav_upakovki (id_sost_zak, id_upak, kolich) 
                                                        VALUES (@id_sost, @id_upak, @kol);";
                            using (NpgsqlCommand comm = new NpgsqlCommand(queryInsertUpak, conn))
                            {
                                comm.Parameters.AddWithValue("@id_sost", id_sost);
                                comm.Parameters.AddWithValue("@id_upak", id_upak);
                                comm.Parameters.AddWithValue("@kol", count);
                                comm.ExecuteNonQuery();
                            }
                            //вычитание
                            string queryOst = @"UPDATE typeupak SET ostatok = ostatok - @kol WHERE id_upak = @id_upak;";
                            using (NpgsqlCommand comm = new NpgsqlCommand(queryOst, conn))
                            {
                                comm.Parameters.AddWithValue("@kol", count);
                                comm.Parameters.AddWithValue("@id_upak", id_upak);
                                comm.ExecuteNonQuery();
                            }
                        }
                    }

                    // Проходим циклом по коллекции декора
                    foreach (var item in DecorsCollection)
                    {
                        // Проверяем, что упаковка выбрана и количество введено корректно
                        if (item.SelectedType != null && int.TryParse(item.Kolich, out int count))
                        {
                            int id_dec = item.SelectedType.Id_decor;
                            string queryInsertDecor = @"INSERT INTO sost_decor (id_sost_zak, id_decor, kolich) 
                                                            VALUES (@id_sost, @id_dec, @kol);";
                            using (NpgsqlCommand comm = new NpgsqlCommand(queryInsertDecor, conn))
                            {
                                comm.Parameters.AddWithValue("@id_sost", id_sost);
                                comm.Parameters.AddWithValue("@id_dec", id_dec);
                                comm.Parameters.AddWithValue("@kol", count);
                                comm.ExecuteNonQuery();
                            }
                            //вычитание
                            string queryOst = @"UPDATE type_decor SET ostatok = ostatok - @kol WHERE id_decor = @id_dec;";
                            using (NpgsqlCommand comm = new NpgsqlCommand(queryOst, conn))
                            {
                                comm.Parameters.AddWithValue("@kol", count);
                                comm.Parameters.AddWithValue("@id_dec", id_dec);
                                comm.ExecuteNonQuery();
                            }
                        }
                    }
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
            AllPrice = 0;
        }

        private void all_Click(object sender, RoutedEventArgs e)
        {
            UpdatePrice(); //себестоим ингр + вычитание ингр из таблицы склада
            CalculateTotalDecor(); //стоим декора
            CalculateTotalUpak(); //стоим упак
            WorkPrice(); //часовая ставка
            NaklZatr(); //накладные расходы 20%
            itog.Text = AllPrice.ToString();

            CreateNewZak();
            DataUpdatedZak?.Invoke();
        }
    }
}
//string komment = "СРОЧНО/ВНЕ ОЧЕРЕДИ"; }; //ДОБАВИТЬ В КОММЕНТ К ЗАКАЗУ, ЕСЛИ ВЫБРАН КРАСНЫЙ ИНТЕРВАЛ!!!!!!