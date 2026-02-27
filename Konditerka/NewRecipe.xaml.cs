using MahApps.Metro.Controls;
using Npgsql;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Konditerka.Metods;

namespace Konditerka
{
    /// <summary>
    /// Логика взаимодействия для NewRecipe.xaml
    /// </summary>
    public partial class NewRecipe : MetroWindow
    {
        // Специальная коллекция, которая уведомляет UI об изменениях
        public ObservableCollection<Ingredient> Ingredients { get; set; }
        private List<string> ingr = new List<string>(); // Для хранения ингр
        DataTable DT = new DataTable();
        public event Action DataUpdatedNewRecipe;

        public class Ingredient : INotifyPropertyChanged
        {
            private string _name;
            private int _kolich;
            private string _edIzm;
            private decimal _price;
            private int _id; // Поле для хранения ID из базы

            public int Id { get => _id; set { _id = value; OnPropertyChanged(); } }
            public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
            public int Kolich { get => _kolich; set { _kolich = value; OnPropertyChanged(); } }
            public string EdIzm { get => _edIzm; set { _edIzm = value; OnPropertyChanged(); } }
            public decimal Price
            {
                get => _price; set { _price = value; OnPropertyChanged(); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public NewRecipe()
        {
            InitializeComponent();

            // Инициализируем список и привязываем его к ItemsControl
            Ingredients = new ObservableCollection<Ingredient>();
            IngredientsList.ItemsSource = Ingredients;

            // Добавим одну пустую строку сразу
            Ingredients.Add(new Ingredient());
        }

        // Обработчик кнопки "+"
        private void AddIngredient_Click(object sender, RoutedEventArgs e)
        {
            Ingredients.Add(new Ingredient());
        }

        // Обработчик кнопки "✕" (удаление)
        private void RemoveIngredient_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var ingredient = button?.Tag as Ingredient;
            if (ingredient != null)
            {
                Ingredients.Remove(ingredient);
            }
        }

        //ПОИСК ингр
        private void TBoxIngr_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tbox = sender as TextBox;
            if (tbox == null) return;

            // Ищем родительский Grid, в котором лежит и этот TextBox, и нужный ListBox
            Grid parent = FindParent<Grid>(tbox);

            // Теперь ищем ListBox внутри этого конкретного Grid
            ListBox list = FindChild<ListBox>(parent, "List");

            string searchText = tbox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                if (list != null) list.Visibility = Visibility.Collapsed;
                return;
            }

            // Ищем в БД
            SearchIngr(searchText);

            // Берем данные из DT
            var filter = DT.AsEnumerable()
                .Select(row => row.Field<string>("nameingrid"))
                .Where(name => name != null)
                .Distinct()
                .ToList();

            if (list != null)
            {
                list.ItemsSource = filter;
                list.Visibility = filter.Any() ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        // Модифицированный метод поиска (принимает строку)
        private void SearchIngr(string searchText)
        {
            using (NpgsqlConnection connection = Metods.Source.OpenConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(Metods.queryIngrNewRec, connection);
                command.Parameters.AddWithValue("nameingrid", "%" + searchText + "%");

                NpgsqlDataAdapter dataAdapterZakupIngr = new NpgsqlDataAdapter(command);
                DT.Clear();
                dataAdapterZakupIngr.Fill(DT);
            }
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox list = sender as ListBox;
            // Проверка, что кликнули не по пустому месту
            if (list == null || list.SelectedItem == null) return;

            // Поднимаемся к общему Grid
            Grid parent = FindParent<Grid>(list);

            // Ищем TextBox "над собой" в этом же Grid
            TextBox tbox = FindChild<TextBox>(parent, "TBoxIngr");

            if (tbox != null)
            {
                string selectedName = list.SelectedItem.ToString();

                // Получаем доступ к самому объекту данных этой строки
                Ingredient ingredientEntry = tbox.DataContext as Ingredient;

                if (ingredientEntry != null)
                {
                    // Отключаем событие, чтобы не вызвать бесконечный поиск
                    tbox.TextChanged -= TBoxIngr_TextChanged;

                    // Обновляем данные в объекте (Binding сам обновит UI)
                    ingredientEntry.Name = selectedName;

                    tbox.Text = selectedName; // Явно прописываем текст в контрол

                    // Ищем единицу измерения в DataTable (DT) по выбранному имени
                    var row = DT.AsEnumerable()
                        .FirstOrDefault(r => r.Field<string>("nameingrid") == selectedName);

                    if (row != null)
                    {
                        ingredientEntry.Id = Convert.ToInt32(row["id_ingrid"]); // Сохраняем ID навсегда
                        ingredientEntry.EdIzm = Convert.ToString(row.Field<object>("edizmer"));
                        // Безопасное приведение цены
                        if (row["priceingrid"] != DBNull.Value)
                            ingredientEntry.Price = Convert.ToDecimal(row["priceingrid"]);
                    }

                    // Прячем список и возвращаем событие
                    list.Visibility = Visibility.Collapsed;
                    tbox.TextChanged += TBoxIngr_TextChanged;
                }
            }
        }
        //Поиск родителя (Grid)
        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }
        private T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild && (child as FrameworkElement).Name == childName) return tChild;
                var result = FindChild<T>(child, childName);
                if (result != null) return result;
            }
            return null;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (NpgsqlConnection connection = Metods.Source.OpenConnection())
                {
                    // 1. Валидация ДО начала транзакции (чтобы не открывать её зря)
                    if (string.IsNullOrWhiteSpace(NameCake.Text))
                    {
                        MessageBox.Show("Введите название!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (Category.SelectedItem == null)
                    {
                        MessageBox.Show("Выберите категорию!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(VesTB.Text))
                    {
                        MessageBox.Show("Не указан вес!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Начинаем транзакцию
                    using (var transaction = connection.BeginTransaction())
                    {
                        string queryNewRec = @"INSERT INTO recipes (namecake, category, opisanie, instruction, priceofone, edprice, time_create) 
                                VALUES (@namecake, @category, @opisanie, @instruction, @priceofone, @edprice, @time_create) RETURNING id_recipes;";

                        string namecake = NameCake.Text;
                        string catname = (Category.SelectedItem as ComboBoxItem)?.Content.ToString();
                        Category category = (Category)Enum.Parse(typeof(Category), catname);

                        EdPrice edprice;
                        if (catname == "Торт") { edprice = EdPrice.кг; }
                        else { edprice = EdPrice.шт; }

                        string opisanie = Opisanie.Text;
                        string instruction = Instruct.Text;
                        //string timetext = Time.Text.Replace('.', ',');
                        decimal time_create = Convert.ToDecimal(Time.Text.Replace('.', ','));
                        decimal ves = Convert.ToDecimal(VesTB.Text.Replace('.', ','));
                        decimal totalRecipePrice = Ingredients.Sum(i => i.Price * i.Kolich) / ves; //стоимость за 1 кг

                        decimal priceofone = Math.Round(totalRecipePrice, 2, MidpointRounding.AwayFromZero);

                        int newRecipeId;
                        using (NpgsqlCommand command = new NpgsqlCommand(queryNewRec, connection))
                        {
                            command.Parameters.AddWithValue("@namecake", namecake);
                            //command.Parameters.AddWithValue("@category", category);
                            // ЯВНОЕ УКАЗАНИЕ ТИПА:
                            command.Parameters.Add(new NpgsqlParameter("@category", NpgsqlTypes.NpgsqlDbType.Unknown)
                            {
                                Value = category.ToString(), // Отправляем строку 'Торт'
                                DataTypeName = "recipes_category" // <-- Говорим PG, что это за тип
                            });
                            command.Parameters.AddWithValue("@opisanie", opisanie);
                            command.Parameters.AddWithValue("@instruction", instruction);
                            command.Parameters.AddWithValue("@priceofone", priceofone);
                            //command.Parameters.AddWithValue("@edprice", edprice);
                            // То же самое для edprice. Вам нужно узнать точное имя этого типа в БД.
                            command.Parameters.Add(new NpgsqlParameter("@edprice", NpgsqlTypes.NpgsqlDbType.Unknown)
                            {
                                Value = edprice.ToString(), // Отправляем строку 'кг'
                                DataTypeName = "unit_recipes" // <-- ПРОВЕРЬТЕ ЭТО ИМЯ ВАШЕЙ БД
                            });
                            command.Parameters.AddWithValue("@time_create", time_create);

                            newRecipeId = Convert.ToInt32(command.ExecuteScalar());
                        }

                        string querySostRecNew = @"INSERT INTO sostrecipes (id_recip, id_ingr, kolich)
                                            VALUES (@id_recip, @id_ingr, @kolich)";

                        foreach (var item in Ingredients)
                        {
                            // Пропускаем пустые строки или ингредиенты без ID
                            if (item.Id == 0 || item.Kolich <= 0) continue;
                            // Принудительно работаем с decimal для точности, затем округляем вверх
                            int kol = (int)Math.Ceiling((decimal)item.Kolich / ves);

                            using (NpgsqlCommand cmdIngr = new NpgsqlCommand(querySostRecNew, connection))
                            {
                                cmdIngr.Parameters.AddWithValue("@id_recip", newRecipeId);
                                cmdIngr.Parameters.AddWithValue("@id_ingr", item.Id);
                                cmdIngr.Parameters.AddWithValue("@kolich", kol);
                                cmdIngr.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        MessageBox.Show($"Рецепт \"{namecake}\" успешно сохранен!");
                        this.Close();
                        DataUpdatedNewRecipe?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"{ex.Message}");
            }
        }
    }
}
