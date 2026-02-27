using Npgsql;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace Konditerka
{
    /// <summary>
    /// Логика взаимодействия для Raskhody.xaml
    /// </summary>
    public partial class Raskhody : Window
    {
        public Raskhody()
        {
            InitializeComponent();
            Metods.LoadGrid(Metods.queryNaklad, naklad);
        }

        private void DataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            string headerName = e.Column.Header.ToString();

            if (headerName == "id_nakl")
            {
                e.Column.Visibility = Visibility.Collapsed;
            }
        }

        private void save(object sender, RoutedEventArgs e)
        {
            string queryNakl = "UPDATE naklad SET price_nakl = @price WHERE id_nakl = @id";
            Save(naklad, 2, queryNakl);
        }

        private void Save(DataGrid dataGrid, int columnprice, string query)
        {
            // 1. Используем using для автоматического закрытия соединения
            using (NpgsqlConnection connection = Metods.Source.OpenConnection())
            {
                try
                {
                    foreach (var item in dataGrid.Items)
                    {
                        // 2. ОБЯЗАТЕЛЬНАЯ ПРОВЕРКА для WPF: пропускаем строку ввода новой записи
                        if (item == System.Windows.Data.CollectionView.NewItemPlaceholder) continue;

                        // 3. Приводим элемент к DataRowView
                        DataRowView row = (DataRowView)item;

                        // Получаем значения (лучше по именам колонок, чем по индексам)
                        var id = row[0];
                        decimal price = Convert.ToDecimal(row[columnprice]);

                        // 4. Создаем команду и ОБЯЗАТЕЛЬНО связываем её с соединением
                        using (NpgsqlCommand comm = new NpgsqlCommand(query, connection))
                        {
                            comm.Parameters.AddWithValue("@price", price);
                            comm.Parameters.AddWithValue("@id", id);
                            comm.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("Данные успешно сохранены!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
        }

        private void newNakl(object sender, RoutedEventArgs e)
        {
            newNaklPopup.IsOpen = true;
            newNaklPop.Focus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            using (NpgsqlConnection connection = Metods.Source.OpenConnection())
            {
                try
                {
                    string text = newNaklPop.Text;
                    string query = "INSERT INTO naklad (name_nakl, price_nakl) VALUES (@text, 0);";
                    using (NpgsqlCommand comm = new NpgsqlCommand(query, connection))
                    {
                        comm.Parameters.AddWithValue("@text", text);
                        comm.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка: " + ex.Message);
                }
            }
            Metods.LoadGridwithoutClear(Metods.queryNaklad, naklad);
            newNaklPopup.IsOpen = false;
            newNaklPop.Clear();
        }

        private void KolichOtmena(object sender, RoutedEventArgs e)
        {
            newNaklPopup.IsOpen = false;
            newNaklPop.Clear();
        }
    }
}
