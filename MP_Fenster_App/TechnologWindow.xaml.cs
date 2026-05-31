using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.SqlClient;

namespace MP_Fenster_App
{
    public partial class TechnologWindow : Window
    {
        private readonly string _connString = "Server=localhost;Database=SeaSharkDB;User Id=sa;Password=zaq1@WSX;TrustServerCertificate=True;";
        private readonly string _user;

        public TechnologWindow(string user)
        {
            InitializeComponent();
            _user = user;

            StatusUser.Content = _user.ToUpper();
            StatusDate.Content = DateTime.Now.ToString("dd.MM.yyyy");
            StatusTime.Content = DateTime.Now.ToString("HH:mm");

            WczytajKolejkeTechnologa();
        }

        private void WczytajKolejkeTechnologa()
        {
            TechOrdersTree.Items.Clear();
            var root = new TreeViewItem { Header = "Oferty wymagające decyzji", IsExpanded = true };

            try
            {
                using var cn = new SqlConnection(_connString);
                cn.Open();

                string sql = @"
                    SELECT TOP 10 z.IdZlecenia, z.NumerZlecenia, u.Login, k.NazwaKlienta
                    FROM Zlecenia z
                    JOIN Uzytkownicy u ON z.IdUzytkownika = u.IdUzytkownika
                    LEFT JOIN Klienci k ON z.IdKlienta = k.IdKlienta
                    WHERE EXISTS (
                        SELECT 1 FROM PozycjeZlecenia p
                        WHERE p.IdZlecenia = z.IdZlecenia AND ISNULL(p.StatusTechniczny, 1) = 0
                    )
                    ORDER BY z.DataWprowadzenia DESC, z.IdZlecenia DESC";

                using var cmd = new SqlCommand(sql, cn);
                using var dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    root.Items.Add(new TreeViewItem
                    {
                        Header = $"⚠ Oferta {dr["NumerZlecenia"]} - {dr["Login"]} - {dr["NazwaKlienta"]}",
                        Tag = Convert.ToInt32(dr["IdZlecenia"])
                    });
                }
            }
            catch (Exception ex)
            {
                root.Items.Add(new TreeViewItem { Header = "Nie udało się pobrać kolejki: " + ex.Message });
            }

            if (root.Items.Count == 0)
            {
                root.Items.Add(new TreeViewItem { Header = "Brak ofert do akceptacji" });
            }

            TechOrdersTree.Items.Add(root);
        }

        private void OtworzAkceptacjeZDrzewa()
        {
            if (TechOrdersTree.SelectedItem is not TreeViewItem item || item.Tag is not int idZlecenia)
            {
                return;
            }

            var okno = new NoweZlecenieWindow(_user);
            okno.WczytajIstniejaceZlecenieZBase(idZlecenia);
            okno.Show();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            Close();
        }

        private void BtnNowe_Click(object sender, RoutedEventArgs e)
        {
            new NoweZlecenieWindow(_user).Show();
        }

        private void BtnPrzeglad_Click(object sender, RoutedEventArgs e)
        {
            new StatusZlecen(_user, "Technolog", "OFERTY").Show();
        }

        private void BtnStatus_Click(object sender, RoutedEventArgs e)
        {
            new StatusZlecen(_user, "Technolog", "STATUS").Show();
        }

        private void BtnProdukcja_Click(object sender, RoutedEventArgs e)
        {
            new StatusZlecen(_user, "Technolog", "PRODUKCJA").Show();
        }

        private void BtnAkceptacja_Click(object sender, RoutedEventArgs e)
        {
            new StatusZlecen(_user, "Technolog", "AKCEPTACJA").Show();
        }

        private void BtnBaza_Click(object sender, RoutedEventArgs e)
        {
            new TechnologBazaDanychWindow(_user) { Owner = this }.ShowDialog();
        }

        private void TechOrdersTree_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OtworzAkceptacjeZDrzewa();
        private void MenuStart_Click(object sender, RoutedEventArgs e) => WczytajKolejkeTechnologa();
        private void MenuOpcje_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Opcje technologa: akceptacja ograniczeń, obsługa statusów produkcji i słowniki techniczne.", "Opcje");
        private void MenuDrukuj_Click(object sender, RoutedEventArgs e)
        {
            new WyborOfertyDoDrukuWindow(_user, "Technolog") { Owner = this }.ShowDialog();
        }
        private void MenuPomoc_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Oferty z błędami trafiają do kolejki akceptacji. Po akceptacji można je zaksięgować na zlecenie i prowadzić status produkcji.", "Pomoc");
    }
}
