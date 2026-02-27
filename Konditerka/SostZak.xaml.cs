using Npgsql;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Konditerka
{
    /// <summary>
    /// Логика взаимодействия для SostZak.xaml
    /// </summary>
    public partial class SostZak : Page
    {
        public SostZak(DataRowView selectedZakaz)
        {
            InitializeComponent();
            // 1. Устанавливаем заголовок из данных
            SostZakHeader.Text = $"Заказ для: {selectedZakaz["Имя"]}, №телефона: {selectedZakaz["Номер телефона"]}";

            // 2. Загружаем данные в DataGrid
            // Здесь вызывайте ваш метод загрузки, передав ID заказа
            LoadSostav(Convert.ToInt32(selectedZakaz["Номер заказа"]));
        }

        private void LoadSostav(int id_zak)
        {
            using (NpgsqlConnection connection = Metods.Source.OpenConnection())
            {
                NpgsqlCommand command = new NpgsqlCommand(Metods.querySostZak, connection);
                command.Parameters.AddWithValue("@id_zak", id_zak);
                NpgsqlDataAdapter dataAdapterSostZak = new NpgsqlDataAdapter(command);
                DataTable dataTableSostZak = new DataTable();
                dataAdapterSostZak.Fill(dataTableSostZak);
                DataGridSostZak.ItemsSource = dataTableSostZak.DefaultView;
            }
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string headerName = e.Column.Header.ToString();
            // перенос текста и стиль выравнивания
            if (headerName == "Упаковка" || headerName == "Декорирование")
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
            //уменьшение ширины столбцов
            if (headerName == "Номер заказа" || headerName == "Изделие" || headerName == "Количество")
            {
                //уменьшение ширины столбца
                e.Column.Width = new DataGridLength(0.5, DataGridLengthUnitType.Star);
            }
            else
            {
                // Все остальные столбцы будут иметь стандартную пропорцию ширины (1*)
                e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            }
        }

        private void BackZakBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null)
            {
                this.NavigationService.Content = null;
            }
        }
    }
}
