using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.SqlClient;

namespace MP_Fenster_App
{
    public partial class HandlowiecWindow : Window
    {
        private readonly string _connString = "Server=localhost;Database=SeaSharkDB;User Id=sa;Password=zaq1@WSX;TrustServerCertificate=True;";
        private readonly string _zalogowanyUser;

        public HandlowiecWindow(string user)
        {
            InitializeComponent();
            _zalogowanyUser = user;

            StatusUser.Content = _zalogowanyUser.ToUpper();
            StatusDate.Content = DateTime.Now.ToString("dd.MM.yyyy");
            StatusTime.Content = DateTime.Now.ToString("HH:mm");

            OdswiezZleceniaZBazy();
        }

        private void OdswiezZleceniaZBazy()
        {
            MyOrdersTree.Items.Clear();
            var root = new TreeViewItem { Header = "Moje ostatnie dokumenty", IsExpanded = true };

            try
            {
                using var cn = new SqlConnection(_connString);
                cn.Open();

                string sql = @"
                    SELECT TOP 10 z.IdZlecenia, z.NumerZlecenia, z.StatusZlecenia,
                           ISNULL(z.StatusProdukcji, N'Nie rozpoczęto') AS StatusProdukcji,
                           CASE WHEN EXISTS (
                                SELECT 1 FROM PozycjeZlecenia p
                                WHERE p.IdZlecenia = z.IdZlecenia AND ISNULL(p.StatusTechniczny, 1) = 0
                           ) THEN 1 ELSE 0 END AS MaBlad
                    FROM Zlecenia z
                    JOIN Uzytkownicy u ON z.IdUzytkownika = u.IdUzytkownika
                    WHERE UPPER(u.Login) = @login
                    ORDER BY z.DataWprowadzenia DESC, z.IdZlecenia DESC";

                using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@login", _zalogowanyUser.ToUpper());
                using var dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    bool maBlad = Convert.ToInt32(dr["MaBlad"]) == 1;
                    string status = dr["StatusZlecenia"]?.ToString() ?? "OFERTA";
                    string statusProdukcji = dr["StatusProdukcji"]?.ToString() ?? "Nie rozpoczęto";
                    string prefix = maBlad ? "⚠ " : "";
                    string opis = status.Equals("ZLECENIE", StringComparison.OrdinalIgnoreCase)
                        ? $"{prefix}Zlecenie {dr["NumerZlecenia"]} - {statusProdukcji}"
                        : $"{prefix}Oferta {dr["NumerZlecenia"]} - {status}";

                    root.Items.Add(new TreeViewItem
                    {
                        Header = opis,
                        Tag = Convert.ToInt32(dr["IdZlecenia"])
                    });
                }
            }
            catch (Exception ex)
            {
                root.Items.Add(new TreeViewItem { Header = "Nie udało się pobrać listy: " + ex.Message });
            }

            if (root.Items.Count == 0)
            {
                root.Items.Add(new TreeViewItem { Header = "Brak ofert dla tego handlowca" });
            }

            MyOrdersTree.Items.Add(root);
        }

        private void OtworzDokumentZDrzewa()
        {
            if (MyOrdersTree.SelectedItem is not TreeViewItem item || item.Tag is not int idZlecenia)
            {
                return;
            }

            if (!CzyDokumentJestOferta(idZlecenia))
            {
                MessageBox.Show("To jest już zlecenie. Handlowiec może śledzić jego status, ale nie edytuje go bez cofnięcia przez technologa.", "Dokument zablokowany");
                return;
            }

            var okno = new NoweZlecenieWindow(_zalogowanyUser);
            okno.WczytajIstniejaceZlecenieZBase(idZlecenia);
            okno.Show();
        }

        private bool CzyDokumentJestOferta(int idZlecenia)
        {
            try
            {
                using var cn = new SqlConnection(_connString);
                cn.Open();
                using var cmd = new SqlCommand("SELECT StatusZlecenia FROM Zlecenia WHERE IdZlecenia = @id", cn);
                cmd.Parameters.AddWithValue("@id", idZlecenia);
                string status = cmd.ExecuteScalar()?.ToString() ?? "OFERTA";
                return status.Equals("OFERTA", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            Close();
        }

        private void BtnNowe_Click(object sender, RoutedEventArgs e)
        {
            var okno = new NoweZlecenieWindow(_zalogowanyUser);
            okno.Show();
        }

        private void BtnPrzeglad_Click(object sender, RoutedEventArgs e)
        {
            new StatusZlecen(_zalogowanyUser, "Handlowiec", "OFERTY").Show();
        }

        private void BtnStatus_Click(object sender, RoutedEventArgs e)
        {
            new StatusZlecen(_zalogowanyUser, "Handlowiec", "STATUS").Show();
        }

        private void BtnKlienci_Click(object sender, RoutedEventArgs e)
        {
            var okno = new ZarzadzanieKlientamiWindow(_zalogowanyUser, false) { Owner = this };
            okno.ShowDialog();
        }

        private void MyOrdersTree_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OtworzDokumentZDrzewa();
        private void MenuStart_Click(object sender, RoutedEventArgs e) => OdswiezZleceniaZBazy();
        private void MenuOpcje_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Opcje handlowca: odświeżenie danych, obsługa klientów i praca na ofertach.", "Opcje");
        private void MenuDrukuj_Click(object sender, RoutedEventArgs e)
        {
            new WyborOfertyDoDrukuWindow(_zalogowanyUser, "Handlowiec") { Owner = this }.ShowDialog();
        }
        private void MenuPomoc_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Najpierw tworzysz ofertę, potem w przeglądzie ofert księgujesz ją na zlecenie. Oferta z błędem wymaga akceptacji technologa.", "Pomoc");
    }
}
