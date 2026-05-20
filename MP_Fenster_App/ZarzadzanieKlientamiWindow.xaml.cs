using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace MP_Fenster_App
{
    public partial class ZarzadzanieKlientamiWindow : Window
    {
        private readonly string _connString = "Server=localhost;Database=SeaSharkDB;User Id=sa;Password=zaq1@WSX;TrustServerCertificate=True;";
        private string _username;

        // Te pola zostaną odczytane przez NoweZlecenieWindow po zamknięciu tego okna
        public int WybraneId { get; private set; } = -1;
        public string WybranaNazwa { get; private set; } = "";

        public ZarzadzanieKlientamiWindow(string zalogowanyUser, bool otworzOdRazuDodawanie = false)
        {
            InitializeComponent();
            _username = zalogowanyUser;

            // Jeśli kliknięto plusika, aktywujemy od razu drugą zakładkę formularza
            if (otworzOdRazuDodawanie)
            {
                TabNowy.IsSelected = true;
            }

            OdswiezListe();
        }

        private void OdswiezListe()
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    // Zabezpieczenie: Ładujemy tylko klientów przypisanych do tego handlowca
                    string sql = @"SELECT IdKlienta, NazwaKlienta, NIP, Adres, Telefon 
                                   FROM Klienci k
                                   JOIN Uzytkownicy u ON k.IdUzytkownika = u.IdUzytkownika
                                   WHERE UPPER(u.Login) = @login";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@login", _username.ToUpper());
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            DataTable dt = new DataTable();
                            dt.Load(dr);
                            DgKlienci.ItemsSource = dt.DefaultView;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd bazy danych: " + ex.Message);
            }
        }

        private void ZatwierdzWybor()
        {
            if (DgKlienci.SelectedItem is DataRowView row)
            {
                WybraneId = Convert.ToInt32(row["IdKlienta"]);
                WybranaNazwa = row["NazwaKlienta"].ToString();
                this.DialogResult = true; // Zamyka okno i przekazuje sukces
                this.Close();
            }
            else
            {
                MessageBox.Show("Wybierz klienta z tabeli!", "Informacja");
            }
        }

        private void BtnWybierz_Click(object sender, RoutedEventArgs e) => ZatwierdzWybor();
        private void DgKlienci_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => ZatwierdzWybor();

        private void BtnZapisz_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FormNazwa.Text))
            {
                MessageBox.Show("Nazwa klienta jest wymagana!", "Walidacja");
                return;
            }

            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();

                    // Pobieramy IdUzytkownika na podstawie loginu handlowca
                    int idUsera = 1;
                    using (SqlCommand cmdGetId = new SqlCommand("SELECT IdUzytkownika FROM Uzytkownicy WHERE UPPER(Login) = @log", cn))
                    {
                        cmdGetId.Parameters.AddWithValue("@log", _username.ToUpper());
                        var res = cmdGetId.ExecuteScalar();
                        if (res != null) idUsera = Convert.ToInt32(res);
                    }

                    // Wstawiamy nowego klienta przypisując mu zalogowanego handlowca
                    string insertSql = @"INSERT INTO Klienci (NazwaKlienta, NIP, Adres, Telefon, IdUzytkownika) 
                                         OUTPUT INSERTED.IdKlienta
                                         VALUES (@nazwa, @nip, @adres, @tel, @idUser);";

                    using (SqlCommand cmd = new SqlCommand(insertSql, cn))
                    {
                        cmd.Parameters.AddWithValue("@nazwa", FormNazwa.Text.Trim());
                        cmd.Parameters.AddWithValue("@nip", string.IsNullOrWhiteSpace(FormNIP.Text) ? (object)DBNull.Value : FormNIP.Text.Trim());
                        cmd.Parameters.AddWithValue("@adres", FormAdres.Text.Trim());
                        cmd.Parameters.AddWithValue("@tel", FormTelefon.Text.Trim());
                        cmd.Parameters.AddWithValue("@idUser", idUsera);

                        // Pobieramy nowo wygenerowane ID klienta prosto z bazy
                        WybraneId = (int)cmd.ExecuteScalar();
                        WybranaNazwa = FormNazwa.Text.Trim();
                    }
                }

                MessageBox.Show($"Kontrahent pomyślnie dodany do bazy i przypisany do Twojego konta!", "Sukces");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas dodawania kontrahenta: " + ex.Message);
            }
        }

        private void TxtSzukaj_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (DgKlienci.ItemsSource is DataView dv)
            {
                dv.RowFilter = $"NazwaKlienta LIKE '%{TxtSzukaj.Text}%' OR NIP LIKE '%{TxtSzukaj.Text}%'";
            }
        }

        private void BtnAnuluj_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}