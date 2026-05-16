using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.SqlClient;

namespace MP_Fenster_App
{
    public partial class StatusZlecen : Window
    {
        private readonly string _connString = "Server=localhost;Database=SeaSharkDB;User Id=sa;Password=zaq1@WSX;TrustServerCertificate=True;";
        private string _username;
        private string _role;

        public StatusZlecen(string zalogowanyUser, string rolaUzytkownika)
        {
            InitializeComponent();
            _username = zalogowanyUser;
            _role = rolaUzytkownika;

            TxtOperator.Text = _username.ToUpper();
            TxtRola.Text = _role.ToUpper();

            LadujZleceniaZBase();
        }

        // WYMAGANIE: Handlowiec widzi tylko swoje, Technolog/Admin widzi wszystko prosto z Dockera
        private void LadujZleceniaZBase()
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();

                    // Budowa zapytania złączeniowego RAW SQL opartego na Twoim diagramie ERD
                    string sql = @"SELECT z.IdZlecenia, z.NumerZlecenia, k.NazwaKlienta, 
                                   u.Login AS Wprowadzil, z.DataWprowadzenia, z.Obszar, z.StatusZlecenia 
                                   FROM Zlecenia z
                                   JOIN Klienci k ON z.IdKlienta = k.IdKlienta
                                   JOIN Uzytkownicy u ON z.IdUzytkownika = u.IdUzytkownika";

                    // Jeśli rola to Handlowiec, docinamy listę tylko do jego ID
                    if (_role.ToLower() == "handlowiec")
                    {
                        sql += " WHERE UPPER(u.Login) = @login";
                    }

                    sql += " ORDER BY z.DataWprowadzenia DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        if (_role.ToLower() == "handlowiec")
                        {
                            cmd.Parameters.AddWithValue("@login", _username.ToUpper());
                        }

                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            DataTable dt = new DataTable();
                            dt.Load(dr);
                            GridZlecenia.ItemsSource = dt.DefaultView;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd pobierania rejestru zleceń z kontenera SQL: " + ex.Message, "Blokada Odczytu");
            }
        }

        private void UruchomKonfiguratorDlaZaznaczonego()
        {
            if (GridZlecenia.SelectedItem == null)
            {
                MessageBox.Show("Wybierz zlecenie z tabeli, aby otworzyć je w konfiguratorze.", "Info");
                return;
            }

            DataRowView row = (DataRowView)GridZlecenia.SelectedItem;
            int idZlecenia = Convert.ToInt32(row["IdZlecenia"]);
            string wprowadzilLogin = row["Wprowadzil"].ToString() ?? _username;

            // Odpalamy kreator, przekazując login oraz IdZlecenia, by wczytać pozycje!
            NoweZlecenieWindow kreator = new NoweZlecenieWindow(wprowadzilLogin);

            // Wywołujemy ukrytą metodę załadowania pozycji (napisana w Kroku 3)
            kreator.WczytajIstniejaceZlecenieZBase(idZlecenia);

            kreator.Show();
            this.Close(); // Zamykamy panel statusu
        }

        private void GridZlecenia_MouseDoubleClick(object sender, MouseButtonEventArgs e) => UruchomKonfiguratorDlaZaznaczonego();
        private void BtnOtworz_Click(object sender, RoutedEventArgs e) => UruchomKonfiguratorDlaZaznaczonego();
        private void BtnOdswiez_Click(object sender, RoutedEventArgs e) => LadujZleceniaZBase();
        private void BtnAnuluj_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}