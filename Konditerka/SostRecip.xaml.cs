using Npgsql;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Konditerka
{
    /// <summary>
    /// Логика взаимодействия для SostRecip.xaml
    /// </summary>
    public partial class SostRecip : Page
    {
        // Конструктор теперь принимает ID или целую строку данных
        public SostRecip(DataRowView selectedRecipe)
        {
            InitializeComponent();
            // 1. Устанавливаем заголовок из данных
            SostRecHeader.Text = $"Состав: {selectedRecipe["Название"]}";

            // 2. Загружаем данные в DataGrid
            // Здесь вызывайте ваш метод загрузки состава, передав ID рецепта
            LoadSostav(Convert.ToInt32(selectedRecipe["id_recipes"]));
        }

        private void LoadSostav(int id_recipes)
        {
            using (NpgsqlConnection connection = Metods.Source.OpenConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(Metods.querySostRec, connection);
                command.Parameters.AddWithValue("@id_rec", id_recipes);
                NpgsqlDataAdapter dataAdapterSostRec = new NpgsqlDataAdapter(command);
                DataTable dataTableSostRec = new DataTable();
                dataAdapterSostRec.Fill(dataTableSostRec);
                DataGridSostRec.ItemsSource = dataTableSostRec.DefaultView;
            }
        }

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null)
            {
                this.NavigationService.Content = null;
            }
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Скрытие столбцов
            string headerName = e.Column.Header.ToString();

            // Проверяем, является ли текущий столбец тем, который мы хотим скрыть
            if (headerName == "id_sost" || headerName == "Название изделия")
            {
                e.Column.Visibility = Visibility.Collapsed;
            }
        }
    }
}
