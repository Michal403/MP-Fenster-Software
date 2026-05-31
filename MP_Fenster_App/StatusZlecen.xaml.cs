using System;
using System.Collections.Generic;
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
        private readonly string _username;
        private readonly string _role;
        private readonly string _tryb;
        private bool _gotowe;

        public StatusZlecen(string zalogowanyUser, string rolaUzytkownika)
            : this(zalogowanyUser, rolaUzytkownika, "OFERTY")
        {
        }

        public StatusZlecen(string zalogowanyUser, string rolaUzytkownika, string tryb)
        {
            InitializeComponent();
            _username = zalogowanyUser;
            _role = rolaUzytkownika;
            _tryb = string.IsNullOrWhiteSpace(tryb) ? "OFERTY" : tryb.ToUpperInvariant();

            TxtOperator.Text = _username.ToUpper();
            TxtRola.Text = _role.ToUpper();

            UstawTrybOkna();
            WczytajStatusyProdukcji();
            _gotowe = true;
            LadujZleceniaZBase();
        }

        private bool CzyTechnolog => _role.Equals("Technolog", StringComparison.OrdinalIgnoreCase);
        private bool CzyHandlowiec => _role.Equals("Handlowiec", StringComparison.OrdinalIgnoreCase);

        private void UstawTrybOkna()
        {
            BtnKsiegowanie.Visibility = Visibility.Collapsed;
            BtnCofnij.Visibility = Visibility.Collapsed;
            LblStatusProdukcji.Visibility = Visibility.Collapsed;
            CmbStatusProdukcji.Visibility = Visibility.Collapsed;
            BtnZmienStatusProdukcji.Visibility = Visibility.Collapsed;

            if (_tryb == "OFERTY")
            {
                Title = "FENSTER 1.0 - Przegląd ofert";
                TxtNaglowek.Text = "PRZEGLĄD OFERT HANDLOWYCH";
                TxtPodpowiedz.Text = "Dwuklik otwiera ofertę do edycji. Ofertę bez błędów można zaksięgować na zlecenie.";
                TxtInfoTrybu.Text = CzyHandlowiec ? "Widok tylko Twoich ofert" : "Widok wszystkich ofert";
                BtnKsiegowanie.Visibility = Visibility.Visible;
            }
            else if (_tryb == "STATUS")
            {
                Title = "FENSTER 1.0 - Status zleceń";
                TxtNaglowek.Text = "STATUS ZLECEŃ";
                TxtPodpowiedz.Text = CzyTechnolog ? "Technolog może cofnąć zlecenie do oferty." : "Handlowiec widzi status produkcji swoich zleceń.";
                TxtInfoTrybu.Text = CzyHandlowiec ? "Widok tylko Twoich zleceń" : "Widok wszystkich zleceń";
                BtnCofnij.Visibility = CzyTechnolog ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (_tryb == "PRODUKCJA")
            {
                Title = "FENSTER 1.0 - Status produkcji";
                TxtNaglowek.Text = "STEROWANIE STATUSEM PRODUKCJI";
                TxtPodpowiedz.Text = "Wybierz zlecenie, ustaw status produkcji i zatwierdź.";
                TxtInfoTrybu.Text = "Technolog widzi wszystkie zaksięgowane zlecenia";
                LblStatusProdukcji.Visibility = Visibility.Visible;
                CmbStatusProdukcji.Visibility = Visibility.Visible;
                BtnZmienStatusProdukcji.Visibility = Visibility.Visible;
            }
            else if (_tryb == "AKCEPTACJA")
            {
                Title = "FENSTER 1.0 - Zlecenia do akceptacji";
                TxtNaglowek.Text = "OFERTY WYMAGAJĄCE AKCEPTACJI TECHNOLOGA";
                TxtPodpowiedz.Text = "Dwuklik otwiera konfigurator, gdzie technolog akceptuje ograniczenia.";
                TxtInfoTrybu.Text = "Pokazane są tylko dokumenty z niezaakceptowanym ograniczeniem";
            }
        }

        private void WczytajStatusyProdukcji()
        {
            try
            {
                using var cn = new SqlConnection(_connString);
                cn.Open();
                using var cmd = new SqlCommand("SELECT NazwaStatusu FROM StatusyProdukcji WHERE CzyAktywny = 1 ORDER BY Kolejnosc", cn);
                using var dr = cmd.ExecuteReader();
                var dt = new DataTable();
                dt.Load(dr);
                CmbStatusProdukcji.ItemsSource = dt.DefaultView;
                if (CmbStatusProdukcji.Items.Count > 0)
                {
                    CmbStatusProdukcji.SelectedIndex = 0;
                }
            }
            catch
            {
                CmbStatusProdukcji.ItemsSource = new[]
                {
                    new { NazwaStatusu = "Nie rozpoczęto" },
                    new { NazwaStatusu = "Produkcja rozpoczęta" },
                    new { NazwaStatusu = "Produkcja zakończona" },
                    new { NazwaStatusu = "Gotowe do wywiezienia" },
                    new { NazwaStatusu = "Dostarczone" }
                };
                CmbStatusProdukcji.SelectedIndex = 0;
            }
        }

        private void LadujZleceniaZBase()
        {
            if (!_gotowe) return;

            try
            {
                using var cn = new SqlConnection(_connString);
                cn.Open();

                var warunki = new List<string>();
                string sql = @"
                    SELECT z.IdZlecenia, z.NumerZlecenia,
                           ISNULL(k.NazwaKlienta, N'Brak klienta') AS NazwaKlienta,
                           u.Login AS Wprowadzil,
                           z.DataWprowadzenia,
                           z.Obszar,
                           ISNULL(z.StatusZlecenia, N'OFERTA') AS StatusZlecenia,
                           ISNULL(z.StatusProdukcji, N'Nie rozpoczęto') AS StatusProdukcji,
                           z.DataZaksiegowania,
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

                if (CzyHandlowiec)
                {
                    warunki.Add("UPPER(u.Login) = @login");
                }

                if (_tryb == "OFERTY")
                {
                    warunki.Add("ISNULL(z.StatusZlecenia, N'OFERTA') = N'OFERTA'");
                }
                else if (_tryb == "STATUS" || _tryb == "PRODUKCJA")
                {
                    warunki.Add("ISNULL(z.StatusZlecenia, N'OFERTA') = N'ZLECENIE'");
                }
                else if (_tryb == "AKCEPTACJA")
                {
                    warunki.Add(@"EXISTS (
                        SELECT 1 FROM PozycjeZlecenia p
                        WHERE p.IdZlecenia = z.IdZlecenia AND ISNULL(p.StatusTechniczny, 1) = 0
                    )");
                }

                if (!string.IsNullOrWhiteSpace(TxtSzukaj.Text))
                {
                    warunki.Add("(CONVERT(NVARCHAR(50), z.NumerZlecenia) LIKE @szukaj OR k.NazwaKlienta LIKE @szukaj OR u.Login LIKE @szukaj)");
                }

                if (warunki.Count > 0)
                {
                    sql += " WHERE " + string.Join(" AND ", warunki);
                }

                sql += " ORDER BY z.DataWprowadzenia DESC, z.IdZlecenia DESC";

                using var cmd = new SqlCommand(sql, cn);
                if (CzyHandlowiec)
                {
                    cmd.Parameters.AddWithValue("@login", _username.ToUpper());
                }
                if (!string.IsNullOrWhiteSpace(TxtSzukaj.Text))
                {
                    cmd.Parameters.AddWithValue("@szukaj", "%" + TxtSzukaj.Text.Trim() + "%");
                }

                using var dr = cmd.ExecuteReader();
                var dt = new DataTable();
                dt.Load(dr);
                GridZlecenia.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd pobierania rejestru zleceń z kontenera SQL: " + ex.Message, "Blokada odczytu");
            }
        }

        private DataRowView? PobierzWybranyWiersz()
        {
            if (GridZlecenia.SelectedItem is DataRowView row)
            {
                return row;
            }

            MessageBox.Show("Wybierz dokument z tabeli.", "Informacja");
            return null;
        }

        private void UruchomKonfiguratorDlaZaznaczonego()
        {
            var row = PobierzWybranyWiersz();
            if (row == null) return;

            int idZlecenia = Convert.ToInt32(row["IdZlecenia"]);
            string status = row["StatusZlecenia"]?.ToString() ?? "OFERTA";

            if (!status.Equals("OFERTA", StringComparison.OrdinalIgnoreCase) && _tryb != "AKCEPTACJA")
            {
                MessageBox.Show("Ten dokument jest już zaksięgowanym zleceniem. Cofnij go do oferty, jeśli trzeba poprawić konfigurację.", "Edycja zablokowana");
                return;
            }

            var kreator = new NoweZlecenieWindow(_username);
            kreator.WczytajIstniejaceZlecenieZBase(idZlecenia);
            kreator.Show();
            Close();
        }

        private int PoliczNiezaakceptowaneBledy(int idZlecenia)
        {
            using var cn = new SqlConnection(_connString);
            cn.Open();
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM PozycjeZlecenia WHERE IdZlecenia = @id AND ISNULL(StatusTechniczny, 1) = 0", cn);
            cmd.Parameters.AddWithValue("@id", idZlecenia);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private void BtnKsiegowanie_Click(object sender, RoutedEventArgs e)
        {
            var row = PobierzWybranyWiersz();
            if (row == null) return;

            int idZlecenia = Convert.ToInt32(row["IdZlecenia"]);
            string status = row["StatusZlecenia"]?.ToString() ?? "OFERTA";
            if (!status.Equals("OFERTA", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Ten dokument jest już zleceniem.", "Informacja");
                return;
            }

            int bledy = PoliczNiezaakceptowaneBledy(idZlecenia);
            if (bledy > 0)
            {
                MessageBox.Show("Nie można zaksięgować tej oferty na zlecenie, bo ma niezaakceptowane ograniczenia techniczne. Najpierw technolog musi je zaakceptować.", "Blokada techniczna");
                return;
            }

            var odp = MessageBox.Show("Zaksięgować wybraną ofertę na zlecenie? Po tej operacji handlowiec nie będzie jej edytował.", "Zaksięgowanie", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (odp != MessageBoxResult.Yes) return;

            using var cn = new SqlConnection(_connString);
            cn.Open();
            using var cmd = new SqlCommand(@"
                UPDATE Zlecenia
                SET StatusZlecenia = N'ZLECENIE',
                    StatusProdukcji = ISNULL(NULLIF(StatusProdukcji, N''), N'Nie rozpoczęto'),
                    DataZaksiegowania = GETDATE()
                WHERE IdZlecenia = @id", cn);
            cmd.Parameters.AddWithValue("@id", idZlecenia);
            cmd.ExecuteNonQuery();

            MessageBox.Show("Oferta została zaksięgowana na zlecenie.", "Gotowe");
            LadujZleceniaZBase();
        }

        private void BtnCofnij_Click(object sender, RoutedEventArgs e)
        {
            if (!CzyTechnolog)
            {
                MessageBox.Show("Tylko technolog może cofnąć zlecenie do oferty.", "Brak uprawnień");
                return;
            }

            var row = PobierzWybranyWiersz();
            if (row == null) return;

            int idZlecenia = Convert.ToInt32(row["IdZlecenia"]);
            var odp = MessageBox.Show("Cofnąć zlecenie do oferty? Handlowiec będzie mógł je poprawić i ponownie zaksięgować.", "Cofnięcie", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (odp != MessageBoxResult.Yes) return;

            using var cn = new SqlConnection(_connString);
            cn.Open();
            using var cmd = new SqlCommand(@"
                UPDATE Zlecenia
                SET StatusZlecenia = N'OFERTA',
                    StatusProdukcji = N'Nie rozpoczęto',
                    DataZmianyStatusuProdukcji = NULL,
                    DataZaksiegowania = NULL
                WHERE IdZlecenia = @id", cn);
            cmd.Parameters.AddWithValue("@id", idZlecenia);
            cmd.ExecuteNonQuery();

            MessageBox.Show("Zlecenie wróciło do ofert.", "Gotowe");
            LadujZleceniaZBase();
        }

        private void BtnZmienStatusProdukcji_Click(object sender, RoutedEventArgs e)
        {
            if (!CzyTechnolog)
            {
                MessageBox.Show("Status produkcji zmienia technolog.", "Brak uprawnień");
                return;
            }

            var row = PobierzWybranyWiersz();
            if (row == null) return;

            if (CmbStatusProdukcji.SelectedValue == null)
            {
                MessageBox.Show("Wybierz status produkcji.", "Informacja");
                return;
            }

            int idZlecenia = Convert.ToInt32(row["IdZlecenia"]);
            string status = CmbStatusProdukcji.SelectedValue.ToString() ?? "Nie rozpoczęto";

            using var cn = new SqlConnection(_connString);
            cn.Open();
            using var cmd = new SqlCommand(@"
                UPDATE Zlecenia
                SET StatusProdukcji = @status,
                    DataZmianyStatusuProdukcji = GETDATE()
                WHERE IdZlecenia = @id AND ISNULL(StatusZlecenia, N'OFERTA') = N'ZLECENIE'", cn);
            cmd.Parameters.AddWithValue("@id", idZlecenia);
            cmd.Parameters.AddWithValue("@status", status);
            int zmienione = cmd.ExecuteNonQuery();

            MessageBox.Show(zmienione > 0 ? "Status produkcji został zmieniony." : "Status można zmienić tylko dla zaksięgowanego zlecenia.", "Status produkcji");
            LadujZleceniaZBase();
        }

        private void TxtSzukaj_TextChanged(object sender, TextChangedEventArgs e) => LadujZleceniaZBase();
        private void GridZlecenia_MouseDoubleClick(object sender, MouseButtonEventArgs e) => UruchomKonfiguratorDlaZaznaczonego();
        private void BtnOtworz_Click(object sender, RoutedEventArgs e) => UruchomKonfiguratorDlaZaznaczonego();
        private void BtnOdswiez_Click(object sender, RoutedEventArgs e) => LadujZleceniaZBase();
        private void BtnAnuluj_Click(object sender, RoutedEventArgs e) => Close();
    }
}
