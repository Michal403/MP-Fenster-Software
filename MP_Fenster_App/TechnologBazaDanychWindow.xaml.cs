using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace MP_Fenster_App
{
    public partial class TechnologBazaDanychWindow : Window
    {
        private readonly string _connString = "Server=localhost;Database=SeaSharkDB;User Id=sa;Password=zaq1@WSX;TrustServerCertificate=True;";
        private readonly string _user;
        private readonly List<TableOption> _slowniki = new()
        {
            new("SystemyProfilowe", "Systemy profilowe"),
            new("TypyKonstrukcji", "Produkty i konstrukcje"),
            new("RodzajePozycji", "Rodzaje pozycji"),
            new("PakietySzybowe", "Pakiety szybowe"),
            new("RamkiDystansowe", "Ramki dystansowe"),
            new("KoloryOklein", "Kolory profili"),
            new("KoloryUszczelek", "Kolory uszczelek"),
            new("KoloryBazyProfilu", "Kolory bazy profilu"),
            new("KoloryWariantyUkladu", "Układy oklein"),
            new("OkuciaKlasyBezpieczenstwa", "Klasy bezpieczeństwa okuć"),
            new("OkuciaWariantyOtwierania", "Warianty otwierania"),
            new("OkuciaZawiasy", "Zawiasy"),
            new("OkuciaKoloryOslonek", "Kolory osłonek zawiasów"),
            new("KlamkiKatalog", "Katalog klamek"),
            new("KlamkiKolory", "Kolory klamek"),
            new("KlamkiWysokosci", "Wysokości klamek"),
            new("SzprosyTypy", "Typy szprosów"),
            new("SzprosyKatalog", "Katalog szprosów"),
            new("ListwyPodparapetowe", "Listwy podparapetowe"),
            new("OgraniczeniaSystemowe", "Ograniczenia gabarytowe"),
            new("GrafikiKonstrukcji", "Grafiki konstrukcji"),
            new("ParametryFinansowe", "Parametry finansowe krajów"),
            new("CennikUslug", "Cennik usług")
        };

        private SqlDataAdapter? _adapter;
        private DataTable? _table;

        public TechnologBazaDanychWindow(string user)
        {
            InitializeComponent();
            _user = user;
            WczytajListeTabel();
        }

        private void WczytajListeTabel()
        {
            try
            {
                using var cn = new SqlConnection(_connString);
                cn.Open();
                using var cmd = new SqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo'", cn);
                using var dr = cmd.ExecuteReader();
                var istnieja = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                while (dr.Read())
                {
                    istnieja.Add(dr.GetString(0));
                }

                CmbTabele.ItemsSource = _slowniki.Where(t => istnieja.Contains(t.Name)).ToList();
                if (CmbTabele.Items.Count > 0)
                {
                    CmbTabele.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie udało się pobrać listy słowników technicznych: " + ex.Message, "Baza danych");
            }
        }

        private void WczytajTabele()
        {
            if (CmbTabele.SelectedValue == null) return;

            string tableName = CmbTabele.SelectedValue.ToString() ?? "";
            if (!_slowniki.Any(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Ta tabela nie jest dopuszczona do edycji technologa.", "Blokada");
                return;
            }

            try
            {
                _adapter = new SqlDataAdapter($"SELECT * FROM dbo.[{tableName}]", _connString);
                var builder = new SqlCommandBuilder(_adapter)
                {
                    QuotePrefix = "[",
                    QuoteSuffix = "]"
                };

                _adapter.InsertCommand = builder.GetInsertCommand();
                _adapter.UpdateCommand = builder.GetUpdateCommand();
                _table = new DataTable(tableName);
                _adapter.Fill(_table);
                DgTabela.ItemsSource = _table.DefaultView;
            }
            catch (Exception ex)
            {
                DgTabela.ItemsSource = null;
                MessageBox.Show("Nie udało się wczytać tabeli. Jeżeli nie ma klucza głównego, edycja automatyczna może być niedostępna.\n\n" + ex.Message, "Baza danych");
            }
        }

        private void ZapiszZmiany()
        {
            if (_adapter == null || _table == null)
            {
                return;
            }

            try
            {
                DgTabela.CommitEdit(DataGridEditingUnit.Cell, true);
                DgTabela.CommitEdit(DataGridEditingUnit.Row, true);

                int zapisane = _adapter.Update(_table);
                MessageBox.Show($"Zapisano zmiany w słowniku. Liczba zmienionych rekordów: {zapisane}", "Baza danych");
                WczytajTabele();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie udało się zapisać zmian. Sprawdź wymagane pola w tabeli.\n\n" + ex.Message, "Baza danych");
            }
        }

        private void CmbTabele_SelectionChanged(object sender, SelectionChangedEventArgs e) => WczytajTabele();
        private void BtnOdswiez_Click(object sender, RoutedEventArgs e) => WczytajTabele();
        private void BtnZapisz_Click(object sender, RoutedEventArgs e) => ZapiszZmiany();
        private void BtnZamknij_Click(object sender, RoutedEventArgs e) => Close();

        public sealed class TableOption
        {
            public TableOption(string name, string label)
            {
                Name = name;
                Label = label;
            }

            public string Name { get; }
            public string Label { get; }
        }
    }
}
