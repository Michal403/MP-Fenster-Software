using System;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using MP_Fenster_App.Services;

namespace MP_Fenster_App
{
    public partial class WyborOfertyDoDrukuWindow : Window
    {
        private readonly string _connString = "Server=localhost;Database=SeaSharkDB;User Id=sa;Password=zaq1@WSX;TrustServerCertificate=True;";
        private readonly string _username;
        private readonly string _role;
        private bool _gotowe;

        public WyborOfertyDoDrukuWindow(string username, string role)
        {
            InitializeComponent();
            _username = username;
            _role = role;
            TxtZakres.Text = CzyHandlowiec ? "Pokazane są tylko Twoje dokumenty" : "Technolog widzi wszystkie dokumenty";
            TxtOpis.Text = CzyHandlowiec
                ? "Wybierz swoją ofertę albo zlecenie i wygeneruj wydruk dla klienta."
                : "Wybierz dowolną ofertę albo zlecenie i wygeneruj wydruk dla klienta.";
            _gotowe = true;
            LadujDokumenty();
        }

        private bool CzyHandlowiec => _role.Equals("Handlowiec", StringComparison.OrdinalIgnoreCase);

        private void LadujDokumenty()
        {
            if (!_gotowe) return;

            try
            {
                using var cn = new SqlConnection(_connString);
                cn.Open();

                string sql = @"
                    SELECT z.IdZlecenia, z.NumerZlecenia,
                           ISNULL(k.NazwaKlienta, N'Brak klienta') AS NazwaKlienta,
                           u.Login AS Wprowadzil,
                           z.DataWprowadzenia,
                           ISNULL(z.StatusZlecenia, N'OFERTA') AS StatusZlecenia,
                           ISNULL(z.StatusProdukcji, N'Nie rozpoczęto') AS StatusProdukcji,
                           CASE
                               WHEN EXISTS (SELECT 1 FROM PozycjeZlecenia p WHERE p.IdZlecenia = z.IdZlecenia AND ISNULL(p.StatusTechniczny, 1) = 0)
                                   THEN N'WYMAGA AKCEPTACJI'
                               WHEN EXISTS (SELECT 1 FROM PozycjeZlecenia p WHERE p.IdZlecenia = z.IdZlecenia AND ISNULL(p.StatusTechniczny, 1) = 2)
                                   THEN N'ZAAKCEPTOWANO ODSTĘPSTWO'
                               ELSE N'OK'
                           END AS StatusTechniczny
                    FROM Zlecenia z
                    LEFT JOIN Klienci k ON z.IdKlienta = k.IdKlienta
                    JOIN Uzytkownicy u ON z.IdUzytkownika = u.IdUzytkownika";

                bool maSzukaj = !string.IsNullOrWhiteSpace(TxtSzukaj.Text);
                string where = "";
                if (CzyHandlowiec)
                {
                    where = " WHERE UPPER(u.Login) = @login";
                }
                if (maSzukaj)
                {
                    where += string.IsNullOrEmpty(where) ? " WHERE " : " AND ";
                    where += "(CONVERT(NVARCHAR(50), z.NumerZlecenia) LIKE @szukaj OR k.NazwaKlienta LIKE @szukaj OR u.Login LIKE @szukaj)";
                }

                sql += where + " ORDER BY z.DataWprowadzenia DESC, z.IdZlecenia DESC";

                using var cmd = new SqlCommand(sql, cn);
                if (CzyHandlowiec)
                {
                    cmd.Parameters.AddWithValue("@login", _username.ToUpper());
                }
                if (maSzukaj)
                {
                    cmd.Parameters.AddWithValue("@szukaj", "%" + TxtSzukaj.Text.Trim() + "%");
                }

                using var dr = cmd.ExecuteReader();
                var dt = new DataTable();
                dt.Load(dr);
                DgDokumenty.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie udało się pobrać dokumentów do druku: " + ex.Message, "Drukowanie");
            }
        }

        private void DrukujWybrany()
        {
            if (DgDokumenty.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Wybierz dokument z listy.", "Drukowanie");
                return;
            }

            int idZlecenia = Convert.ToInt32(row["IdZlecenia"]);
            try
            {
                var service = new PdfOfertaService(_connString);
                string htmlPath = service.UtworzHtmlOfertyZBazy(idZlecenia);
                Process.Start(new ProcessStartInfo(htmlPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie udało się wygenerować wydruku: " + ex.Message, "Drukowanie");
            }
        }

        private void TxtSzukaj_TextChanged(object sender, TextChangedEventArgs e) => LadujDokumenty();
        private void BtnOdswiez_Click(object sender, RoutedEventArgs e) => LadujDokumenty();
        private void BtnDrukuj_Click(object sender, RoutedEventArgs e) => DrukujWybrany();
        private void DgDokumenty_MouseDoubleClick(object sender, MouseButtonEventArgs e) => DrukujWybrany();
        private void BtnZamknij_Click(object sender, RoutedEventArgs e) => Close();
    }
}
