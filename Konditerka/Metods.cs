using Npgsql;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Konditerka
{
    public class Metods
    {
        public static string ConnString { get; set; }
        public static string querySostZak { get; set; }
        public static string queryOrderDetails { get; set; }
        public static string queryKlients { get; set; }
        public static string queryZakazy { get; set; }
        public static string queryRec { get; set; }
        public static string queryIngr { get; set; }
        public static string querySostRec { get; set; }
        public static string queryUpak { get; set; }
        public static string queryZakup { get; set; }
        public static string queryZakupIngr { get; set; }
        public static string queryDecor { get; set; }
        public static string queryDecorSearch { get; set; }
        public static string queryUpakSearch { get; set; }
        public static string queryIngrNewRec { get; set; }
        public static string queryAddPrice { get; set; }
        public static string queryNaklad { get; set; }

        // Главная точка доступа к базе
        public static NpgsqlDataSource Source { get; }

        // Это C# представление вашего ENUM из PostgreSQL
        public enum IngridEd
        {
            //значения, как они указаны в БД:
            кг,
            шт,
            л,
            мл,
            гр
        }

        public enum StatusZak
        {
            Новый,
            В_работе,
            Готов_к_выдаче,
            Выдан,
            Отменен
        }

        public enum Category
        {
            Торт,
            Эклер,
            Пирожное,
            Капкейк
        }

        public enum EdPrice
        {
            кг,
            шт
        }

        //строка подключения к БД и указание расположений
        static Metods()
        {
            ConnString = "Host=localhost;Port=5432;Username=postgres;Password=1;Database=Konditerka;";

            //для получения значений Enum из БД
            var builder = new NpgsqlDataSourceBuilder(ConnString);
            // Регистрируем ВСЕ ваши Enum'ы здесь
            builder.MapEnum<IngridEd>();
            builder.MapEnum<StatusZak>();
            builder.MapEnum<Category>();
            builder.MapEnum<EdPrice>();
            Source = builder.Build();


            querySostZak = @"SELECT 
    sz.Id_zak AS ""Номер заказа"", 
    r.NameCake AS ""Изделие"", 
	string_agg(sz.Count_zak::text || ' ' || r.edprice::text, ', ') AS ""Количество"",
    COALESCE(pack.nazvania_upakovok, 'Нет упаковки') AS ""Упаковка"",
    COALESCE(decor.nazvania_decorov, 'Нет декора') AS ""Декорирование"",
	sz.FinishPrice AS ""Общая стоимость""
FROM 
    SostavZakaza sz
INNER JOIN
    Recipes r ON sz.Id_recip = r.Id_recipes
LEFT JOIN (
    -- Подзапрос для расчета упаковок
    SELECT 
        su.id_sost_zak,
        string_agg(tu.nazvanie, ',' || CHR(10)) AS nazvania_upakovok,
        sum(tu.priceupak) AS summa_upakovki
    FROM 
        sostav_upakovki su
    INNER JOIN typeupak tu ON su.id_upak = tu.id_upak
    GROUP BY su.id_sost_zak) pack ON sz.Id_sost = pack.id_sost_zak
LEFT JOIN (
    -- Подзапрос для расчета декоров
    SELECT 
        sd.id_sost_zak,
        string_agg(td.name_decor, ',' || CHR(10)) AS nazvania_decorov,
        sum(td.price_decor) AS summa_decorov
    FROM 
        sost_decor sd
    INNER JOIN type_decor td ON sd.id_decor = td.id_decor
    GROUP BY sd.id_sost_zak) decor ON sz.Id_sost = decor.id_sost_zak
	WHERE sz.Id_zak = @id_zak
	GROUP BY 
    sz.Id_sost, 
    sz.Id_zak, 
    r.NameCake, 
    sz.FinishPrice, 
    pack.nazvania_upakovok, 
    pack.summa_upakovki, 
    decor.nazvania_decorov, 
    decor.summa_decorov;";

            queryKlients = @"
        SELECT 
            Id_Klient AS ""ID клиента"",
    FirstName AS ""Имя"",
    LastName AS ""Фамилия"",
    Phone AS ""Номер телефона"",
	Adress AS ""Адрес"",
    Zametka AS ""Заметки""
FROM
Klients
        ORDER BY 
            Id_Klient ASC";

            queryZakazy = @"
SELECT Zakazy.Id_Zakaz AS ""Номер заказа"",
	Klients.FirstName AS ""Имя"",
	Zakazy.Date_zakaza AS ""Дата заказа"",
	Zakazy.Date_Ispoln AS ""Дата выполнения заказа"",
	Zakazy.Status::text AS ""Статус заказа"",
	Zakazy.Predoplata AS ""Предоплата"",
	Zakazy.Zametka AS ""Комментарии"",
Klients.phone AS ""Номер телефона""
FROM
Zakazy
INNER JOIN 
Klients ON Zakazy.Id_Klient = Klients.Id_Klient
ORDER BY
Zakazy.Id_Zakaz ASC";

            queryRec = @"
SELECT id_recipes,
    string_agg(category || '""' || NameCake || '""', ', ') AS ""Название"",
    Opisanie AS ""Описание"",
    Instruction AS ""Инструкция"",
    string_agg(priceofone::text || ' за ' || edprice, '') AS ""Цена, руб"",
time_create AS ""Время приготовления, часы""
FROM 
    Recipes
GROUP BY 
id_recipes;";

            queryIngr = @"SELECT Id_Ingrid,
	NameIngrid AS ""Ингредиент"",
Priceingrid AS ""Стоимость за единицу"",
Ostatok AS ""Остаток"",
EdIzmer::text AS ""Единицы измерения""	
FROM Ingridients
ORDER BY NameIngrid ASC";

            querySostRec = @"
SELECT SostRecipes.Id_sost,
	Recipes.NameCake AS ""Название изделия"",
	Ingridients.NameIngrid AS ""Ингредиент"",
	SostRecipes.Kolich AS ""Необходимое количество""
FROM SostRecipes
INNER JOIN 
Recipes ON SostRecipes.Id_recip = Recipes.Id_Recipes
INNER JOIN
Ingridients ON SostRecipes.Id_ingr = Ingridients.Id_Ingrid
WHERE id_recipes = @id_rec
ORDER BY Id_sost ASC";

            queryUpak = @"
SELECT Id_upak,
	Nazvanie AS ""Наименование"",
	PriceUpak AS ""Стоимость за единицу"",
    ostatok AS ""Остаток""
FROM TypeUpak
ORDER BY Id_upak ASC";

            queryZakup = @"
SELECT zakupka.id_zakup,
ingridients.nameingrid AS ""Название"",
zakupka.kolich_pokup AS ""Сколько куплено"",
zakupka.date_pok AS ""Когда куплено"",
zakupka.price_pokup AS ""За сколько куплено""
FROM zakupka
INNER JOIN
ingridients ON zakupka.id_ing = ingridients.id_ingrid
ORDER BY id_zakup DESC";

            queryZakupIngr = @"
SELECT id_ingrid, 
nameingrid, edizmer::text
    FROM ingridients
    WHERE nameingrid ILIKE @nameingrid";

            queryIngrNewRec = @"
SELECT id_ingrid, 
nameingrid, edizmer::text, priceingrid
    FROM ingridients
    WHERE nameingrid ILIKE @nameingrid";

            queryDecor = @"SELECT id_decor,
name_decor AS ""Название"",
price_decor AS ""Стоимость за единицу"",
ostatok AS ""Остаток""
FROM type_decor
ORDER BY id_decor ASC ";

            queryNaklad = @"SELECT id_nakl, name_nakl AS ""Наименование затраты"", price_nakl AS ""Стоимость отнесенная к 1 заказу"" FROM naklad;";
        }

        public static void LoadGrid(string query, DataGrid dataGrid)
        {
            try
            {
                using (NpgsqlConnection connection = Source.OpenConnection())
                {
                    NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);
                    dataGrid.Items.Clear();
                    dataGrid.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных таблицы {dataGrid}: {ex.Message}",
                                "Ошибка подключения",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                Console.WriteLine("{ ex.Message}");
            }
        }
        public static void LoadGridwithoutClear(string query, DataGrid dataGrid)
        {
            try
            {
                using (NpgsqlConnection connection = Source.OpenConnection())
                {
                    NpgsqlDataAdapter dataAdapter = new NpgsqlDataAdapter(query, connection);
                    DataTable dataTable = new DataTable();
                    dataAdapter.Fill(dataTable);
                    //dataGrid.Items.Clear();
                    dataGrid.ItemsSource = dataTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных таблицы: {ex.Message}",
                                "Ошибка подключения",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                Console.WriteLine("{ ex.Message}");
            }
        }

        //добавление НОВОГО ингр, если его нет в БД
        public static int GetCodeIngr(string name, IngridEd? edizmer = null)
        {
            // ИСПОЛЬЗУЕМ Source, который вы создали и настроили (в нем уже есть MapEnum)
            using (NpgsqlConnection connection = Source.OpenConnection())
            {
                string query = @"SELECT id_ingrid FROM ingridients WHERE nameingrid = @name";

                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@name", name);

                    // 3. Безопасное получение результата
                    var result = command.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                    // Проверяем, передали ли нам единицы измерения для создания
                    if (edizmer == null)
                    {
                        return -1; // Возвращаем -1 как сигнал, что нужно спросить ед.изм.
                    }
                    // Создаем новый
                    string queryNewIngr = @"INSERT INTO ingridients (nameingrid, edizmer, priceingrid, ostatok) 
                                    VALUES (@name, @edizmer, 0, 0) RETURNING id_ingrid;";
                    using (NpgsqlCommand cmdInsert = new NpgsqlCommand(queryNewIngr, connection))
                    {
                        cmdInsert.Parameters.AddWithValue("@name", name);
                        cmdInsert.Parameters.AddWithValue("@edizmer", edizmer.Value);
                        return Convert.ToInt32(cmdInsert.ExecuteScalar());
                    }
                }
            }
        }

        //добавление НОВОГО декор/упаковка
        public static int GetCodeD_U(string querySelect, string queryNew, string name)
        {
            // ИСПОЛЬЗУЕМ Source, который вы создали и настроили (в нем уже есть MapEnum)
            using (NpgsqlConnection connection = Source.OpenConnection())
            {
                //string query = @"SELECT id_ingrid FROM ingridients WHERE nameingrid = @name";

                using (NpgsqlCommand command = new NpgsqlCommand(querySelect, connection))
                {
                    command.Parameters.AddWithValue("@name", name);

                    // 3. Безопасное получение результата
                    var result = command.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                    else
                    {
                        //string queryNewIngr = @"INSERT INTO ingridients (nameingrid, edizmer, priceingrid, ostatok) 
                        //                       VALUES (@name, @edizmer, 0, 0) RETURNING id_ingrid;";
                        using (NpgsqlCommand cmdInsert = new NpgsqlCommand(queryNew, connection))
                        {
                            cmdInsert.Parameters.AddWithValue("@name", name);

                            return Convert.ToInt32(cmdInsert.ExecuteScalar());
                        }
                    }
                }
            }
        }

        //метод для пересчета себестоимости изделий
        public static void DataUpdatedPriceOfOne(int id_ingr)
        {
            using (NpgsqlConnection connection = Source.OpenConnection())
            {
                //Ищем рецепты, где он есть
                string recipes = @"SELECT id_recip FROM sostrecipes WHERE id_ingr = @id_ingr";

                List<int> affectedRecipes = new List<int>();
                using (NpgsqlCommand comm = new NpgsqlCommand(recipes, connection))
                {
                    comm.Parameters.AddWithValue("@id_ingr", id_ingr);
                    using (var reader = comm.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            //Вместо обращения по имени (строке), можно обращаться по индексу. Так как в SELECT всего одно поле, оно имеет индекс 0
                            affectedRecipes.Add(reader.GetInt32(0));
                        }
                    }
                }
                // ЦИКЛ: Пересчитываем каждый найденный рецепт
                foreach (int recipeId in affectedRecipes)
                {
                    // Вызываем SQL запрос, который делает UPDATE recipes SET priceofone = (сумма всего состава)
                    // где id_recipes = recipeId
                    CalculatePrice(recipeId, connection);
                }
            }
        }
        private decimal GetPriceFromDb(int id)
        {
            // Пример запроса. Подставь свои имена таблиц и колонок.
            string query = $"SELECT price FROM ingridients WHERE Id_ingrid = {id}";

            // Используй свой класс Metods для получения одного значения (Scalar)
            object result = Metods.ExecuteScalar(query);
            return result != null ? Convert.ToDecimal(result) : 0;
        }

        private static void CalculatePrice(int id, NpgsqlConnection conn)
        {
            string query = @"UPDATE recipes 
                                SET priceofone = ( 
                                    SELECT SUM(sostrecipes.kolich * ingridients.priceingrid)
                                    FROM sostrecipes JOIN ingridients ON sostrecipes.id_ingr = ingridients.id_ingrid
                                    WHERE sostrecipes.id_recip = @id )
                                WHERE id_recipes = @id";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        //выполняет SQL-запрос и возвращает значение из первой колонки первой строки результата
        public static object ExecuteScalar(string query)
        {
            using (NpgsqlConnection connection = Source.OpenConnection()) // твоя строка подключения
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                return command.ExecuteScalar(); // Возвращает один объект (цену, ID или count)
            }
        }
        public static object ExecuteNonQuery(string query)
        {
            using (NpgsqlConnection connection = Source.OpenConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                return command.ExecuteNonQuery();
            }
        }

        //метод для изменения остатков на складе
        public static void ChangeOst(TextBox tbox, string query, int id, Popup popup, DataGrid dataGrid)
        {
            if (tbox.Text == null) return;
            int newKolich = Convert.ToInt32(tbox.Text);
            using (NpgsqlConnection connection = Metods.Source.OpenConnection())
            {
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@newKolich", newKolich); // Новый остаток
                    command.Parameters.AddWithValue("@id", id);  // id_ингр
                    command.ExecuteNonQuery();
                }
            }
            popup.IsOpen = false;

            if (dataGrid.SelectedItem is DataRowView row)
            {
                row["Остаток"] = newKolich; // Укажи здесь точное имя колонки из DataTable
            }
            tbox.Clear();
        }


        /// <summary>
        /// Анимирует плавное появление/исчезновение элемента WPF.
        /// </summary>
        /// <param name="elementToAnimate">Элемент, который нужно показать/скрыть (например, StackPanel).</param>
        /// <param name="transformElement">RenderTransform (TranslateTransform) для движения.</param>
        /// <param name="show">True для появления, False для исчезновения.</param>
        public static void AnimatePanel(UIElement elementToAnimate, TranslateTransform transformElement, bool show)
        {
            if (show)
            {
                elementToAnimate.Visibility = Visibility.Visible;

                // Анимация появления: прозрачность от 0 до 1
                DoubleAnimation opacityAnim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
                // Анимация движения: X от -20 до 0
                DoubleAnimation moveAnim = new DoubleAnimation(-20, 0, TimeSpan.FromSeconds(0.3))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                elementToAnimate.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
                transformElement.BeginAnimation(TranslateTransform.XProperty, moveAnim);
            }
            else
            {
                // Анимация исчезновения
                DoubleAnimation opacityAnim = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
                // Как только анимация закончится, ставим Visibility = Collapsed
                opacityAnim.Completed += (s, e) => elementToAnimate.Visibility = Visibility.Collapsed;
                elementToAnimate.BeginAnimation(UIElement.OpacityProperty, opacityAnim);
            }
        }

        //заполнение комбобокса в соответствии с классом, определенным в форме
        public static void LoadCombo<T>(string query, ComboBox comboBox, Func<NpgsqlDataReader, T> createItem)
        {
            using (NpgsqlConnection conn = new NpgsqlConnection(ConnString))
            {
                conn.Open();

                using (NpgsqlCommand command = new NpgsqlCommand(query, conn))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            T item = createItem(reader); // Создаем объект типа T
                            comboBox.Items.Add(item); // Добавляем объект в ComboBox
                        }
                    }
                }
            }
        }
    }

    //динамическое перекрашивание строки прямо во время ввода веса 
    //конвертер
    public class DifferenceToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Безопасное приведение к double или decimal
            if (value != null && double.TryParse(value.ToString(), out double diff))
            {
                return diff < 0 ? new SolidColorBrush(Color.FromRgb(240, 113, 117)) : Brushes.Transparent;
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
