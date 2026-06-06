using Microsoft.Data.SqlClient;
using System;
using MP_Fenster_App.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MP_Fenster_App
{
    public partial class NoweZlecenieWindow : Window
    {
        private readonly string _connString = "Server=localhost;Database=SeaSharkDB;User Id=sa;Password=zaq1@WSX;TrustServerCertificate=True;";
        public ObservableCollection<PozycjaZlecenia> ListaPozycji { get; set; }
        private string _zalogowanyUzytkownik;
        private bool _blokadaOdswiezania = false;
        private bool _blokadaBudowyZestawu = false;
        private DispatcherTimer _timer = null!;

        // Pole śledzące ID edytowanego dokumentu z bazy
        private int? _obecneIdZlecenia = null;

        public PozycjaZlecenia? WybranaPozycja => GridPozycje?.SelectedItem as PozycjaZlecenia;

        public NoweZlecenieWindow(string wprowadzilLogin)
        {
            InitializeComponent();
            _zalogowanyUzytkownik = wprowadzilLogin;

            ListaPozycji = new ObservableCollection<PozycjaZlecenia>();
            GridPozycje.ItemsSource = ListaPozycji;

            InicjalizujDaneNaglowka();
            UruchomZywyZegar();
            GridPozycje.SelectionChanged += GridPozycje_SelectionChanged;
        }

        private void InicjalizujDaneNaglowka()
        {
            string userUpper = _zalogowanyUzytkownik.ToUpper();
            StUser.Text = userUpper;
            TxtWprowadzil.Text = userUpper;

            string aktualnaData = DateTime.Now.ToString("dd.MM.yyyy");
            TxtDataWpr.Text = aktualnaData;
            StData.Text = aktualnaData;

            // Blokada dat: jutro to minimum (data wejścia systemu: maj 2026 r.)
            DpPreferowanyTermin.DisplayDateStart = DateTime.Today.AddDays(1);
            DpPreferowanyTermin.SelectedDate = DateTime.Today.AddDays(14); // Propozycja terminu: za 2 tygodnie

            // Wywołanie ładowania dynamicznych priorytetów z tabeli TypyZlecen w Dockerze
            ZaładujTypyZlecenZBase();

            PobierzKolejnyNumerZlecenia();
            LadujSlownikiZBase();
            PrzeliczFinanseZlecenia();
        }

        private void UruchomZywyZegar()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            StCzas.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            StCzas.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        private void PobierzKolejnyNumerZlecenia()
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    string sql = "SELECT ISNULL(MAX(NumerZlecenia), 2999) + 1 FROM Zlecenia";
                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        object? res = cmd.ExecuteScalar();
                        TxtZlecenie.Text = res != null ? res.ToString() : "100";
                    }
                }
            }
            catch
            {
                TxtZlecenie.Text = "100";
            }
        }

        private void LadujSlownikiZBase()
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    PobierzDaneDoCombo(cn, @"SELECT
    Producent + ' ' + NazwaSystemu AS Wartosc,
    Producent + ' ' + NazwaSystemu + ' / rama ' + CAST(GruboscRamy AS nvarchar(10)) + ' mm / ' + CAST(IloscKomor AS nvarchar(10)) + ' komór' AS Opis,
    ISNULL(CenaBazowaM2,0) AS Cena1,
    0 AS Cena2
FROM SystemyProfilowe
WHERE CzyAktywny = 1
ORDER BY Producent, NazwaSystemu", CmbSystemOkna);

                    PobierzDaneDoCombo(cn, @"SELECT
    KodRodzaju AS Wartosc,
    NazwaRodzaju AS Opis,
    0 AS Cena1,
    0 AS Cena2
FROM RodzajePozycji
ORDER BY IdRodzaju", CmbRodzajKonstrukcji);
                    ZaladujProduktyDlaRodzaju(WartoscCombo(CmbRodzajKonstrukcji));

                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    NazwaPakietu AS Opis,
    ISNULL(CenaBazowaM2,0) AS Cena1,
    0 AS Cena2
FROM PakietySzybowe
ORDER BY IdPakietu", CmbWypelnienie);

                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    NazwaRamki AS Opis,
    ISNULL(CenaDoplatyMb,0) AS Cena1,
    0 AS Cena2
FROM RamkiDystansowe
ORDER BY IdRamki", CmbTypRamki);

                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    NazwaUkladu AS Opis,
    0 AS Cena1,
    0 AS Cena2
FROM KoloryWariantyUkladu
ORDER BY IdWariantuKoloru", CmbWariantKoloru);

                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    NazwaKoloru AS Opis,
    ISNULL(DoplataProcentowa,0) AS Cena1,
    0 AS Cena2
FROM KoloryOklein
WHERE Producent = N'Uniwersalny'
ORDER BY CASE WHEN Oznaczenie = 'W' THEN 0 ELSE 1 END, NazwaKoloru", CmbKolorOkleiny);

                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    NazwaKoloru AS Opis,
    ISNULL(DoplataProcentowa,0) AS Cena1,
    0 AS Cena2
FROM KoloryOklein
WHERE Producent = N'Uniwersalny'
ORDER BY CASE WHEN Oznaczenie = 'W' THEN 0 ELSE 1 END, NazwaKoloru", CmbKolorWewnetrzny);


                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    NazwaKoloru AS Opis,
    0 AS Cena1,
    0 AS Cena2
FROM KoloryUszczelek
ORDER BY IdUszczelki", CmbUszczelka);

                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    NazwaBazy AS Opis,
    0 AS Cena1,
    0 AS Cena2
FROM KoloryBazyProfilu
ORDER BY IdBazy", CmbKolorBazy);

                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    NazwaKlasy AS Opis,
    ISNULL(CenaDoplaty,0) AS Cena1,
    0 AS Cena2
FROM OkuciaKlasyBezpieczenstwa
ORDER BY IdKlasy", CmbKlasaBezp);

                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    NazwaWariantu AS Opis,
    ISNULL(CenaBazowaOkucia,0) AS Cena1,
    0 AS Cena2
FROM OkuciaWariantyOtwierania
ORDER BY IdWariantu", CmbWariantOkuc);

                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    NazwaTypu AS Opis,
    ISNULL(CenaDoplaty,0) AS Cena1,
    0 AS Cena2
FROM OkuciaZawiasy
ORDER BY IdZawiasu", CmbZawiasy);

                    PobierzDaneDoCombo(cn, @"SELECT
    NazwaHandlowa AS Wartosc,
    Producent + ' / ' + Material + ISNULL(' / ' + TypZabezpieczenia, '') AS Opis,
    ISNULL(CenaBazowa,0) AS Cena1,
    0 AS Cena2
FROM KlamkiKatalog
ORDER BY IdKlamki", CmbTypKlamki);

                    PobierzDaneDoCombo(cn, @"SELECT
    NazwaKoloru AS Wartosc,
    NazwaKoloru AS Opis,
    ISNULL(CenaDoplaty,0) AS Cena1,
    0 AS Cena2
FROM KlamkiKolory
ORDER BY IdKoloruKlamki", CmbKolorKlamki);
                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    NazwaTechniczna AS Opis,
    ISNULL(CenaDoplatyM2,0) AS Cena1,
    0 AS Cena2
FROM SzybyKomponenty
ORDER BY Oznaczenie", CmbSzybaZewnetrzna);

                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    NazwaTechniczna AS Opis,
    ISNULL(CenaDoplatyM2,0) AS Cena1,
    0 AS Cena2
FROM SzybyKomponenty
ORDER BY Oznaczenie", CmbSzybaWewnetrzna);

                    PobierzDaneDoCombo(cn, @"SELECT
    NazwaKoloru AS Wartosc,
    NazwaKoloru AS Opis,
    ISNULL(CenaDoplaty,0) AS Cena1,
    0 AS Cena2
FROM OkuciaKoloryOslonek
ORDER BY NazwaKoloru", CmbKolorZawiasow);

                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    Opis AS Opis,
    ISNULL(Doplata,0) AS Cena1,
    0 AS Cena2
FROM KlamkiWysokosci
ORDER BY IdWysokosci", CmbWysokoscKlamki);

                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    NazwaTypu AS Opis,
    0 AS Cena1,
    0 AS Cena2
FROM SzprosyTypy
ORDER BY IdTypu", CmbTypSzprosu);

                    PobierzDaneDoCombo(cn, @"SELECT
    CAST(SzerokoscMm AS nvarchar(10)) AS Wartosc,
    NazwaHandlowa AS Opis,
    ISNULL(CenaBazowaMb,0) AS Cena1,
    SzerokoscMm AS Cena2
FROM SzprosyKatalog
ORDER BY SzerokoscMm", CmbSzpros);

                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    NazwaKoloru AS Opis,
    ISNULL(DoplataProcentowa,0) AS Cena1,
    0 AS Cena2
FROM KoloryOklein
WHERE Producent = N'Uniwersalny'
ORDER BY CASE WHEN Oznaczenie = 'W' THEN 0 ELSE 1 END, NazwaKoloru", CmbKolorSzprosu);

                    PobierzDaneDoCombo(cn, @"SELECT
    Oznaczenie AS Wartosc,
    NazwaHandlowa AS Opis,
    ISNULL(CenaBazowaMb,0) AS Cena1,
    0 AS Cena2
FROM ListwyPodparapetowe
ORDER BY IdListwy", CmbListwa);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd struktur bazy danych SQL: " + ex.Message, "Błąd");
            }
        }

        private void PobierzDaneDoCombo(SqlConnection cn, string sql, ComboBox cmb)
        {
            cmb.Items.Clear();
            cmb.DisplayMemberPath = nameof(SlownikOpcja.Wartosc);
            cmb.SelectedValuePath = nameof(SlownikOpcja.Wartosc);

            using (SqlCommand cmd = new SqlCommand(sql, cn))
            using (SqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    cmb.Items.Add(new SlownikOpcja
                    {
                        Wartosc = dr["Wartosc"].ToString() ?? string.Empty,
                        Opis = dr["Opis"].ToString() ?? string.Empty,
                        Cena1 = dr["Cena1"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Cena1"]),
                        Cena2 = dr["Cena2"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Cena2"])
                    });
                }
            }

            if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
        }

        private void ZaladujProduktyDlaRodzaju(string kodRodzaju)
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    string sql = @"SELECT
    UPPER(t.KodProduktu) AS Wartosc,
    t.NazwaKonstrukcji AS Opis,
    ISNULL(t.CenaBazowaKonstrukcji,0) AS Cena1,
    0 AS Cena2
FROM TypyKonstrukcji t
JOIN RodzajePozycji r ON r.IdRodzaju = t.IdRodzaju
WHERE r.KodRodzaju = @Rodzaj
ORDER BY t.KodProduktu";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@Rodzaj", kodRodzaju);
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            CmbProdukt.Items.Clear();
                            CmbProdukt.DisplayMemberPath = nameof(SlownikOpcja.Wartosc);
                            CmbProdukt.SelectedValuePath = nameof(SlownikOpcja.Wartosc);

                            while (dr.Read())
                            {
                                CmbProdukt.Items.Add(new SlownikOpcja
                                {
                                    Wartosc = dr["Wartosc"].ToString() ?? string.Empty,
                                    Opis = dr["Opis"].ToString() ?? string.Empty,
                                    Cena1 = dr["Cena1"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Cena1"]),
                                    Cena2 = dr["Cena2"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Cena2"])
                                });
                            }
                        }
                    }
                }

                if (CmbProdukt.Items.Count > 0)
                    CmbProdukt.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Blad ladowania produktow dla rodzaju pozycji: " + ex.Message, "Blad");
            }
        }

        private void GridPozycje_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var pos = WybranaPozycja;
            if (pos == null)
            {
                PanelKonfiguratora.Visibility = Visibility.Collapsed;
                return;
            }
            _blokadaOdswiezania = true;
            PanelKonfiguratora.Visibility = Visibility.Visible;

            CmbSystemOkna.Text = pos.SystemOkna;
            TxtIlosc.Text = pos.Szt.ToString();
            CmbRodzajKonstrukcji.Text = pos.Rodzaj;
            ZaladujProduktyDlaRodzaju(pos.Rodzaj);
            CmbProdukt.Text = pos.NrProd;
            TxtSzerokosc.Text = pos.Szerokosc.ToString();
            TxtWysokosc.Text = pos.Wysokosc.ToString();
            CmbWypelnienie.Text = pos.Wypelnienie;

            CmbSzybaZewnetrzna.Text = pos.SzybaZewnetrzna;
            CmbSzybaWewnetrzna.Text = pos.SzybaWewnetrzna;

            CmbTypRamki.Text = pos.TypRamki;
            CmbWariantKoloru.Text = pos.WariantUkladuKoloru;
            CmbKolorOkleiny.Text = pos.KolorOkleiny;

            CmbKolorWewnetrzny.Text = pos.KolorWewnetrzny;

            CmbUszczelka.Text = pos.KolorUszczelki;
            CmbKolorBazy.Text = pos.KolorBazy;
            CmbKlasaBezp.Text = pos.KlasaBezpieczenstwa;
            CmbWariantOkuc.Text = pos.WariantOkuc;
            CmbZawiasy.Text = pos.Zawiasy;

            CmbKolorZawiasow.Text = pos.KolorZawiasow;

            CmbTypKlamki.Text = pos.TypKlamki;
            CmbKolorKlamki.Text = pos.KolorKlamki;

            CmbWysokoscKlamki.Text = pos.WysokoscKlamki;

            CmbListwa.Text = pos.ListwaPodparapetowa;

            CmbTypSzprosu.Text = pos.TypSzprosu;
            CmbSzpros.Text = pos.Szpros;
            CmbKolorSzprosu.Text = pos.KolorSzprosu;
            UstawWidocznoscKolorow();
            UstawWidocznoscSekcjiKonfiguratora(pos);

            ActualizeLabelsDescriptor();
            PrzeliczFinanseZlecenia();

            TxtDetalPozycji.Text = ZbudujOpisPozycji(pos);

            _blokadaOdswiezania = false;
            RysujGabarytyOkna();
        }

        private void Konfigurator_InputChanged(object sender, EventArgs e)
        {
            var pos = WybranaPozycja;
            if (_blokadaOdswiezania || pos == null) return;

            bool bylNaglowkiemZestawu = pos.CzyNaglowekZestawu;
            string staryNumerPozycji = pos.Poz;

            if (sender == CmbRodzajKonstrukcji)
            {
                ZaladujProduktyDlaRodzaju(WartoscCombo(CmbRodzajKonstrukcji));
            }

            pos.SystemOkna = WartoscCombo(CmbSystemOkna);
            pos.Rodzaj = WartoscCombo(CmbRodzajKonstrukcji);
            pos.NrProd = WartoscCombo(CmbProdukt);
            pos.Wypelnienie = WartoscCombo(CmbWypelnienie);
            pos.TypRamki = WartoscCombo(CmbTypRamki);
            pos.WariantUkladuKoloru = WartoscCombo(CmbWariantKoloru);
            UstawWidocznoscKolorow();
            pos.SzybaZewnetrzna = WartoscCombo(CmbSzybaZewnetrzna);
            pos.SzybaWewnetrzna = WartoscCombo(CmbSzybaWewnetrzna);
            pos.KolorOkleiny = WartoscCombo(CmbKolorOkleiny);
            pos.KolorWewnetrzny = WartoscCombo(CmbKolorWewnetrzny);
            pos.KolorZawiasow = WartoscCombo(CmbKolorZawiasow);
            pos.WysokoscKlamki = WartoscCombo(CmbWysokoscKlamki);
            pos.TypSzprosu = WartoscCombo(CmbTypSzprosu);
            pos.Szpros = WartoscCombo(CmbSzpros);
            pos.KolorSzprosu = WartoscCombo(CmbKolorSzprosu);
            pos.KolorUszczelki = WartoscCombo(CmbUszczelka);
            pos.KolorBazy = WartoscCombo(CmbKolorBazy);
            pos.KlasaBezpieczenstwa = WartoscCombo(CmbKlasaBezp);
            pos.WariantOkuc = WartoscCombo(CmbWariantOkuc);
            pos.Zawiasy = WartoscCombo(CmbZawiasy);
            pos.TypKlamki = WartoscCombo(CmbTypKlamki);
            pos.KolorKlamki = WartoscCombo(CmbKolorKlamki);
            pos.ListwaPodparapetowa = WartoscCombo(CmbListwa);

            if (int.TryParse(TxtIlosc.Text, out int szt) && szt > 0) pos.Szt = szt;
            if (int.TryParse(TxtSzerokosc.Text, out int sz)) pos.Szerokosc = sz;
            if (int.TryParse(TxtWysokosc.Text, out int wy)) pos.Wysokosc = wy;

            pos.Oznaczenie = $"{pos.NrProd} / {pos.Wypelnienie}";

            if (bylNaglowkiemZestawu && !pos.CzyNaglowekZestawu)
            {
                UsunPodpozycjeZestawu(staryNumerPozycji);
            }

            if (pos.CzyNaglowekZestawu && CzyZmianaBudujacaZestaw(sender))
            {
                UtworzPodpozycjeZestawu(pos);
            }

            UstawWidocznoscSekcjiKonfiguratora(pos);
            ActualizeLabelsDescriptor();
            PrzeliczFinanseZlecenia();
            RysujGabarytyOkna();
            TxtDetalPozycji.Text = ZbudujOpisPozycji(pos);
        }
        private string WartoscCombo(ComboBox combo)
        {
            return combo.SelectedItem is SlownikOpcja opcja ? opcja.Wartosc : combo.Text;
        }

        private string OpisCombo(ComboBox combo)
        {
            return combo.SelectedItem is SlownikOpcja opcja ? opcja.Opis : string.Empty;
        }

        private string ZbudujOpisPozycji(PozycjaZlecenia pos)
        {
            string status = pos.OdstepstwoZaakceptowane
                ? $"Odstępstwo zaakceptowane: {pos.KomentarzTechnologa}"
                : pos.StatusZablokowany
                    ? "Wymaga akceptacji technologa"
                    : "Gabaryty poprawne";

            string cena = pos.CzyCenaReczna ? $"Cena ręczna netto: {pos.CenaRecznaNetto:N2}" : "Cena liczona automatycznie";

            return $"========================================\n" +
            $" PARAMETRY POZYCJI: {pos.Poz} | PROFIL: {pos.SystemOkna}\n" +
            $"========================================\n" +
            $" • Typ konstrukcji: {pos.Oznaczenie}\n" +
            $" • Gabaryty ościeżnicy: {pos.Szerokosc} x {pos.Wysokosc} mm\n" +
            $" • Pakiet szklenia: {pos.Wypelnienie} mm (Ramka: {pos.TypRamki})\n" +
            $"   - Szyba zewnętrzna: {pos.SzybaZewnetrzna}\n" +
            $"   - Szyba wewnętrzna: {pos.SzybaWewnetrzna}\n" +
            $" • Kolorystyka: Okleina {pos.KolorOkleiny} | Rdzeń/Baza: {pos.KolorBazy}\n" +
            $"   - Kolor zewnętrzny / wewnętrzny: {pos.KolorOkleiny} / {pos.KolorWewnetrzny}\n" +
            $" • Uszczelnienie: Color {pos.KolorUszczelki}\n" +
            $" • Mechanizm okuć: {pos.WariantOkuc} (Klasa odporności: {pos.KlasaBezpieczenstwa})\n" +
            $"   - Kolor zawiasów: {pos.KolorZawiasow}\n" +
            $" • Klamka: {pos.TypKlamki} ({pos.KolorKlamki})\n" +
            $"   - Wysokość klamki: {pos.WysokoscKlamki}\n" +
            $" • Szprosy: {pos.TypSzprosu} {pos.Szpros} {pos.KolorSzprosu}\n" +
            $" • Listwa transportowa: {pos.ListwaPodparapetowa}\n" +
            $" • Status techniczny: {status}\n" +
            $" • Tryb ceny: {cena}\n" +
            $" • Uwagi: {pos.UwagiPozycji}\n" +
            $"========================================\n" +
            $" Architektura dostępu: SQL Server / RAW SQL Mode";
        }
        private SlownikOpcja? OpcjaCombo(ComboBox combo)
        {
            return combo.SelectedItem as SlownikOpcja;
        }

        private double CenaCombo(ComboBox combo, int nr = 1)
        {
            var opcja = OpcjaCombo(combo);
            if (opcja == null) return 0;
            return nr == 2 ? (double)opcja.Cena2 : (double)opcja.Cena1;
        }

        private void UstawWidocznoscKolorow()
        {
            string uklad = WartoscCombo(CmbWariantKoloru);

            bool zewDekor = uklad == "D-W" || uklad == "D-D" || uklad == "BICOLOR";
            bool wewDekor = uklad == "W-D" || uklad == "D-D" || uklad == "BICOLOR";

            CmbKolorOkleiny.IsEnabled = zewDekor;
            CmbKolorWewnetrzny.IsEnabled = wewDekor;

            if (!zewDekor) CmbKolorOkleiny.SelectedValue = "W";
            if (!wewDekor) CmbKolorWewnetrzny.SelectedValue = "W";
        }

        private double PobierzCeneM2Systemu(string systemOkna)
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    string sql = @"SELECT TOP 1 CenaBazowaM2
FROM SystemyProfilowe
WHERE @System = Producent + ' ' + NazwaSystemu";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@System", systemOkna);
                        object? res = cmd.ExecuteScalar();
                        return res == null || res == DBNull.Value ? 410 : Convert.ToDouble(res);
                    }
                }
            }
            catch
            {
                return 410;
            }
        }

        private double PobierzCeneProsta(string sql, string parametr, string wartosc)
        {
            if (string.IsNullOrWhiteSpace(wartosc)) return 0;
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue(parametr, wartosc);
                        object? res = cmd.ExecuteScalar();
                        return res == null || res == DBNull.Value ? 0 : Convert.ToDouble(res);
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private string SprawdzOgraniczeniaZBazy(PozycjaZlecenia pos)
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    string sql = @"SELECT TOP 1
    [MinSzerokość], [MaxSzerokość], [MinWysokość], [MaxWysokość], MaxPowierzchnia, UwagiTechnologa
FROM OgraniczeniaSystemowe
WHERE (KodProduktu = @KodProduktu OR KodProduktu IS NULL)
  AND (SystemNazwa = @System OR SystemNazwa = N'DEFAULT' OR SystemNazwa IS NULL)
ORDER BY
    CASE WHEN KodProduktu = @KodProduktu THEN 0 ELSE 1 END,
    CASE WHEN SystemNazwa = @System THEN 0 WHEN SystemNazwa = N'DEFAULT' THEN 1 ELSE 2 END,
    IdOgraniczenia DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@KodProduktu", pos.NrProd);
                        cmd.Parameters.AddWithValue("@System", pos.SystemOkna);
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (!dr.Read()) return string.Empty;

                            int minS = Convert.ToInt32(dr["MinSzerokość"]);
                            int maxS = Convert.ToInt32(dr["MaxSzerokość"]);
                            int minW = Convert.ToInt32(dr["MinWysokość"]);
                            int maxW = Convert.ToInt32(dr["MaxWysokość"]);
                            double maxM2 = Convert.ToDouble(dr["MaxPowierzchnia"]);
                            double m2 = (pos.Szerokosc / 1000.0) * (pos.Wysokosc / 1000.0);
                            string opis = dr["UwagiTechnologa"]?.ToString() ?? pos.NrProd;

                            if (pos.Szerokosc < minS) return $"{opis}: szerokość {pos.Szerokosc} mm jest mniejsza niż minimum {minS} mm.";
                            if (pos.Szerokosc > maxS) return $"{opis}: szerokość {pos.Szerokosc} mm przekracza maksimum {maxS} mm.";
                            if (pos.Wysokosc < minW) return $"{opis}: wysokość {pos.Wysokosc} mm jest mniejsza niż minimum {minW} mm.";
                            if (pos.Wysokosc > maxW) return $"{opis}: wysokość {pos.Wysokosc} mm przekracza maksimum {maxW} mm.";
                            if (m2 > maxM2) return $"{opis}: powierzchnia {m2:N2} m2 przekracza maksimum {maxM2:N2} m2.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SQL Error (OgraniczeniaSystemowe): " + ex.Message);
            }

            return string.Empty;
        }

        private bool CzyZmianaBudujacaZestaw(object sender)
        {
            return sender == CmbProdukt || sender == CmbRodzajKonstrukcji ||
                   sender == TxtSzerokosc || sender == TxtWysokosc || sender == TxtIlosc ||
                   sender == CmbSystemOkna || sender == CmbWypelnienie || sender == CmbTypRamki ||
                   sender == CmbWariantKoloru || sender == CmbKolorOkleiny || sender == CmbKolorWewnetrzny ||
                   sender == CmbUszczelka || sender == CmbKolorBazy;
        }

        private void UstawWidocznoscSekcjiKonfiguratora(PozycjaZlecenia pos)
        {
            if (pos.CzyNaglowekZestawu)
            {
                ExpDomyslne.Visibility = Visibility.Visible;
                ExpKolory.Visibility = Visibility.Visible;
                ExpOkucia.Visibility = Visibility.Collapsed;
                ExpKlamka.Visibility = Visibility.Collapsed;
                ExpSzprosy.Visibility = Visibility.Collapsed;
                ExpListwa.Visibility = Visibility.Collapsed;
                return;
            }

            if (pos.CzyElementZestawu)
            {
                ExpDomyslne.Visibility = Visibility.Collapsed;
                ExpKolory.Visibility = Visibility.Collapsed;
                ExpOkucia.Visibility = Visibility.Visible;
                ExpKlamka.Visibility = Visibility.Visible;
                ExpSzprosy.Visibility = Visibility.Visible;
                ExpListwa.Visibility = Visibility.Visible;
                return;
            }

            ExpDomyslne.Visibility = Visibility.Visible;
            ExpKolory.Visibility = Visibility.Visible;
            ExpOkucia.Visibility = Visibility.Visible;
            ExpKlamka.Visibility = Visibility.Visible;
            ExpSzprosy.Visibility = Visibility.Visible;
            ExpListwa.Visibility = Visibility.Visible;
        }

        private sealed class ElementZestawuDef
        {
            public int Lp { get; set; }
            public string KodElementu { get; set; } = string.Empty;
            public decimal UdzialSzerokosci { get; set; }
            public int DomyslnaSzerokosc { get; set; }
            public int DomyslnaWysokosc { get; set; }
            public string Opis { get; set; } = string.Empty;
        }

        private void UsunPodpozycjeZestawu(string pozZestawu)
        {
            var doUsuniecia = ListaPozycji
                .Where(p => p.CzyElementZestawu && p.ParentPoz == pozZestawu)
                .ToList();

            foreach (var dziecko in doUsuniecia)
                ListaPozycji.Remove(dziecko);
        }

        private void UtworzPodpozycjeZestawu(PozycjaZlecenia zestaw)
        {
            if (_blokadaBudowyZestawu || !zestaw.CzyNaglowekZestawu) return;

            try
            {
                _blokadaBudowyZestawu = true;

                var elementy = PobierzElementyZestawu(zestaw.NrProd);
                if (elementy.Count == 0) return;

                UsunPodpozycjeZestawu(zestaw.Poz);

                int szerokoscCalkowita = zestaw.Szerokosc > 0
                    ? zestaw.Szerokosc
                    : elementy.Sum(x => Math.Max(0, x.DomyslnaSzerokosc));

                int wysokoscCalkowita = zestaw.Wysokosc > 0
                    ? zestaw.Wysokosc
                    : elementy.Max(x => Math.Max(0, x.DomyslnaWysokosc));

                zestaw.Szerokosc = szerokoscCalkowita;
                zestaw.Wysokosc = wysokoscCalkowita;

                int indexZestawu = ListaPozycji.IndexOf(zestaw);
                int insertIndex = indexZestawu + 1;

                foreach (var def in elementy)
                {
                    int szerokoscDziecka = def.UdzialSzerokosci > 0
                        ? Math.Max(1, (int)Math.Round(szerokoscCalkowita * (double)def.UdzialSzerokosci))
                        : def.DomyslnaSzerokosc;

                    var dziecko = new PozycjaZlecenia
                    {
                        Poz = string.Empty,
                        ParentPoz = zestaw.Poz,
                        CzyElementZestawu = true,
                        NrProd = def.KodElementu,
                        Rodzaj = "J",
                        Szt = zestaw.Szt,
                        Oznaczenie = $"{def.KodElementu} / {zestaw.Wypelnienie}",
                        Szerokosc = szerokoscDziecka,
                        Wysokosc = wysokoscCalkowita > 0 ? wysokoscCalkowita : def.DomyslnaWysokosc,
                        SystemOkna = zestaw.SystemOkna,
                        Wypelnienie = zestaw.Wypelnienie,
                        SzybaZewnetrzna = zestaw.SzybaZewnetrzna,
                        SzybaWewnetrzna = zestaw.SzybaWewnetrzna,
                        TypRamki = zestaw.TypRamki,
                        WariantUkladuKoloru = zestaw.WariantUkladuKoloru,
                        KolorOkleiny = zestaw.KolorOkleiny,
                        KolorWewnetrzny = zestaw.KolorWewnetrzny,
                        KolorUszczelki = zestaw.KolorUszczelki,
                        KolorBazy = zestaw.KolorBazy,
                        KlasaBezpieczenstwa = "Standard",
                        WariantOkuc = "UR-P",
                        Zawiasy = "Standard",
                        KolorZawiasow = "Bialy",
                        TypKlamki = "Klamka aluminiowa Standard",
                        KolorKlamki = "Bialy",
                        WysokoscKlamki = "S",
                        TypSzprosu = string.Empty,
                        Szpros = string.Empty,
                        KolorSzprosu = zestaw.KolorOkleiny,
                        ListwaPodparapetowa = "TAK"
                    };

                    ListaPozycji.Insert(insertIndex, dziecko);
                    insertIndex++;
                }

                PrzebudujNumeracjePozycji();
            }
            finally
            {
                _blokadaBudowyZestawu = false;
            }
        }

        private List<ElementZestawuDef> PobierzElementyZestawu(string kodZestawu)
        {
            var wynik = new List<ElementZestawuDef>();

            using (SqlConnection cn = new SqlConnection(_connString))
            {
                cn.Open();
                string sql = @"SELECT e.Lp, e.KodElementu, e.UdzialSzerokosci, e.DomyslnaSzerokosc, e.DomyslnaWysokosc,
       ISNULL(e.Opis, t.NazwaKonstrukcji) AS Opis
FROM ZestawyElementy e
LEFT JOIN TypyKonstrukcji t ON t.KodProduktu = e.KodElementu
WHERE e.KodZestawu = @kod
ORDER BY e.Lp";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@kod", kodZestawu);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            wynik.Add(new ElementZestawuDef
                            {
                                Lp = Convert.ToInt32(dr["Lp"]),
                                KodElementu = dr["KodElementu"].ToString() ?? string.Empty,
                                UdzialSzerokosci = dr["UdzialSzerokosci"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["UdzialSzerokosci"]),
                                DomyslnaSzerokosc = dr["DomyslnaSzerokosc"] == DBNull.Value ? 0 : Convert.ToInt32(dr["DomyslnaSzerokosc"]),
                                DomyslnaWysokosc = dr["DomyslnaWysokosc"] == DBNull.Value ? 0 : Convert.ToInt32(dr["DomyslnaWysokosc"]),
                                Opis = dr["Opis"].ToString() ?? string.Empty
                            });
                        }
                    }
                }
            }

            return wynik;
        }
        private void ActualizeLabelsDescriptor()
        {
            var pos = WybranaPozycja;
            if (pos == null) return;

            TxtOznaczSystem.Text = OpisCombo(CmbSystemOkna);
            TxtOznaczRodzaj.Text = OpisCombo(CmbRodzajKonstrukcji);
            TxtOznaczProdukt.Text = OpisCombo(CmbProdukt);
            TxtOznaczWyp.Text = OpisCombo(CmbWypelnienie);
            TxtOznaczRamka.Text = OpisCombo(CmbTypRamki);
            TxtOznaczWariantKolor.Text = OpisCombo(CmbWariantKoloru);
            TxtOznaczKolorOkleiny.Text = OpisCombo(CmbKolorOkleiny);
            TxtOznaczUszczelka.Text = OpisCombo(CmbUszczelka);
            TxtOznaczBaza.Text = OpisCombo(CmbKolorBazy);
            TxtOznaczKlasa.Text = OpisCombo(CmbKlasaBezp);
            TxtOznaczWariant.Text = OpisCombo(CmbWariantOkuc);
            TxtOznaczZawiasy.Text = OpisCombo(CmbZawiasy);
            TxtOznaczKlamka.Text = OpisCombo(CmbTypKlamki);
            TxtOznaczKolorKlamki.Text = OpisCombo(CmbKolorKlamki);
            TxtOznaczListwa.Text = OpisCombo(CmbListwa);
            TxtOznaczSzybaZewnetrzna.Text = OpisCombo(CmbSzybaZewnetrzna);
            TxtOznaczSzybaWewnetrzna.Text = OpisCombo(CmbSzybaWewnetrzna);
            TxtOznaczKolorWewnetrzny.Text = OpisCombo(CmbKolorWewnetrzny);
            TxtOznaczKolorZawiasow.Text = OpisCombo(CmbKolorZawiasow);
            TxtOznaczWysokoscKlamki.Text = OpisCombo(CmbWysokoscKlamki);
            TxtOznaczTypSzprosu.Text = OpisCombo(CmbTypSzprosu);
            TxtOznaczSzpros.Text = OpisCombo(CmbSzpros);
            TxtOznaczKolorSzprosu.Text = OpisCombo(CmbKolorSzprosu);
        }

        private void PrzeliczFinanseZlecenia()
        {
            if (ListaPozycji == null || CmbTransport == null || CmbObszar == null || CmbMontaz == null ||
                TxtSumaWszystkichNetto == null || TxtCenaTransportu == null || TxtCenaMontazu == null ||
                TxtCalkowityKosztBrutto == null || TxtRabatProcent == null || TxtWartoscRabatu == null ||
                TxtCenaPoRabacie == null || TxtCenaAktywnejPoz == null) return;

            string obszarText = (CmbObszar.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Polska";

            string waluta = "PLN";
            double przelicznikWaluty = 1.0;
            double stawkaVat = 0.23;

            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    string sqlQuery = "SELECT Waluta, Kurs, StawkaVat FROM ParametryFinansowe WHERE Kraj = @Kraj";
                    using (SqlCommand cmd = new SqlCommand(sqlQuery, cn))
                    {
                        cmd.Parameters.AddWithValue("@Kraj", obszarText);
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                waluta = dr["Waluta"].ToString() ?? "PLN";
                                przelicznikWaluty = Convert.ToDouble(dr["Kurs"]);
                                stawkaVat = Convert.ToDouble(dr["StawkaVat"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SQL Error (Finanse krajowe): " + ex.Message);
            }

            double sumaWszystkichNetto = 0;
            double cenaAktywnejPozycjiNetto = 0;
            int lacznaIloscOkienwZleceniu = 0;

            foreach (var pos in ListaPozycji)
            {
                if (pos.CzyNaglowekZestawu) continue;
                if (pos.Szerokosc == 0 || pos.Wysokosc == 0) continue;

                lacznaIloscOkienwZleceniu += pos.Szt;

                double m2 = (pos.Szerokosc / 1000.0) * (pos.Wysokosc / 1000.0);
                double obwodMb = 2 * ((pos.Szerokosc / 1000.0) + (pos.Wysokosc / 1000.0));

                double cenaProfilM2 = PobierzCeneM2Systemu(pos.SystemOkna);

                double cenaPakietM2 = PobierzCeneProsta(
                    "SELECT CenaBazowaM2 FROM PakietySzybowe WHERE Oznaczenie = @v",
                    "@v", pos.Wypelnienie);

                double cenaSzybaZewM2 = PobierzCeneProsta(
                    "SELECT CenaDoplatyM2 FROM SzybyKomponenty WHERE Oznaczenie = @v",
                    "@v", pos.SzybaZewnetrzna);

                double cenaSzybaWewM2 = PobierzCeneProsta(
                    "SELECT CenaDoplatyM2 FROM SzybyKomponenty WHERE Oznaczenie = @v",
                    "@v", pos.SzybaWewnetrzna);

                double doplataKolorZewProc = PobierzCeneProsta(
                    "SELECT DoplataProcentowa FROM KoloryOklein WHERE Producent = N'Uniwersalny' AND Oznaczenie = @v",
                    "@v", pos.KolorOkleiny);

                double doplataKolorWewProc = PobierzCeneProsta(
                    "SELECT DoplataProcentowa FROM KoloryOklein WHERE Producent = N'Uniwersalny' AND Oznaczenie = @v",
                    "@v", pos.KolorWewnetrzny);

                double cenaRamkaMb = PobierzCeneProsta(
                    "SELECT CenaDoplatyMb FROM RamkiDystansowe WHERE Oznaczenie = @v",
                    "@v", pos.TypRamki);

                double cenaKlasa = PobierzCeneProsta(
                    "SELECT CenaDoplaty FROM OkuciaKlasyBezpieczenstwa WHERE Oznaczenie = @v",
                    "@v", pos.KlasaBezpieczenstwa);

                double cenaOkuc = PobierzCeneProsta(
                    "SELECT CenaBazowaOkucia FROM OkuciaWariantyOtwierania WHERE Oznaczenie = @v",
                    "@v", pos.WariantOkuc);

                double cenaZawiasy = PobierzCeneProsta(
                    "SELECT CenaDoplaty FROM OkuciaZawiasy WHERE Oznaczenie = @v",
                    "@v", pos.Zawiasy);

                double cenaKolorZawiasow = PobierzCeneProsta(
                    "SELECT CenaDoplaty FROM OkuciaKoloryOslonek WHERE NazwaKoloru = @v",
                    "@v", pos.KolorZawiasow);

                double cenaKlamka = PobierzCeneProsta(
                    "SELECT CenaBazowa FROM KlamkiKatalog WHERE NazwaHandlowa = @v",
                    "@v", pos.TypKlamki);

                double cenaKolorKlamki = PobierzCeneProsta(
                    "SELECT CenaDoplaty FROM KlamkiKolory WHERE NazwaKoloru = @v",
                    "@v", pos.KolorKlamki);

                double cenaWysokoscKlamki = PobierzCeneProsta(
                    "SELECT Doplata FROM KlamkiWysokosci WHERE Oznaczenie = @v",
                    "@v", pos.WysokoscKlamki);

                double cenaListwaMb = PobierzCeneProsta(
                    "SELECT CenaBazowaMb FROM ListwyPodparapetowe WHERE Oznaczenie = @v",
                    "@v", pos.ListwaPodparapetowa);

                double cenaSzprosMb = PobierzCeneProsta(
                    "SELECT TOP 1 CenaBazowaMb FROM SzprosyKatalog WHERE CAST(SzerokoscMm AS nvarchar(10)) = @v",
                    "@v", pos.Szpros);

                double cenaBazowaPozycji =
                    (m2 * (cenaProfilM2 + cenaPakietM2 + cenaSzybaZewM2 + cenaSzybaWewM2)) +
                    (obwodMb * cenaRamkaMb) +
                    cenaKlasa + cenaOkuc + cenaZawiasy + cenaKolorZawiasow +
                    cenaKlamka + cenaKolorKlamki + cenaWysokoscKlamki +
                    (pos.ListwaPodparapetowa == "TAK" ? obwodMb * cenaListwaMb : 0) +
                    (string.IsNullOrWhiteSpace(pos.Szpros) ? 0 : obwodMb * cenaSzprosMb);

                double doplataKolorProc = Math.Max(doplataKolorZewProc, doplataKolorWewProc);
                cenaBazowaPozycji += cenaBazowaPozycji * (doplataKolorProc / 100.0);

                double wycenaPozycji = pos.CzyCenaReczna
                    ? (pos.CenaRecznaNetto * pos.Szt) * przelicznikWaluty
                    : (cenaBazowaPozycji * pos.Szt) * przelicznikWaluty;

                sumaWszystkichNetto += wycenaPozycji;

                if (pos == WybranaPozycja) cenaAktywnejPozycjiNetto = wycenaPozycji;

            }

            double stawkaZaJednoOknoTransport = 30;
            double stawkaZaJednoOknoMontaz = 150;

            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    string sqlUslugi = "SELECT stawka_transport, stawka_montaz FROM CennikUslug WHERE Obszar = @Obszar";
                    using (SqlCommand cmd = new SqlCommand(sqlUslugi, cn))
                    {
                        cmd.Parameters.AddWithValue("@Obszar", obszarText);
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                stawkaZaJednoOknoTransport = Convert.ToDouble(dr["stawka_transport"]);
                                stawkaZaJednoOknoMontaz = Convert.ToDouble(dr["stawka_montaz"]);
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SQL Error (CennikUslug): " + ex.Message);
            }

            string transportText = (CmbTransport.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "NIE";
            string montazText = (CmbMontaz.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "NIE";

            if (!double.TryParse(TxtRabatProcent.Text, out double rabatProc)) rabatProc = 0;
            double wartoscRabatu = sumaWszystkichNetto * (rabatProc / 100.0);
            double cenaPoRabacie = sumaWszystkichNetto - wartoscRabatu;

            double kosztTransportu = 0;
            if (transportText == "TAK")
            {
                kosztTransportu = (stawkaZaJednoOknoTransport * lacznaIloscOkienwZleceniu) * przelicznikWaluty;
                if (obszarText == "Niemcy") kosztTransportu += (1000 * przelicznikWaluty);
            }

            double kosztMontazu = 0;
            if (montazText == "TAK")
            {
                kosztMontazu = (stawkaZaJednoOknoMontaz * lacznaIloscOkienwZleceniu) * przelicznikWaluty;
            }

            double calkowityKosztBrutto = (cenaPoRabacie + kosztTransportu + kosztMontazu) * (1.0 + stawkaVat);

            TxtCenaAktywnejPoz.Text = $"{cenaAktywnejPozycjiNetto:N2} {waluta}";
            TxtSumaWszystkichNetto.Text = $"{sumaWszystkichNetto:N2} {waluta}";
            TxtWartoscRabatu.Text = $"{wartoscRabatu:N2} {waluta}";
            TxtCenaPoRabacie.Text = $"{cenaPoRabacie:N2} {waluta}";
            TxtCenaMontazu.Text = $"{kosztMontazu:N2} {waluta}";
            TxtCenaTransportu.Text = $"{kosztTransportu:N2} {waluta}";
            TxtCalkowityKosztBrutto.Text = $"{calkowityKosztBrutto:N2} {waluta}";
        }
        private string? PobierzSciezkeGrafikiKonstrukcji(PozycjaZlecenia pos)
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    string sql = @"SELECT TOP 1 SciezkaPliku
FROM GrafikiKonstrukcji
WHERE KodProduktu = @Kod
  AND CzyAktywna = 1
  AND SciezkaPliku IS NOT NULL
ORDER BY
    CASE
        WHEN KolorProfilu = @KolorProfilu AND KolorKlamki = @KolorKlamki AND KolorZawiasow = @KolorZawiasow THEN 0
        WHEN KolorProfilu = @KolorProfilu AND (KolorKlamki = 'DEFAULT' OR KolorKlamki IS NULL) THEN 1
        WHEN KolorProfilu = 'DEFAULT' THEN 2
        ELSE 3
    END";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@Kod", pos.NrProd);
                        cmd.Parameters.AddWithValue("@KolorProfilu", string.IsNullOrWhiteSpace(pos.KolorOkleiny) ? "DEFAULT" : pos.KolorOkleiny);
                        cmd.Parameters.AddWithValue("@KolorKlamki", string.IsNullOrWhiteSpace(pos.KolorKlamki) ? "DEFAULT" : pos.KolorKlamki);
                        cmd.Parameters.AddWithValue("@KolorZawiasow", string.IsNullOrWhiteSpace(pos.KolorZawiasow) ? "DEFAULT" : pos.KolorZawiasow);

                        object? res = cmd.ExecuteScalar();
                        return res == null || res == DBNull.Value ? null : res.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SQL Error (GrafikiKonstrukcji): " + ex.Message);
                return null;
            }
        }

        private string? ZnajdzPlikGrafiki(string sciezkaZBazy)
        {
            string rel = sciezkaZBazy.Replace('/', Path.DirectorySeparatorChar);

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string desktopProject = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "MP_Fenster_Software",
                "MP_Fenster_App");

            string[] kandydaci =
            {
                Path.Combine(baseDir, rel),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", rel)),
                Path.Combine(desktopProject, rel)
            };

            foreach (string path in kandydaci)
            {
                if (File.Exists(path)) return path;
            }

            return null;
        }
        private bool PokazGrafikeKonstrukcji(PozycjaZlecenia pos)
        {
            string? sciezkaZBazy = PobierzSciezkeGrafikiKonstrukcji(pos);
            if (string.IsNullOrWhiteSpace(sciezkaZBazy)) return false;

            string? plik = ZnajdzPlikGrafiki(sciezkaZBazy);
            if (string.IsNullOrWhiteSpace(plik)) return false;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(plik, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                double canvasW = Math.Max(340, CanvasOkno.ActualWidth);
                double canvasH = Math.Max(240, CanvasOkno.ActualHeight);
                double maxW = Math.Max(190, Math.Min(canvasW - 220, 420));
                double maxH = Math.Max(160, Math.Min(canvasH - 82, 270));
                double ratio = 1.0;
                if (pos.Szerokosc > 0 && pos.Wysokosc > 0)
                    ratio = Math.Max(0.55, Math.Min(3.0, pos.Szerokosc / (double)pos.Wysokosc));

                double szer = maxW;
                double wys = szer / ratio;
                if (wys > maxH)
                {
                    wys = maxH;
                    szer = wys * ratio;
                }

                szer = Math.Max(150, szer);
                wys = Math.Max(140, wys);

                var obraz = new System.Windows.Controls.Image
                {
                    Source = bitmap,
                    Width = szer,
                    Height = wys,
                    Stretch = Stretch.Uniform
                };

                double left = Math.Max(26, (canvasW - szer) / 2 - 20);
                double top = Math.Max(18, (canvasH - wys) / 2 - 8);

                Canvas.SetLeft(obraz, left);
                Canvas.SetTop(obraz, top);
                CanvasOkno.Children.Add(obraz);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Blad grafiki konstrukcji: " + ex.Message);
                return false;
            }
        }        private void DodajOpisyWymiarow(PozycjaZlecenia pos)
        {
            if (pos.Szerokosc <= 0 || pos.Wysokosc <= 0) return;

            double canvasW = Math.Max(340, CanvasOkno.ActualWidth);
            double canvasH = Math.Max(240, CanvasOkno.ActualHeight);
            double maxW = Math.Max(190, Math.Min(canvasW - 220, 420));
            double maxH = Math.Max(160, Math.Min(canvasH - 82, 270));
            double ratio = Math.Max(0.55, Math.Min(3.0, pos.Szerokosc / (double)pos.Wysokosc));
            double szer = maxW;
            double wys = szer / ratio;
            if (wys > maxH)
            {
                wys = maxH;
                szer = wys * ratio;
            }

            szer = Math.Max(150, szer);
            wys = Math.Max(140, wys);
            double left = Math.Max(26, (canvasW - szer) / 2 - 20);
            double top = Math.Max(18, (canvasH - wys) / 2 - 8);
            double right = left + szer;
            double bottom = top + wys;

            Border Etykieta(string text)
            {
                return new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(245, 255, 255, 255)),
                    BorderBrush = Brushes.Silver,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(2),
                    Padding = new Thickness(8, 3, 8, 3),
                    Child = new TextBlock
                    {
                        Text = text,
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.DimGray
                    }
                };
            }

            void Linia(double x1, double y1, double x2, double y2)
            {
                CanvasOkno.Children.Add(new System.Windows.Shapes.Line
                {
                    X1 = x1,
                    Y1 = y1,
                    X2 = x2,
                    Y2 = y2,
                    Stroke = Brushes.Silver,
                    StrokeThickness = 1.2
                });

                double angle = Math.Atan2(y2 - y1, x2 - x1);
                Grot(x1, y1, angle + Math.PI);
                Grot(x2, y2, angle);
            }

            void Grot(double x, double y, double angle)
            {
                const double len = 8;
                const double spread = Math.PI / 7;
                CanvasOkno.Children.Add(new System.Windows.Shapes.Line
                {
                    X1 = x,
                    Y1 = y,
                    X2 = x - len * Math.Cos(angle - spread),
                    Y2 = y - len * Math.Sin(angle - spread),
                    Stroke = Brushes.Silver,
                    StrokeThickness = 1.2
                });
                CanvasOkno.Children.Add(new System.Windows.Shapes.Line
                {
                    X1 = x,
                    Y1 = y,
                    X2 = x - len * Math.Cos(angle + spread),
                    Y2 = y - len * Math.Sin(angle + spread),
                    Stroke = Brushes.Silver,
                    StrokeThickness = 1.2
                });
            }

            string szerokosc = $"{pos.Szerokosc} mm";
            string wysokosc = $"{pos.Wysokosc} mm";

            double y = Math.Min(canvasH - 30, bottom + 24);
            double x = Math.Min(canvasW - 82, right + 38);

            Linia(left, y, right, y);
            Linia(x, top, x, bottom);

            var opisSzer = Etykieta(szerokosc);
            opisSzer.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(opisSzer, left + ((szer - opisSzer.DesiredSize.Width) / 2));
            Canvas.SetTop(opisSzer, y - 16);
            CanvasOkno.Children.Add(opisSzer);

            var opisWys = Etykieta(wysokosc);
            opisWys.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(opisWys, Math.Min(canvasW - opisWys.DesiredSize.Width - 8, x + 8));
            Canvas.SetTop(opisWys, top + ((wys - opisWys.DesiredSize.Height) / 2));
            CanvasOkno.Children.Add(opisWys);
        }
private void RysujGabarytyOkna()
        {
            var pos = WybranaPozycja;
            if (CanvasOkno == null || pos == null) return;
            CanvasOkno.Children.Clear();

            bool pokazanoGrafike = PokazGrafikeKonstrukcji(pos);

            if (pos.Szerokosc == 0 || pos.Wysokosc == 0)
            {
                TxtStatusWalidacji.Text = "⚠️ Wymiary ramy równe 0 mm.";
                TxtStatusWalidacji.Foreground = Brushes.Orange;
                TxtExclamation.Visibility = Visibility.Collapsed;
                return;
            }

            if (!pokazanoGrafike)
            {
                double maxDim = Math.Max(pos.Szerokosc, pos.Wysokosc);
                double scale = 180.0 / maxDim;
                double w = pos.Szerokosc * scale;
                double h = pos.Wysokosc * scale;

                var rect = new System.Windows.Shapes.Rectangle
                {
                    Width = w,
                    Height = h,
                    Stroke = Brushes.SteelBlue,
                    StrokeThickness = 3,
                    Fill = Brushes.AliceBlue
                };
                Canvas.SetLeft(rect, (CanvasOkno.ActualWidth - w) / 2);
                Canvas.SetTop(rect, (CanvasOkno.ActualHeight - h) / 2);
                CanvasOkno.Children.Add(rect);
            }

            DodajOpisyWymiarow(pos);

            if (pos.OdstepstwoZaakceptowane)
            {
                TxtStatusWalidacji.Text = "✔ Odstępstwo zaakceptowane przez technologa.";
                TxtStatusWalidacji.Foreground = Brushes.DarkGreen;
                TxtExclamation.Text = "!";
                TxtExclamation.Foreground = Brushes.Green;
                TxtExclamation.Visibility = Visibility.Visible;
                return;
            }

            string bladOgraniczen = SprawdzOgraniczeniaZBazy(pos);
            if (!string.IsNullOrWhiteSpace(bladOgraniczen))
            {
                TxtStatusWalidacji.Text = bladOgraniczen;
                TxtStatusWalidacji.Foreground = Brushes.Red;
                TxtExclamation.Text = "!";
                TxtExclamation.Foreground = Brushes.Red;
                TxtExclamation.Visibility = Visibility.Visible;
                pos.StatusZablokowany = true;
            }
            else
            {
                TxtStatusWalidacji.Text = "✔ Gabaryty techniczne ramy zatwierdzone z tabeli ograniczeń";
                TxtStatusWalidacji.Foreground = Brushes.Green;
                TxtExclamation.Visibility = Visibility.Collapsed;
                pos.StatusZablokowany = false;
            }
        }
        private void DpPreferowanyTermin_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListaPozycji == null) return;
            PrzeliczFinanseZlecenia();
        }
        private void TxtRabatProcent_LostFocus(object sender, RoutedEventArgs e)
        {
            // Zabezpieczenie na wypadek ładowania okna
            if (ListaPozycji == null) return;

            // Przeliczamy ceny po zmianie rabatu
            PrzeliczFinanseZlecenia();
        }
        private void OpcjeFinansowe_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Zabezpieczenie: Jeśli okno się dopiero ładuje i koszyk nie istnieje, nic nie rób
            if (ListaPozycji == null) return;

            // Wymuszamy ponowne przeliczenie finansów i uaktualnienie etykiet walutowych
            PrzeliczFinanseZlecenia();
        }

        private void BtnToggleToolBar_Click(object sender, RoutedEventArgs e)
        {
            if (PasekNarzedzi != null)
                PasekNarzedzi.Visibility = PasekNarzedzi.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BtnPodgladPozycji_Click(object sender, RoutedEventArgs e)
        {
            if (WybranaPozycja != null)
            {
                var okno = new PodgladPozycjiWindow(WybranaPozycja) { Owner = this };
                okno.ShowDialog();
            }
            else
            {
                MessageBox.Show("Zaznacz pozycję z tabeli po prawej stronie, aby wyświetlić pełny podgląd.", "Informacja");
            }
        }

        private void BtnNowaPozycja_Click(object sender, RoutedEventArgs e)
        {
            var aktywna = WybranaPozycja;
            bool dodajJakoElementZestawu = aktywna != null &&
                (aktywna.CzyNaglowekZestawu || aktywna.CzyElementZestawu);

            string parentPoz = string.Empty;
            if (dodajJakoElementZestawu)
            {
                parentPoz = aktywna!.CzyNaglowekZestawu ? aktywna.Poz : aktywna.ParentPoz;
            }

            int nr = ListaPozycji.Count + 1;
            var nowa = new PozycjaZlecenia
            {
                Poz = nr.ToString(),
                NrProd = dodajJakoElementZestawu ? "F100" : "F100",
                Szt = 1,
                Rodzaj = dodajJakoElementZestawu ? "J" : "J",
                CzyElementZestawu = dodajJakoElementZestawu,
                ParentPoz = parentPoz,
                Oznaczenie = "Okno 1 kw.",
                Szerokosc = 0,
                Wysokosc = 0,
                SystemOkna = "Schuco Living MD",
                Wypelnienie = "2-24",
                TypRamki = "ALU",
                WariantUkladuKoloru = "W-W",
                KolorOkleiny = "W",
                KolorUszczelki = "SZARY",
                KolorBazy = "Bialy",
                KlasaBezpieczenstwa = "Standard",
                WariantOkuc = "UR-P",
                Zawiasy = "Standard",
                TypKlamki = "Klamka aluminiowa Standard",
                KolorKlamki = "Bialy",
                ListwaPodparapetowa = "TAK",
                SzybaZewnetrzna = "4",
                SzybaWewnetrzna = "4T",
                KolorWewnetrzny = "W",
                KolorZawiasow = "Bialy",
                WysokoscKlamki = "S",
                TypSzprosu = "",
                Szpros = "",
                KolorSzprosu = "W",
            };
            ListaPozycji.Add(nowa);
            if (dodajJakoElementZestawu && aktywna != null)
            {
                int indexAktywnej = ListaPozycji.IndexOf(aktywna);
                ListaPozycji.Remove(nowa);
                ListaPozycji.Insert(indexAktywnej + 1, nowa);
            }
            PrzebudujNumeracjePozycji();
            GridPozycje.SelectedItem = nowa;
            PrzeliczFinanseZlecenia();
        }

        private void BtnKopiujPozycje_Click(object sender, RoutedEventArgs e)
        {
            var pos = WybranaPozycja;
            if (pos == null) return;
            var kopia = pos.Klonuj();
            kopia.Poz = (ListaPozycji.Count + 1).ToString();
            kopia.Oznaczenie = pos.Oznaczenie + " (Kopia)";
            ListaPozycji.Add(kopia);
            GridPozycje.SelectedItem = kopia;
            PrzeliczFinanseZlecenia();
        }

        private void BtnUsunPozycje_Click(object sender, RoutedEventArgs e)
        {
            var pos = WybranaPozycja;
            if (pos == null) return;
            var res = MessageBox.Show("Usunąć element?", "Potwierdzenie", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.Yes)
            {
                ListaPozycji.Remove(pos);
                PrzebudujNumeracjePozycji();
                PrzeliczFinanseZlecenia();
            }
        }

        private void BtnPrzesunNaGore_Click(object sender, RoutedEventArgs e)
        {
            var pos = WybranaPozycja;
            if (pos == null) return;
            int idx = ListaPozycji.IndexOf(pos);
            if (idx > 0) { ListaPozycji.Move(idx, 0); PrzebudujNumeracjePozycji(); }
        }

        private void BtnPrzesunWGore_Click(object sender, RoutedEventArgs e)
        {
            var pos = WybranaPozycja;
            if (pos == null) return;
            int idx = ListaPozycji.IndexOf(pos);
            if (idx > 0) { ListaPozycji.Move(idx, idx - 1); PrzebudujNumeracjePozycji(); }
        }

        private void BtnPrzesunNaDol_Click(object sender, RoutedEventArgs e)
        {
            var pos = WybranaPozycja;
            if (pos == null) return;
            int idx = ListaPozycji.IndexOf(pos);
            if (idx < ListaPozycji.Count - 1) { ListaPozycji.Move(idx, ListaPozycji.Count - 1); PrzebudujNumeracjePozycji(); }
        }

        private void BtnPrzesunWDol_Click(object sender, RoutedEventArgs e)
        {
            var pos = WybranaPozycja;
            if (pos == null) return;
            int idx = ListaPozycji.IndexOf(pos);
            if (idx < ListaPozycji.Count - 1) { ListaPozycji.Move(idx, idx + 1); PrzebudujNumeracjePozycji(); }
        }

        private void PrzebudujNumeracjePozycji()
        {
            int glownyLicznik = 0;
            string aktualnyZestaw = string.Empty;
            int licznikPodpozycji = 0;

            foreach (var pos in ListaPozycji)
            {
                if (pos.CzyNaglowekZestawu)
                {
                    glownyLicznik++;
                    pos.Poz = glownyLicznik.ToString();
                    pos.ParentPoz = string.Empty;
                    pos.CzyElementZestawu = false;
                    aktualnyZestaw = pos.Poz;
                    licznikPodpozycji = 0;
                    continue;
                }

                if (pos.CzyElementZestawu && !string.IsNullOrWhiteSpace(aktualnyZestaw))
                {
                    licznikPodpozycji++;
                    pos.ParentPoz = aktualnyZestaw;
                    pos.Poz = $"{aktualnyZestaw}.{licznikPodpozycji}";
                    continue;
                }

                glownyLicznik++;
                pos.Poz = glownyLicznik.ToString();
                pos.ParentPoz = string.Empty;
                pos.CzyElementZestawu = false;
                aktualnyZestaw = string.Empty;
                licznikPodpozycji = 0;
            }
        }


        private List<PozycjaZlecenia> PobierzGrupeAkceptacji(PozycjaZlecenia pos, out PozycjaZlecenia pozycjaDoOkna)
        {
            pozycjaDoOkna = pos;

            if (pos.CzyNaglowekZestawu)
            {
                var grupa = ListaPozycji
                    .Where(p => p == pos || (p.CzyElementZestawu && p.ParentPoz == pos.Poz))
                    .ToList();
                return grupa.Count > 0 ? grupa : new List<PozycjaZlecenia> { pos };
            }

            if (pos.CzyElementZestawu && !string.IsNullOrWhiteSpace(pos.ParentPoz))
            {
                var naglowek = ListaPozycji.FirstOrDefault(p => p.Poz == pos.ParentPoz && p.CzyNaglowekZestawu);
                if (naglowek != null)
                {
                    pozycjaDoOkna = naglowek;
                    return ListaPozycji
                        .Where(p => p == naglowek || (p.CzyElementZestawu && p.ParentPoz == naglowek.Poz))
                        .ToList();
                }
            }

            return new List<PozycjaZlecenia> { pos };
        }

        private void ZastosujAkceptacjeDoGrupy(List<PozycjaZlecenia> grupa, string komentarzTechnologa)
        {
            foreach (var element in grupa)
            {
                element.StatusZablokowany = false;
                element.OdstepstwoZaakceptowane = true;
                element.KomentarzTechnologa = komentarzTechnologa;
            }
        }

        private void BtnBledy_Click(object sender, RoutedEventArgs e)
        {
            var pos = WybranaPozycja;
            if (pos == null) return;

            var grupa = PobierzGrupeAkceptacji(pos, out var pozycjaDoOkna);
            bool grupaMaBlad = grupa.Any(p => p.StatusZablokowany);
            bool grupaZaakceptowana = grupa.Any(p => p.OdstepstwoZaakceptowane);

            bool staryStatus = pozycjaDoOkna.StatusZablokowany;
            bool staraAkceptacja = pozycjaDoOkna.OdstepstwoZaakceptowane;
            string staryKomentarz = pozycjaDoOkna.KomentarzTechnologa;

            if (grupa.Count > 1)
            {
                pozycjaDoOkna.StatusZablokowany = grupaMaBlad;
                pozycjaDoOkna.OdstepstwoZaakceptowane = grupaZaakceptowana;

                string komentarzZGrupy = grupa
                    .Select(p => p.KomentarzTechnologa)
                    .FirstOrDefault(k => !string.IsNullOrWhiteSpace(k)) ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(komentarzZGrupy))
                    pozycjaDoOkna.KomentarzTechnologa = komentarzZGrupy;
            }

            var okno = new BledyOgraniczenWindow(pozycjaDoOkna, CzyZalogowanyTechnologLubAdmin())
            {
                Owner = this
            };

            if (okno.ShowDialog() == true)
            {
                ZastosujAkceptacjeDoGrupy(grupa, pozycjaDoOkna.KomentarzTechnologa);
                ZapiszZlecenieDoBazySystemu();
            }
            else if (grupa.Count > 1)
            {
                pozycjaDoOkna.StatusZablokowany = staryStatus;
                pozycjaDoOkna.OdstepstwoZaakceptowane = staraAkceptacja;
                pozycjaDoOkna.KomentarzTechnologa = staryKomentarz;
            }

            RysujGabarytyOkna();
            TxtDetalPozycji.Text = ZbudujOpisPozycji(pos);
        }
        private double ObliczCeneJednostkowaNettoZBazy(PozycjaZlecenia pos)
        {
            if (pos == null || pos.CzyNaglowekZestawu || pos.Szerokosc <= 0 || pos.Wysokosc <= 0)
                return 0;

            if (pos.CzyCenaReczna)
                return pos.CenaRecznaNetto;

            double m2 = (pos.Szerokosc / 1000.0) * (pos.Wysokosc / 1000.0);
            double obwodMb = 2 * ((pos.Szerokosc / 1000.0) + (pos.Wysokosc / 1000.0));

            double cenaProfilM2 = PobierzCeneM2Systemu(pos.SystemOkna);
            double cenaPakietM2 = PobierzCeneProsta("SELECT CenaBazowaM2 FROM PakietySzybowe WHERE Oznaczenie = @v", "@v", pos.Wypelnienie);
            double cenaSzybaZewM2 = PobierzCeneProsta("SELECT CenaDoplatyM2 FROM SzybyKomponenty WHERE Oznaczenie = @v", "@v", pos.SzybaZewnetrzna);
            double cenaSzybaWewM2 = PobierzCeneProsta("SELECT CenaDoplatyM2 FROM SzybyKomponenty WHERE Oznaczenie = @v", "@v", pos.SzybaWewnetrzna);
            double doplataKolorZewProc = PobierzCeneProsta("SELECT DoplataProcentowa FROM KoloryOklein WHERE Producent = N'Uniwersalny' AND Oznaczenie = @v", "@v", pos.KolorOkleiny);
            double doplataKolorWewProc = PobierzCeneProsta("SELECT DoplataProcentowa FROM KoloryOklein WHERE Producent = N'Uniwersalny' AND Oznaczenie = @v", "@v", pos.KolorWewnetrzny);
            double cenaRamkaMb = PobierzCeneProsta("SELECT CenaDoplatyMb FROM RamkiDystansowe WHERE Oznaczenie = @v", "@v", pos.TypRamki);
            double cenaKlasa = PobierzCeneProsta("SELECT CenaDoplaty FROM OkuciaKlasyBezpieczenstwa WHERE Oznaczenie = @v", "@v", pos.KlasaBezpieczenstwa);
            double cenaOkuc = PobierzCeneProsta("SELECT CenaBazowaOkucia FROM OkuciaWariantyOtwierania WHERE Oznaczenie = @v", "@v", pos.WariantOkuc);
            double cenaZawiasy = PobierzCeneProsta("SELECT CenaDoplaty FROM OkuciaZawiasy WHERE Oznaczenie = @v", "@v", pos.Zawiasy);
            double cenaKolorZawiasow = PobierzCeneProsta("SELECT CenaDoplaty FROM OkuciaKoloryOslonek WHERE NazwaKoloru = @v", "@v", pos.KolorZawiasow);
            double cenaKlamka = PobierzCeneProsta("SELECT CenaBazowa FROM KlamkiKatalog WHERE NazwaHandlowa = @v", "@v", pos.TypKlamki);
            double cenaKolorKlamki = PobierzCeneProsta("SELECT CenaDoplaty FROM KlamkiKolory WHERE NazwaKoloru = @v", "@v", pos.KolorKlamki);
            double cenaWysokoscKlamki = PobierzCeneProsta("SELECT Doplata FROM KlamkiWysokosci WHERE Oznaczenie = @v", "@v", pos.WysokoscKlamki);
            double cenaListwaMb = PobierzCeneProsta("SELECT CenaBazowaMb FROM ListwyPodparapetowe WHERE Oznaczenie = @v", "@v", pos.ListwaPodparapetowa);
            double cenaSzprosMb = PobierzCeneProsta("SELECT TOP 1 CenaBazowaMb FROM SzprosyKatalog WHERE CAST(SzerokoscMm AS nvarchar(10)) = @v", "@v", pos.Szpros);

            double cena =
                (m2 * (cenaProfilM2 + cenaPakietM2 + cenaSzybaZewM2 + cenaSzybaWewM2)) +
                (obwodMb * cenaRamkaMb) +
                cenaKlasa + cenaOkuc + cenaZawiasy + cenaKolorZawiasow +
                cenaKlamka + cenaKolorKlamki + cenaWysokoscKlamki +
                (pos.ListwaPodparapetowa == "TAK" ? obwodMb * cenaListwaMb : 0) +
                (string.IsNullOrWhiteSpace(pos.Szpros) ? 0 : obwodMb * cenaSzprosMb);

            double doplataKolorProc = Math.Max(doplataKolorZewProc, doplataKolorWewProc);
            return cena + (cena * (doplataKolorProc / 100.0));
        }
        private void BtnZamknijZapisz_Click(object sender, RoutedEventArgs e)
        {
            ZapiszZlecenieDoBazySystemu();
            this.Close();
        }
        private void ZapiszZlecenieDoBazySystemu()
        {
            if (ListaPozycji.Count == 0)
            {
                MessageBox.Show("Koszyk handlowy jest pusty! Dodaj przynajmniej jedną pozycję przed zatwierdzeniem zlecenia.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    using (SqlTransaction tx = cn.BeginTransaction())
                    {
                        try
                        {
                            int idUzytkownika = 1;
                            string sqlUser = "SELECT IdUzytkownika FROM Uzytkownicy WHERE UPPER(Login) = @login";
                            using (SqlCommand cmdUser = new SqlCommand(sqlUser, cn, tx))
                            {
                                cmdUser.Parameters.AddWithValue("@login", _zalogowanyUzytkownik.ToUpper());
                                object? resUser = cmdUser.ExecuteScalar();
                                if (resUser != null) idUzytkownika = Convert.ToInt32(resUser);
                            }

                            string transportText = (CmbTransport.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "NIE";
                            string montazText = (CmbMontaz.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "NIE";
                            string obszarText = (CmbObszar.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Polska";

                            int idZleceniaDoPozycji = 0;

                            if (_obecneIdZlecenia.HasValue)
                            {
                                idZleceniaDoPozycji = _obecneIdZlecenia.Value;
                                string sqlUpdateHeader = @"UPDATE Zlecenia SET 
                                    IdKlienta = @klient, 
                                    TerminPreferowany = @termin, 
                                    Obszar = @obszar, 
                                    CzyTransport = @trans, 
                                    CzyMontaz = @montaz, 
                                    TypZlecenia = @typ,
                                    Referencja = @ref
                                    WHERE IdZlecenia = @idZlec";

                                using (SqlCommand cmdUpdate = new SqlCommand(sqlUpdateHeader, cn, tx))
                                {
                                    cmdUpdate.Parameters.AddWithValue("@idZlec", idZleceniaDoPozycji);
                                    cmdUpdate.Parameters.AddWithValue("@klient", string.IsNullOrEmpty(TxtIdKlienta.Text) ? 1 : Convert.ToInt32(TxtIdKlienta.Text));
                                    cmdUpdate.Parameters.AddWithValue("@user", idUzytkownika);
                                    cmdUpdate.Parameters.AddWithValue("@termin", DpPreferowanyTermin.SelectedDate ?? DateTime.Today.AddDays(14));
                                    cmdUpdate.Parameters.AddWithValue("@obszar", obszarText);
                                    cmdUpdate.Parameters.AddWithValue("@trans", transportText == "TAK");
                                    cmdUpdate.Parameters.AddWithValue("@montaz", montazText == "TAK");
                                    cmdUpdate.Parameters.AddWithValue("@typ", CmbTypZlecenia.SelectedValue ?? "Standard");
                                    cmdUpdate.Parameters.AddWithValue("@ref", TxtRef.Text.Trim());

                                    cmdUpdate.ExecuteNonQuery();
                                }

                                string sqlDeleteLines = "DELETE FROM PozycjeZlecenia WHERE IdZlecenia = @idZlec";
                                using (SqlCommand cmdDel = new SqlCommand(sqlDeleteLines, cn, tx))
                                {
                                    cmdDel.Parameters.AddWithValue("@idZlec", idZleceniaDoPozycji);
                                    cmdDel.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                string sqlHeader = @"INSERT INTO Zlecenia 
                                    (NumerZlecenia, IdKlienta, IdUzytkownika, DataWprowadzenia, TerminPreferowany, Obszar, CzyTransport, CzyMontaz, TypZlecenia, StatusZlecenia, Referencja) 
                                    VALUES (@nr, @klient, @user, GETDATE(), @termin, @obszar, @trans, @montaz, @typ, 'OFERTA', @ref);
                                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                                using (SqlCommand cmdHeader = new SqlCommand(sqlHeader, cn, tx))
                                {
                                    cmdHeader.Parameters.AddWithValue("@nr", TxtZlecenie.Text);
                                    cmdHeader.Parameters.AddWithValue("@klient", string.IsNullOrEmpty(TxtIdKlienta.Text) ? 1 : Convert.ToInt32(TxtIdKlienta.Text));
                                    cmdHeader.Parameters.AddWithValue("@user", idUzytkownika);
                                    cmdHeader.Parameters.AddWithValue("@termin", DpPreferowanyTermin.SelectedDate ?? DateTime.Today.AddDays(14));
                                    cmdHeader.Parameters.AddWithValue("@obszar", obszarText);
                                    cmdHeader.Parameters.AddWithValue("@trans", transportText == "TAK");
                                    cmdHeader.Parameters.AddWithValue("@montaz", montazText == "TAK");
                                    cmdHeader.Parameters.AddWithValue("@typ", CmbTypZlecenia.SelectedValue ?? "Standard");
                                    cmdHeader.Parameters.AddWithValue("@ref", TxtRef.Text.Trim());

                                    idZleceniaDoPozycji = (int)cmdHeader.ExecuteScalar();
                                    _obecneIdZlecenia = idZleceniaDoPozycji;
                                }
                            }

                            string sqlLine = @"INSERT INTO PozycjeZlecenia 
                                (IdZlecenia, ParentId, NrProdukcyjny, Sztuk, Szerokosc, Wysokosc, CenaJednostkowa, SortOrder, Uwagi, StatusTechniczny, KomentarzTechnologa) 
                                OUTPUT INSERTED.IdPozycji
                                VALUES (@idZlec, @parentId, @nrProd, @szt, @szer, @wys, @cena, @sort, @uwagi, @statusTech, @komentarzTech)";

                            var mapaIdPozycji = new Dictionary<string, int>();
                            int sortOrder = 0;

                            foreach (var pos in ListaPozycji)
                            {
                                sortOrder++;
                                using (SqlCommand cmdLine = new SqlCommand(sqlLine, cn, tx))
                                {
                                    double m2 = (pos.Szerokosc / 1000.0) * (pos.Wysokosc / 1000.0);
                                    double cenaBazowa = pos.SystemOkna.Contains("Salamander") ? 620 : 450;
                                    if (pos.Wypelnienie == "3-48") cenaBazowa += 130;
                                    double cenaJednostkowa = m2 * cenaBazowa;

                                    cmdLine.Parameters.AddWithValue("@idZlec", idZleceniaDoPozycji);
                                    object parentId = DBNull.Value;
                                    if (!string.IsNullOrWhiteSpace(pos.ParentPoz) && mapaIdPozycji.TryGetValue(pos.ParentPoz, out int znalezionyParentId))
                                        parentId = znalezionyParentId;
                                    cmdLine.Parameters.Add("@parentId", SqlDbType.Int).Value = parentId;
                                    cmdLine.Parameters.AddWithValue("@nrProd", pos.NrProd);
                                    cmdLine.Parameters.AddWithValue("@szt", pos.Szt);
                                    cmdLine.Parameters.AddWithValue("@szer", pos.Szerokosc);
                                    cmdLine.Parameters.AddWithValue("@wys", pos.Wysokosc);
                                    cmdLine.Parameters.AddWithValue("@cena", cenaJednostkowa);
                                    cmdLine.Parameters.AddWithValue("@sort", sortOrder);
                                    string uwagi = $"System ramy: {pos.SystemOkna}, Okleina: {pos.KolorOkleiny}, Uwagi: {pos.UwagiPozycji}, Technolog: {pos.KomentarzTechnologa}";
                                    cmdLine.Parameters.AddWithValue("@uwagi", uwagi);
                                    int statusTech = pos.OdstepstwoZaakceptowane ? 2 : (pos.StatusZablokowany ? 0 : 1);
                                    cmdLine.Parameters.AddWithValue("@statusTech", statusTech);
                                    cmdLine.Parameters.AddWithValue("@komentarzTech", pos.KomentarzTechnologa ?? string.Empty);

                                    int zapisaneIdPozycji = Convert.ToInt32(cmdLine.ExecuteScalar());
                                    if (!string.IsNullOrWhiteSpace(pos.Poz))
                                        mapaIdPozycji[pos.Poz] = zapisaneIdPozycji;
                                }
                            }

                            tx.Commit();
                            MessageBox.Show($"Zlecenie numer {TxtZlecenie.Text} wraz z pozycjami zostało pomyślnie zapisane!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            tx.Rollback();
                            throw new Exception("Błąd transakcji zapisu SQL: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd zapisu koszyka: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnZapiszPostepy_Click(object sender, RoutedEventArgs e) => ZapiszZlecenieDoBazySystemu();
        private void BtnWyjdzBezZapisu_Click(object sender, RoutedEventArgs e) { var res = MessageBox.Show("Wyjść bez zapisu?", "Potwierdzenie", MessageBoxButton.YesNo); if (res == MessageBoxResult.Yes) this.Close(); }
        private void BtnDrukuj_Click(object sender, RoutedEventArgs e)
        {
            if (ListaPozycji.Count == 0)
            {
                MessageBox.Show("Dodaj przynajmniej jedną pozycję przed przygotowaniem oferty.", "Informacja");
                return;
            }

            string klient = string.IsNullOrWhiteSpace(TxtWybranyKlient.Text) ? "Nie wybrano klienta" : TxtWybranyKlient.Text;
            var pdf = new PdfOfertaService();
            string path = pdf.UtworzRoboczyHtmlOferty(TxtZlecenie.Text, klient, ListaPozycji);

            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        private void BtnDodajKlienta_Click(object sender, RoutedEventArgs e)
        {
            ZarzadzanieKlientamiWindow oknoSlownika = new ZarzadzanieKlientamiWindow(_zalogowanyUzytkownik, otworzOdRazuDodawanie: true);
            oknoSlownika.Owner = this;

            if (oknoSlownika.ShowDialog() == true)
            {
                TxtIdKlienta.Text = oknoSlownika.WybraneId.ToString();
                TxtWybranyKlient.Text = oknoSlownika.WybranaNazwa;
            }
        }

        private void BtnListaKlientow_Click(object sender, RoutedEventArgs e)
        {
            var oknoWyboru = new WyborKlientaWindow(_connString, _zalogowanyUzytkownik)
            {
                Owner = this
            };

            if (oknoWyboru.ShowDialog() == true)
            {
                TxtIdKlienta.Text = oknoWyboru.WybraneId.ToString();
                TxtWybranyKlient.Text = oknoWyboru.WybranaNazwa;
            }
        }

        private void BtnAktualizujCeny_Click(object sender, RoutedEventArgs e)
        {
            PrzeliczFinanseZlecenia();
            MessageBox.Show("Ceny zostały przeliczone według aktualnych danych z cennika.", "Aktualizacja cen");
        }

        private void BtnKopiujZInnego_Click(object sender, RoutedEventArgs e)
        {
            var okno = new KopiujPozycjeWindow(_connString) { Owner = this };
            if (okno.ShowDialog() == true)
            {
                PozycjaZlecenia? pierwszaDodana = null;
                foreach (var pozycja in okno.WybranePozycje)
                {
                    pozycja.Poz = (ListaPozycji.Count + 1).ToString();
                    ListaPozycji.Add(pozycja);
                    pierwszaDodana ??= pozycja;
                }

                PrzebudujNumeracjePozycji();
                if (pierwszaDodana != null) GridPozycje.SelectedItem = pierwszaDodana;
                PrzeliczFinanseZlecenia();
            }
        }

        private void BtnWstawSpecjalne_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ten przycisk zostawiamy jako miejsce na przyszłe wstawki, np. szprosy lub element pomiędzy pozycjami.", "Funkcja robocza");
        }

        private void BtnOdswiezPozycje_Click(object sender, RoutedEventArgs e)
        {
            LadujSlownikiZBase();
            PrzeliczFinanseZlecenia();
            if (WybranaPozycja != null)
            {
                ActualizeLabelsDescriptor();
                RysujGabarytyOkna();
                TxtDetalPozycji.Text = ZbudujOpisPozycji(WybranaPozycja);
            }
        }

        private void BtnUwagi_Click(object sender, RoutedEventArgs e)
        {
            var pos = WybranaPozycja;
            if (pos == null)
            {
                MessageBox.Show("Zaznacz pozycję, do której chcesz dopisać uwagę.", "Informacja");
                return;
            }

            var okno = new UwagiPozycjiWindow(pos) { Owner = this };
            if (okno.ShowDialog() == true)
            {
                pos.UwagiPozycji = okno.Uwagi;
                TxtDetalPozycji.Text = ZbudujOpisPozycji(pos);
            }
        }

        private void BtnSpecMat_Click(object sender, RoutedEventArgs e)
        {
            var okno = new PodgladPozycjiWindow(ListaPozycji) { Owner = this, Title = "Specyfikacja materiałowa zlecenia" };
            okno.ShowDialog();
        }

        private void BtnKorektaCeny_Click(object sender, RoutedEventArgs e)
        {
            var pos = WybranaPozycja;
            if (pos == null)
            {
                MessageBox.Show("Zaznacz pozycję, której cenę chcesz skorygować.", "Informacja");
                return;
            }

            string aktualna = pos.CzyCenaReczna ? pos.CenaRecznaNetto.ToString("N2") : string.Empty;
            string input = Microsoft.VisualBasic.Interaction.InputBox("Wpisz ręczną cenę netto za jedną sztukę. Zostaw puste, aby wrócić do automatycznego liczenia.", "Korekta ceny", aktualna);

            if (string.IsNullOrWhiteSpace(input))
            {
                pos.CzyCenaReczna = false;
                pos.CenaRecznaNetto = 0;
            }
            else if (double.TryParse(input, out double cena) && cena >= 0)
            {
                pos.CzyCenaReczna = true;
                pos.CenaRecznaNetto = cena;
            }
            else
            {
                MessageBox.Show("Podana cena jest niepoprawna.", "Walidacja");
                return;
            }

            PrzeliczFinanseZlecenia();
            TxtDetalPozycji.Text = ZbudujOpisPozycji(pos);
        }

        public void WczytajIstniejaceZlecenieZBase(int idZlecenia)
        {
            _obecneIdZlecenia = idZlecenia;
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();

                    string sqlHeader = @"SELECT z.NumerZlecenia, z.IdKlienta, z.Obszar, z.CzyTransport, z.CzyMontaz, 
                                                z.TypZlecenia, z.TerminPreferowany, z.DataWprowadzenia, z.Referencja,
                                                u.Login AS Wprowadzil
                                         FROM Zlecenia z
                                         LEFT JOIN Uzytkownicy u ON z.IdUzytkownika = u.IdUzytkownika 
                                         WHERE z.IdZlecenia = @id";

                    using (SqlCommand cmdH = new SqlCommand(sqlHeader, cn))
                    {
                        cmdH.Parameters.AddWithValue("@id", idZlecenia);
                        using (SqlDataReader drH = cmdH.ExecuteReader())
                        {
                            if (drH.Read())
                            {
                                TxtZlecenie.Text = drH["NumerZlecenia"].ToString();
                                if (drH["Wprowadzil"] != DBNull.Value)
                                    TxtWprowadzil.Text = drH["Wprowadzil"].ToString()?.ToUpper() ?? TxtWprowadzil.Text;
                                StUser.Text = _zalogowanyUzytkownik.ToUpper();
                                TxtIdKlienta.Text = drH["IdKlienta"].ToString();
                                CmbObszar.Text = drH["Obszar"].ToString();

                                if (drH["DataWprowadzenia"] != DBNull.Value)
                                {
                                    TxtDataWpr.Text = Convert.ToDateTime(drH["DataWprowadzenia"]).ToString("dd.MM.yyyy");
                                }

                                bool czyTransport = Convert.ToBoolean(drH["CzyTransport"]);
                                CmbTransport.SelectedItem = czyTransport ? CmbTransport.Items[0] : CmbTransport.Items[1];

                                bool czyMontaz = Convert.ToBoolean(drH["CzyMontaz"]);
                                CmbMontaz.SelectedItem = czyMontaz ? CmbMontaz.Items[0] : CmbMontaz.Items[1];

                                if (drH["Referencja"] != DBNull.Value)
                                {
                                    TxtRef.Text = drH["Referencja"].ToString();
                                }
                                else
                                {
                                    TxtRef.Text = string.Empty;
                                }

                                if (drH["TypZlecenia"] != DBNull.Value)
                                {
                                    CmbTypZlecenia.SelectedValue = drH["TypZlecenia"].ToString();
                                }

                                if (drH["TerminPreferowany"] != DBNull.Value)
                                {
                                    DpPreferowanyTermin.SelectedDate = Convert.ToDateTime(drH["TerminPreferowany"]);
                                }
                            }
                        }
                    }

                    string sqlLines = "SELECT * FROM PozycjeZlecenia WHERE IdZlecenia = @id ORDER BY ISNULL(SortOrder, IdPozycji), IdPozycji ASC";
                    using (SqlCommand cmdL = new SqlCommand(sqlLines, cn))
                    {
                        cmdL.Parameters.AddWithValue("@id", idZlecenia);
                        using (SqlDataReader drL = cmdL.ExecuteReader())
                        {
                            ListaPozycji.Clear();
                            int licznik = 1;
                            while (drL.Read())
                            {
                                string uwagiZBase = drL["Uwagi"].ToString() ?? "";
                                string systemOkna = uwagiZBase.Contains("System ramy: ") ? uwagiZBase.Split(',')[0].Replace("System ramy: ", "") : "Schuco Living MD";
                                string nrProdZBase = drL["NrProdukcyjny"].ToString() ?? "F100";
                                bool czyZestaw = nrProdZBase.StartsWith("Z", StringComparison.OrdinalIgnoreCase);
                                bool czyElementZestawu = drL["ParentId"] != DBNull.Value;

                                ListaPozycji.Add(new PozycjaZlecenia
                                {
                                    Poz = licznik.ToString(),
                                    NrProd = nrProdZBase,
                                    Rodzaj = czyZestaw ? "JP" : "J",
                                    CzyElementZestawu = czyElementZestawu,
                                    Szt = Convert.ToInt32(drL["Sztuk"]),
                                    Szerokosc = Convert.ToInt32(drL["Szerokosc"]),
                                    Wysokosc = Convert.ToInt32(drL["Wysokosc"]),
                                    SystemOkna = systemOkna,
                                    Wypelnienie = "2-24",
                                    TypRamki = "ALU",
                                    WariantUkladuKoloru = "W-W",
                                    KolorOkleiny = "W",
                                    KolorUszczelki = "SZARY",
                                    KolorBazy = "Bialy",
                                    KlasaBezpieczenstwa = "Standard",
                                    WariantOkuc = "UR-P",
                                    Zawiasy = "Standard",
                                    TypKlamki = "Klamka aluminiowa Standard",
                                    KolorKlamki = "Bialy",
                                    ListwaPodparapetowa = "TAK",
                                    UwagiPozycji = uwagiZBase,
                                    Oznaczenie = $"{nrProdZBase} / Zrzut",
                                    StatusZablokowany = (drL["StatusTechniczny"].ToString() == "0"),
                                    OdstepstwoZaakceptowane = (drL["StatusTechniczny"].ToString() == "2"),
                                    KomentarzTechnologa = drL["KomentarzTechnologa"] == DBNull.Value ? string.Empty : drL["KomentarzTechnologa"].ToString() ?? string.Empty
                                });
                                licznik++;
                            }
                        }
                    }
                    PrzebudujNumeracjePozycji();
                    PrzeliczFinanseZlecenia();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd krytyczny odtwarzania koszyka zlecenia: " + ex.Message, "Błąd Re-Open");
            }
        }

        private bool CzyZalogowanyTechnologLubAdmin()
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Rola FROM Uzytkownicy WHERE UPPER(Login) = @login", cn))
                    {
                        cmd.Parameters.AddWithValue("@login", _zalogowanyUzytkownik.ToUpper());
                        string rola = cmd.ExecuteScalar()?.ToString() ?? string.Empty;
                        return rola.Equals("Technolog", StringComparison.OrdinalIgnoreCase) ||
                               rola.Equals("Admin", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch
            {
                return _zalogowanyUzytkownik.Equals("technolog", StringComparison.OrdinalIgnoreCase) ||
                       _zalogowanyUzytkownik.Equals("admin", StringComparison.OrdinalIgnoreCase);
            }
        }

        private void ZaładujTypyZlecenZBase()
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    string sql = "SELECT NazwaTypu FROM TypyZlecen ORDER BY Id ASC";

                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            var listaTypow = new System.Collections.Generic.List<object>();
                            while (dr.Read())
                            {
                                listaTypow.Add(new { NazwaTypu = dr["NazwaTypu"].ToString() });
                            }

                            CmbTypZlecenia.ItemsSource = listaTypow;
                        }
                    }
                }
                if (CmbTypZlecenia.Items.Count > 0) CmbTypZlecenia.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Błąd struktur słownika TypyZlecen: " + ex.Message);
            }
        }

    } // Zamyka klasę główną NoweZlecenieWindow

    // =========================================================================
    // KLASA MODELOWA (Musi być POZA klasą okna, ale WEWNĄTRZ namespace)
    // =========================================================================
    public class SlownikOpcja
    {
        public string Wartosc { get; set; } = string.Empty;
        public string Opis { get; set; } = string.Empty;
        public decimal Cena1 { get; set; }
        public decimal Cena2 { get; set; }

        public override string ToString() => Wartosc;
    }
    public class PozycjaZlecenia : INotifyPropertyChanged
    {
        private string _poz = string.Empty; private string _nrProd = string.Empty; private int _szt; private string _rodzaj = string.Empty; private string _oznaczenie = string.Empty;
        private int _szerokosc; private int _wysokosc; private string _systemOkna = string.Empty; private string _kolor = string.Empty;
        private string _wypelnienie = string.Empty; private string _typRamki = string.Empty; private string _kolorUszczelki = string.Empty;
        private string _klasaBezpieczenstwa = string.Empty; private string _wariantOkuc = string.Empty; private string _zawiasy = string.Empty; private string _typKlamki = string.Empty;
        private string _kolorKlamki = string.Empty; private string _listwaPodparapetowa = string.Empty;
        private string _wariantUkladuKoloru = string.Empty; private string _kolorOkleiny = string.Empty; private string _kolorBazy = string.Empty;
        private string _uwagiPozycji = string.Empty; private string _komentarzTechnologa = string.Empty;
        private string _szybaZewnetrzna = string.Empty; private string _szybaWewnetrzna = string.Empty;
        private string _kolorWewnetrzny = string.Empty; private string _kolorZawiasow = string.Empty;
        private string _wysokoscKlamki = string.Empty; private string _typSzprosu = string.Empty;
        private string _szpros = string.Empty; private string _kolorSzprosu = string.Empty;
        private string _parentPoz = string.Empty;
        private bool _czyElementZestawu;
        private bool _odstepstwoZaakceptowane; private bool _czyCenaReczna; private double _cenaRecznaNetto;
        public bool StatusZablokowany { get; set; } = false;

        public string Poz { get => _poz; set => SetProperty(ref _poz, value); }
        public string NrProd { get => _nrProd; set => SetProperty(ref _nrProd, value); }
        public int Szt { get => _szt; set => SetProperty(ref _szt, value); }
        public string Rodzaj { get => _rodzaj; set => SetProperty(ref _rodzaj, value); }
        public string Oznaczenie { get => _oznaczenie; set => SetProperty(ref _oznaczenie, value); }
        public int Szerokosc { get => _szerokosc; set => SetProperty(ref _szerokosc, value); }
        public int Wysokosc { get => _wysokosc; set => SetProperty(ref _wysokosc, value); }
        public string SystemOkna { get => _systemOkna; set => SetProperty(ref _systemOkna, value); }
        public string Wypelnienie { get => _wypelnienie; set => SetProperty(ref _wypelnienie, value); }
        public string TypRamki { get => _typRamki; set => SetProperty(ref _typRamki, value); }
        public string WariantUkladuKoloru { get => _wariantUkladuKoloru; set => SetProperty(ref _wariantUkladuKoloru, value); }
        public string KolorOkleiny { get => _kolorOkleiny; set => SetProperty(ref _kolorOkleiny, value); }
        public string KolorUszczelki { get => _kolorUszczelki; set => SetProperty(ref _kolorUszczelki, value); }
        public string KolorBazy { get => _kolorBazy; set => SetProperty(ref _kolorBazy, value); }
        public string KlasaBezpieczenstwa { get => _klasaBezpieczenstwa; set => SetProperty(ref _klasaBezpieczenstwa, value); }
        public string WariantOkuc { get => _wariantOkuc; set => SetProperty(ref _wariantOkuc, value); }
        public string Zawiasy { get => _zawiasy; set => SetProperty(ref _zawiasy, value); }
        public string TypKlamki { get => _typKlamki; set => SetProperty(ref _typKlamki, value); }
        public string KolorKlamki { get => _kolorKlamki; set => SetProperty(ref _kolorKlamki, value); }
        public string ListwaPodparapetowa { get => _listwaPodparapetowa; set => SetProperty(ref _listwaPodparapetowa, value); }
        public string UwagiPozycji { get => _uwagiPozycji; set => SetProperty(ref _uwagiPozycji, value); }
        public string KomentarzTechnologa { get => _komentarzTechnologa; set => SetProperty(ref _komentarzTechnologa, value); }
        public string SzybaZewnetrzna { get => _szybaZewnetrzna; set => SetProperty(ref _szybaZewnetrzna, value); }
        public string SzybaWewnetrzna { get => _szybaWewnetrzna; set => SetProperty(ref _szybaWewnetrzna, value); }
        public string KolorWewnetrzny { get => _kolorWewnetrzny; set => SetProperty(ref _kolorWewnetrzny, value); }
        public string KolorZawiasow { get => _kolorZawiasow; set => SetProperty(ref _kolorZawiasow, value); }
        public string WysokoscKlamki { get => _wysokoscKlamki; set => SetProperty(ref _wysokoscKlamki, value); }
        public string TypSzprosu { get => _typSzprosu; set => SetProperty(ref _typSzprosu, value); }
        public string Szpros { get => _szpros; set => SetProperty(ref _szpros, value); }
        public string KolorSzprosu { get => _kolorSzprosu; set => SetProperty(ref _kolorSzprosu, value); }
        public bool OdstepstwoZaakceptowane { get => _odstepstwoZaakceptowane; set => SetProperty(ref _odstepstwoZaakceptowane, value); }
        public bool CzyCenaReczna { get => _czyCenaReczna; set => SetProperty(ref _czyCenaReczna, value); }
        public double CenaRecznaNetto { get => _cenaRecznaNetto; set => SetProperty(ref _cenaRecznaNetto, value); }
        public string WymiaryOpis => $"{Szerokosc} x {Wysokosc} mm";
        public bool CzyZatwierdzone { get => OdstepstwoZaakceptowane; set => OdstepstwoZaakceptowane = value; }

        public string UwagiTechnologa { get => KomentarzTechnologa; set => KomentarzTechnologa = value; }
        public string ParentPoz { get => _parentPoz; set => SetProperty(ref _parentPoz, value); }
        public bool CzyElementZestawu { get => _czyElementZestawu; set => SetProperty(ref _czyElementZestawu, value); }
        public bool CzyNaglowekZestawu => Rodzaj == "JP" && NrProd.StartsWith("Z", StringComparison.OrdinalIgnoreCase);
        public string FormatUszczelka() => string.IsNullOrEmpty(KeepFormatUszczelka) ? "SZARY" : KeepFormatUszczelka;
        private string KeepFormatUszczelka => KolorUszczelki;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        protected bool SetProperty<T>(ref T st, T val, [CallerMemberName] string? p = null) { if (Equals(st, val)) return false; st = val; OnPropertyChanged(p); return true; }

        public PozycjaZlecenia Klonuj()
        {
            return new PozycjaZlecenia
            {
                Poz = Poz,
                NrProd = NrProd,
                Szt = Szt,
                Rodzaj = Rodzaj,
                Oznaczenie = Oznaczenie,
                Szerokosc = Szerokosc,
                Wysokosc = Wysokosc,
                SystemOkna = SystemOkna,
                Wypelnienie = Wypelnienie,
                TypRamki = TypRamki,
                WariantUkladuKoloru = WariantUkladuKoloru,
                KolorOkleiny = KolorOkleiny,
                KolorUszczelki = KolorUszczelki,
                KolorBazy = KolorBazy,
                KlasaBezpieczenstwa = KlasaBezpieczenstwa,
                WariantOkuc = WariantOkuc,
                Zawiasy = Zawiasy,
                TypKlamki = TypKlamki,
                KolorKlamki = KolorKlamki,
                ListwaPodparapetowa = ListwaPodparapetowa,
                StatusZablokowany = StatusZablokowany,
                OdstepstwoZaakceptowane = OdstepstwoZaakceptowane,
                KomentarzTechnologa = KomentarzTechnologa,
                UwagiPozycji = UwagiPozycji,
                CzyCenaReczna = CzyCenaReczna,
                CenaRecznaNetto = CenaRecznaNetto,
                SzybaZewnetrzna = SzybaZewnetrzna,
                SzybaWewnetrzna = SzybaWewnetrzna,
                KolorWewnetrzny = KolorWewnetrzny,
                KolorZawiasow = KolorZawiasow,
                WysokoscKlamki = WysokoscKlamki,
                TypSzprosu = TypSzprosu,
                Szpros = Szpros,
                KolorSzprosu = KolorSzprosu,

            };
        }
    }
}






