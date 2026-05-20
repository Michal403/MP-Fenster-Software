using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace MP_Fenster_App
{
    public partial class AdminPanelWindow : Window
    {
        private readonly string _connString = "Server=localhost;Database=SeaSharkDB;User Id=sa;Password=zaq1@WSX;TrustServerCertificate=True;";

        private SqlDataAdapter _adapter;
        private DataTable _dataTable;
        private string _currentTable = "";

        public AdminPanelWindow()
        {
            InitializeComponent();
            LoadAllTablesToTreeView();
        }

        private void LoadAllTablesToTreeView()
        {
            try
            {
                TvTables.Items.Clear();
                TreeViewItem databaseNode = new TreeViewItem() { Header = "SeaSharkDB (dbo)", IsExpanded = true };
                TreeViewItem tablesFolder = new TreeViewItem() { Header = "Tabele", FontWeight = FontWeights.Bold, IsExpanded = true };

                databaseNode.Items.Add(tablesFolder);
                TvTables.Items.Add(databaseNode);

                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    string query = "SELECT name FROM sys.tables WHERE is_ms_shipped = 0 ORDER BY name;";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableName = reader["name"].ToString();
                            TreeViewItem tableItem = new TreeViewItem() { Header = tableName, FontWeight = FontWeights.Normal };
                            tablesFolder.Items.Add(tableItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd odczytu struktury bazy z Dockera: {ex.Message}", "Błąd krytyczny", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TvTables_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (TvTables.SelectedItem is TreeViewItem selectedItem)
            {
                string selectedHeader = selectedItem.Header.ToString();
                if (selectedHeader == "Tabele" || selectedHeader.Contains("SeaSharkDB")) return;

                _currentTable = selectedHeader;
                LblCurrentTable.Text = _currentTable;
                LoadTableData(_currentTable);
            }
        }

        // POPRAWIONE: Wymuszanie na WPF całkowitego przebudowania kolumn schematu bazy
        private void LoadTableData(string tableName)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    string query = $"SELECT * FROM [{tableName}]";

                    _adapter = new SqlDataAdapter(query, conn);
                    SqlCommandBuilder builder = new SqlCommandBuilder(_adapter);

                    _dataTable = new DataTable();
                    _adapter.Fill(_dataTable);

                    // PANCERNY RESET WIDOKU - Zapobiega trzymaniu starych kolumn w pamięci WPF!
                    DgAdminData.ItemsSource = null;
                    DgAdminData.Columns.Clear();

                    DgAdminData.ItemsSource = _dataTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas ładowania tabeli {tableName}: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentTable)) LoadTableData(_currentTable);
        }

        private void BtnAddRow_Click(object sender, RoutedEventArgs e)
        {
            if (_dataTable == null || string.IsNullOrEmpty(_currentTable)) return;
            _dataTable.Rows.Add(_dataTable.NewRow());
        }

        private void BtnDuplicate_Click(object sender, RoutedEventArgs e)
        {
            if (DgAdminData.SelectedItem is DataRowView selectedRow)
            {
                DataRow newRow = _dataTable.NewRow();
                for (int i = 0; i < _dataTable.Columns.Count; i++)
                {
                    if (!_dataTable.Columns[i].AutoIncrement) newRow[i] = selectedRow.Row[i];
                }
                _dataTable.Rows.Add(newRow);
            }
        }

        private void BtnDeleteExplicit_Click(object sender, RoutedEventArgs e)
        {
            if (DgAdminData.SelectedItem is DataRowView selectedRow) selectedRow.Row.Delete();
        }

        private void BtnCancelChanges_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentTable)) return;
            MessageBoxResult result = MessageBox.Show("Czy odrzucić modyfikacje i przywrócić stan z bazy?", "Odrzuć zmiany", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes) LoadTableData(_currentTable);
        }

        private void BtnClearTable_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentTable)) return;
            MessageBoxResult result = MessageBox.Show($"Czy wyczyścić całą zawartość tabeli [{_currentTable}]?", "Czyszczenie wierszy", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(_connString))
                    {
                        conn.Open();
                        string deleteQuery = $"DELETE FROM [{_currentTable}];";
                        using (SqlCommand cmd = new SqlCommand(deleteQuery, conn)) cmd.ExecuteNonQuery();
                    }
                    LoadTableData(_currentTable);
                }
                catch (Exception ex) { MessageBox.Show($"Błąd: {ex.Message}", "Błąd SQL", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void BtnAddColumn_Click(object sender, RoutedEventArgs e)
        {
            string columnName = TxtNewColumn.Text.Trim();
            if (string.IsNullOrEmpty(_currentTable) || string.IsNullOrEmpty(columnName)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    string alterQuery = $"ALTER TABLE [{_currentTable}] ADD [{columnName}] NVARCHAR(255) NULL;";
                    using (SqlCommand cmd = new SqlCommand(alterQuery, conn)) cmd.ExecuteNonQuery();
                }
                TxtNewColumn.Clear();
                LoadTableData(_currentTable); // Ponowne ładowanie z nową kolumną
            }
            catch (Exception ex) { MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        // POPRAWIONE: Usuwanie kolumny z natychmiastowym czyszczeniem interfejsu
        private void BtnDeleteColumn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentTable)) return;

            string columnName = "";

            // Sposób 1: Próbujemy pobrać kolumnę z klikniętej komórki w tabeli
            if (DgAdminData.CurrentColumn != null)
            {
                columnName = DgAdminData.CurrentColumn.Header.ToString();
            }
            // Sposób 2: Fallback – jeśli kliknąłeś w nagłówek, weźmiemy nazwę wpisaną w pole tekstowe obok
            else if (!string.IsNullOrWhiteSpace(TxtNewColumn.Text))
            {
                columnName = TxtNewColumn.Text.Trim();
            }

            // Jeśli oba sposoby zawiodły, pokazujemy jasną instrukcję
            if (string.IsNullOrEmpty(columnName))
            {
                MessageBox.Show("Nie wybrano kolumny! Możesz to zrobić na dwa sposoby:\n\n" +
                                "1. Kliknij w puste, białe pole (komórkę) wewnątrz tej kolumny (nie w nagłówek!).\n" +
                                "2. Wpisz jej nazwę w pole tekstowe 'Kolumny:' obok fioletowego przycisku i kliknij Usuń.",
                                "Wskazówka", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Blokada bezpieczeństwa dla kluczy głównych
            if (columnName.Equals("Id", StringComparison.OrdinalIgnoreCase) || columnName.StartsWith("Id", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show($"System zablokował usunięcie kolumny '{columnName}' ze względu na integralność kluczy bazy danych!", "Blokada integralności", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show($"⚠️ Czy na pewno chcesz całkowicie i bezpowrotnie USUNĄĆ kolumnę [{columnName}] z tabeli [{_currentTable}]?\n\nWszystkie dane w tej kolumnie zostaną skasowane!", "DROP COLUMN", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(_connString))
                    {
                        conn.Open();
                        string dropColQuery = $"ALTER TABLE [{_currentTable}] DROP COLUMN [{columnName}];";
                        using (SqlCommand cmd = new SqlCommand(dropColQuery, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show($"Kolumna '{columnName}' została trwale usunięta z bazy w Dockerze.", "Sukces");
                    TxtNewColumn.Clear();          // Czyszczenie pola na wypadek, gdyby tam była wpisana
                    LoadTableData(_currentTable);  // Pełne odświeżenie schematu i reset widoku DataGrid
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd podczas usuwania kolumny z bazy SQL Server: {ex.Message}", "Błąd SQL", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCreateTable_Click(object sender, RoutedEventArgs e)
        {
            string tableName = TxtNewTable.Text.Trim();
            if (string.IsNullOrEmpty(tableName)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    conn.Open();
                    string createQuery = $"CREATE TABLE [{tableName}] (Id INT IDENTITY(1,1) PRIMARY KEY);";
                    using (SqlCommand cmd = new SqlCommand(createQuery, conn)) cmd.ExecuteNonQuery();
                }
                TxtNewTable.Clear();
                LoadAllTablesToTreeView();
            }
            catch (Exception ex) { MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnDeleteTable_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentTable)) return;
            MessageBoxResult result = MessageBox.Show($"⚠️ Czy całkowicie USUNĄĆ (DROP) tabelę [{_currentTable}]?", "DROP TABLE", MessageBoxButton.YesNo, MessageBoxImage.Stop);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(_connString))
                    {
                        conn.Open();
                        string dropQuery = $"DROP TABLE [{_currentTable}];";
                        using (SqlCommand cmd = new SqlCommand(dropQuery, conn)) cmd.ExecuteNonQuery();
                    }
                    _currentTable = "";
                    LblCurrentTable.Text = "wybierz tabelę z drzewka...";
                    DgAdminData.ItemsSource = null;
                    LoadAllTablesToTreeView();
                }
                catch (Exception ex) { MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void BtnInfo_Click(object sender, RoutedEventArgs e)
        {
            string instrukcja = "📘 PODZIAŁ FUNKCJI PANELU SEASHARK:\n\n" +
                                "1. REKORDY (Wiersze):\n" +
                                "   - Służą do wprowadzania, kopiowania i usuwania danych handlowych/technicznych wewnątrz tabeli.\n\n" +
                                "2. STRUKTURA (Kolumny):\n" +
                                "   - Pozwala modyfikować budowę tabeli. Aby usunąć kolumnę, kliknij pole w danej kolumnie i wybierz 'Usuń zaznaczoną kolumnę'.\n\n" +
                                "3. TABELE SYSTEMOWE:\n" +
                                "   - Całkowite dodawanie nowych relacji słownikowych lub usuwanie całych tabel z bazy danych (DROP).";

            MessageBox.Show(instrukcja, "Instrukcja Obsługi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_adapter == null || _dataTable == null || string.IsNullOrEmpty(_currentTable)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connString))
                {
                    _adapter.SelectCommand.Connection = conn;
                    SqlCommandBuilder builder = new SqlCommandBuilder(_adapter);
                    _adapter.Update(_dataTable);
                }
                MessageBox.Show("Wszystkie modyfikacje wierszy zostały zaktualizowane w Dockerze!", "Sukces");
                LoadTableData(_currentTable);
            }
            catch (Exception ex) { MessageBox.Show($"Błąd zapisu danych: {ex.Message}", "Błąd krytyczny", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}