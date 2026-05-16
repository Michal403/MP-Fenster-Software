using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace MP_Fenster_App
{
    public partial class StatusZlecenWindow : Window
    {
        private readonly string _connString = "Server=localhost;Database=SeaSharkDB;User Id=sa;Password=zaq1@WSX;TrustServerCertificate=True;";
        private string _zalogowanyUzytkownik;
        private int _idUzytkownika = 0;
        private string _rolaUzytkownika = "Handlowiec";

        public StatusZlecenWindow(string loginZalogowanego)
        {
            InitializeComponent();
            _zalogowanyUzytkownik = loginZalogowanego;
            TxtPracownikInfo.Text = _zalogowanyUzytkownik.ToUpper();

            OkreślRolęIUruchomRejestr();
        }

        // Pobieranie Roli bezpośrednio z bazy danych w celu weryfikacji uprawnień
        private void OkreślRolęIUruchomRejestr()
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    string sql = "SELECT IdUzytkownika, Rola FROM Uzytkownicy WHERE UPPER(Login) = @login";
                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@login", _zalogowanyUzytkownik.ToUpper());
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                _idUzytkownika = Convert.ToInt32(dr["IdUzytkownika"]);
                                _rolaUzytkownika = dr["Rola"].ToString() ?? "Handlowiec";
                            }
                        }
                    }
                }

                TxtRolaInfo.Text = _rolaUzytkownika.ToUpper();

                // Dynamiczna zmiana koloru paska roli w zależności od uprawnień
                if (_rolaUzytkownika.ToLower() == "technolog")
                {
                    BrdrRola.Background = System.Windows.Media.Brushes.DarkGoldenrod;
                }

                LadujRejestrZlecenBazy();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd autoryzacji profilu użytkownika: " + ex.Message, "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtRolaInfo.Text = "OFFLINE MODE";
                LadujRejestrZlecenBazy();
            }
        }

        // Prawdziwe, bezlitosne i wydajne pobieranie danych za pomocą RAW SQL
        private void LadujRejestrZlecenBazy()
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();

                    // Bazowy SQL łączący Zlecenia, Klientów i twórców ofert
                    string sql = @"SELECT 
                                    Z.IdZlecenia, 
                                    Z.NumerZlecenia, 
                                    K.NazwaKlienta, 
                                    K.NIP, 
                                    U.Login AS LoginUzytkownika, 
                                    Z.DataWprowadzenia, 
                                    Z.TerminPreferowany, 
                                    Z.Obszar, 
                                    Z.StatusZlecenia 
                                   FROM Zlecenia Z
                                   INNER JOIN Klienci K ON Z.IdKlienta = K.IdKlienta
                                   INNER JOIN Uzytkownicy U ON Z.IdUzytkownika = U.IdUzytkownika";

                    // ZASADA BEZPIECZEŃSTWA: Jeśli zalogowany jest Handlowiec, doklejamy filtr WHERE i pokazujemy tylko jego rekordy
                    if (_rolaUzytkownika.ToLower() == "handlowiec")
                    {
                        sql += " WHERE Z.IdUzytkownika = @idUser";
                    }

                    sql += " ORDER BY Z.IdZlecenia DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        if (_rolaUzytkownika.ToLower() == "handlowiec")
                        {
                            cmd.Parameters.AddWithValue("@idUser", _idUzytkownika);
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
                MessageBox.Show("Błąd synchronizacji rejestru z chmurą Docker: " + ex.Message, "Błąd SQL SQLServer", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // REAKCJA: Kliknięcie wiersza w głównej tabeli ładuje surowym SQL pozycje składowe zlecenia
        private void GridZlecenia_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRowView? selectedRow = GridZlecenia.SelectedItem as DataRowView;
            if (selectedRow == null)
            {
                GridPozycjeZlecenia.ItemsSource = null;
                TxtUwagiPozycji.Text = string.Empty;
                return;
            }

            int idZlecenia = Convert.ToInt32(selectedRow["IdZlecenia"]);

            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    string sql = @"SELECT IdPozycji, NrProdukcyjny, Sztuk, Szerokosc, Wysokosc, CenaJednostkowa, Uwagi, StatusTechniczny 
                                   FROM PozycjeZlecenia 
                                   WHERE IdZlecenia = @idZlec 
                                   ORDER BY IdPozycji ASC";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@idZlec", idZlecenia);
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            DataTable dt = new DataTable();
                            dt.Load(dr);
                            GridPozycjeZlecenia.ItemsSource = dt.DefaultView;
                        }
                    }
                }

                // Podpięcie zdarzenia zmiany zaznaczenia pozycji koszyka, aby wyświetlić uwagi
                if (GridPozycjeZlecenia.Items.Count > 0)
                {
                    GridPozycjeZlecenia.SelectionChanged -= GridPozycjeZlecenia_SelectionChanged;
                    GridPozycjeZlecenia.SelectionChanged += GridPozycjeZlecenia_SelectionChanged;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd pobierania składników zlecenia: " + ex.Message, "Błąd relacji");
            }
        }

        private void GridPozycjeZlecenia_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRowView? selectedLine = GridPozycjeZlecenia.SelectedItem as DataRowView;
            if (selectedLine != null)
            {
                TxtUwagiPozycji.Text = selectedLine["Uwagi"].ToString();
            }
            else
            {
                TxtUwagiPozycji.Text = string.Empty;
            }
        }

        private void BtnOdswiez_Click(object sender, RoutedEventArgs e)
        {
            LadujRejestrZlecenBazy();
            MessageBox.Show("Zaktualizowano rejestr zleceń pobierając najświeższe dane wejściowe.", "Synchronizacja", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }

        private void BtnSzczegolyZlecenia_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funkcja podglądu i edycji historycznych parametrów zlecenia w kreatorze.", "Informacja");
        }
    }
}