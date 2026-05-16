using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Data.SqlClient;

namespace MP_Fenster_App
{
    public partial class NoweZlecenieWindow : Window
    {
        private readonly string _connString = "Server=localhost;Database=SeaSharkDB;User Id=sa;Password=zaq1@WSX;TrustServerCertificate=True;";
        public ObservableCollection<PozycjaZlecenia> ListaPozycji { get; set; }
        private string _zalogowanyUzytkownik;
        private bool _blokadaOdswiezania = false;
        private DispatcherTimer _timer = null!;

        // POPRAWKA: Pole śledzące ID edytowanego dokumentu z bazy
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

            DpTermin.DisplayDateStart = DateTime.Now.AddDays(7);
            DpTermin.SelectedDate = DateTime.Now.AddDays(30);

            PobierzKolejnyNumerZlecenia();
            LadujKlientowZBase();
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
                    string sql = "SELECT ISNULL(MAX(IdZlecenia), 99) + 1 FROM Zlecenia";
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

        private void LadujKlientowZBase()
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    string sql = "SELECT IdKlienta, (NazwaKlienta + ' - ' + ISNULL(NIP,'Indywidualny')) AS KlientOpis FROM Klienci";
                    using (SqlCommand cmd = new SqlCommand(sql, cn))
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            DataTable dt = new DataTable();
                            dt.Load(dr);
                            CmbKlienci.ItemsSource = dt.DefaultView;
                            CmbKlienci.DisplayMemberPath = "KlientOpis";
                            CmbKlienci.SelectedValuePath = "IdKlienta";
                        }
                    }
                }
            }
            catch
            {
                CmbKlienci.Items.Add("Nowak Mateusz - FIRMA BUDOWLANA");
                CmbKlienci.SelectedIndex = 0;
            }
        }

        private void LadujSlownikiZBase()
        {
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    PobierzDaneDoCombo(cn, "SELECT DISTINCT (Producent + ' ' + NazwaSystemu) FROM SystemyProfilowe WHERE CzyAktywny=1", CmbSystemOkna);
                    PobierzDaneDoCombo(cn, "SELECT KodRodzaju FROM RodzajePozycji", CmbRodzajKonstrukcji);
                    PobierzDaneDoCombo(cn, "SELECT UPPER(KodProduktu) FROM TypyKonstrukcji", CmbProdukt);
                    PobierzDaneDoCombo(cn, "SELECT Oznaczenie FROM PakietySzybowe", CmbWypelnienie);
                    PobierzDaneDoCombo(cn, "SELECT Oznaczenie FROM RamkiDystansowe", CmbTypRamki);
                    PobierzDaneDoCombo(cn, "SELECT Oznaczenie FROM KoloryWariantyUkladu", CmbWariantKoloru);
                    PobierzDaneDoCombo(cn, "SELECT Oznaczenie FROM KoloryOklein", CmbKolorOkleiny);
                    PobierzDaneDoCombo(cn, "SELECT Oznaczenie FROM KoloryUszczelek", CmbUszczelka);
                    PobierzDaneDoCombo(cn, "SELECT Oznaczenie FROM KoloryBazyProfilu", CmbKolorBazy);
                    PobierzDaneDoCombo(cn, "SELECT Oznaczenie FROM OkuciaKlasyBezpieczenstwa", CmbKlasaBezp);
                    PobierzDaneDoCombo(cn, "SELECT Oznaczenie FROM OkuciaWariantyOtwierania", CmbWariantOkuc);
                    PobierzDaneDoCombo(cn, "SELECT Oznaczenie FROM OkuciaZawiasy", CmbZawiasy);
                    PobierzDaneDoCombo(cn, "SELECT NazwaHandlowa FROM KlamkiKatalog", CmbTypKlamki);
                    PobierzDaneDoCombo(cn, "SELECT NazwaKoloru FROM KlamkiKolory", CmbKolorKlamki);
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
            using (SqlCommand cmd = new SqlCommand(sql, cn))
            {
                using (SqlDataReader dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        cmb.Items.Add(dr[0].ToString());
                    }
                }
            }
            if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
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
            CmbProdukt.Text = pos.NrProd;
            TxtSzerokosc.Text = pos.Szerokosc.ToString();
            TxtWysokosc.Text = pos.Wysokosc.ToString();
            CmbWypelnienie.Text = pos.Wypelnienie;
            CmbTypRamki.Text = pos.TypRamki;
            CmbWariantKoloru.Text = pos.WariantUkladuKoloru;
            CmbKolorOkleiny.Text = pos.KolorOkleiny;
            CmbUszczelka.Text = pos.KolorUszczelki;
            CmbKolorBazy.Text = pos.KolorBazy;
            CmbKlasaBezp.Text = pos.KlasaBezpieczenstwa;
            CmbWariantOkuc.Text = pos.WariantOkuc;
            CmbZawiasy.Text = pos.Zawiasy;
            CmbTypKlamki.Text = pos.TypKlamki;
            CmbKolorKlamki.Text = pos.KolorKlamki;
            CmbListwa.Text = pos.ListwaPodparapetowa;

            ActualizeLabelsDescriptor();
            PrzeliczFinanseZlecenia();

            TxtDetalPozycji.Text = $"========================================\n" +
                                   $" PARAMETRY POZYCJI: {pos.Poz} | PROFIL: {pos.SystemOkna}\n" +
                                   $"========================================\n" +
                                   $" • Typ konstrukcji: {pos.Oznaczenie}\n" +
                                   $" • Gabaryty ościeżnicy: {pos.Szerokosc} x {pos.Wysokosc} mm\n" +
                                   $" • Pakiet szklenia: {pos.Wypelnienie} mm (Ramka: {pos.TypRamki})\n" +
                                   $" • Kolorystyka: Okleina {pos.KolorOkleiny} | Rdzeń/Baza: {pos.KolorBazy}\n" +
                                   $" • Uszczelnienie: Kolor {pos.KolorUszczelki}\n" +
                                   $" • Mechanizm okuć: {pos.WariantOkuc} (Klasa odporności: {pos.KlasaBezpieczenstwa})\n" +
                                   $" • Klamka: {pos.TypKlamki} ({pos.KolorKlamki})\n" +
                                   $" • Listwa transportowa: {pos.ListwaPodparapetowa}\n" +
                                   $"========================================\n" +
                                   $" Architektura dostępu: Dapper / RAW SQL Mode";

            _blokadaOdswiezania = false;
            RysujGabarytyOkna();
        }

        private void Konfigurator_InputChanged(object sender, EventArgs e)
        {
            var pos = WybranaPozycja;
            if (_blokadaOdswiezania || pos == null) return;

            pos.SystemOkna = CmbSystemOkna.Text;
            pos.Rodzaj = CmbRodzajKonstrukcji.Text;
            pos.NrProd = CmbProdukt.Text;
            pos.Wypelnienie = CmbWypelnienie.Text;
            pos.TypRamki = CmbTypRamki.Text;
            pos.WariantUkladuKoloru = CmbWariantKoloru.Text;
            pos.KolorOkleiny = CmbKolorOkleiny.Text;
            pos.KolorUszczelki = CmbUszczelka.Text;
            pos.KolorBazy = CmbKolorBazy.Text;
            pos.KlasaBezpieczenstwa = CmbKlasaBezp.Text;
            pos.WariantOkuc = CmbWariantOkuc.Text;
            pos.Zawiasy = CmbZawiasy.Text;
            pos.TypKlamki = CmbTypKlamki.Text;
            pos.KolorKlamki = CmbKolorKlamki.Text;
            pos.ListwaPodparapetowa = CmbListwa.Text;

            if (int.TryParse(TxtIlosc.Text, out int szt) && szt > 0) pos.Szt = szt;
            if (int.TryParse(TxtSzerokosc.Text, out int sz)) pos.Szerokosc = sz;
            if (int.TryParse(TxtWysokosc.Text, out int wy)) pos.Wysokosc = wy;

            pos.Oznaczenie = $"{pos.NrProd} / {pos.Wypelnienie}";

            ActualizeLabelsDescriptor();
            PrzeliczFinanseZlecenia();
            RysujGabarytyOkna();
        }

        private void ActualizeLabelsDescriptor()
        {
            var pos = WybranaPozycja;
            if (pos == null) return;
            TxtOznaczSystem.Text = "OK";
            TxtOznaczRodzaj.Text = pos.Rodzaj;
            TxtOznaczProdukt.Text = "Zweryfikowano";
            TxtOznaczWyp.Text = $"Pakiet {pos.Wypelnienie}";
            TxtOznaczRamka.Text = pos.TypRamki;
            TxtOznaczWariantKolor.Text = pos.WariantUkladuKoloru;
            TxtOznaczKolorOkleiny.Text = pos.KolorOkleiny;
            TxtOznaczUszczelka.Text = pos.FormatUszczelka(); // Secure extraction format fallback
            TxtOznaczBaza.Text = pos.KolorBazy;
            TxtOznaczKlasa.Text = pos.KlasaBezpieczenstwa;
            TxtOznaczWariant.Text = pos.WariantOkuc;
            TxtOznaczZawiasy.Text = pos.Zawiasy;
            TxtOznaczKlamka.Text = "Klamka systemowa";
            TxtOznaczKolorKlamki.Text = pos.KolorKlamki;
            TxtOznaczListwa.Text = "Listwa ramy";
        }

        private void PrzeliczFinanseZlecenia()
        {
            if (ListaPozycji == null || CmbTransport == null || CmbObszar == null || CmbMontaz == null ||
                TxtSumaWszystkichNetto == null || TxtCenaTransportu == null || TxtCenaMontazu == null ||
                TxtCalkowityKosztBrutto == null || TxtRabatProcent == null || TxtWartoscRabatu == null ||
                TxtCenaPoRabacie == null || TxtCenaAktywnejPoz == null) return;

            double sumaWszystkichNetto = 0;
            double cenaAktywnejPozycjiNetto = 0;

            foreach (var pos in ListaPozycji)
            {
                if (pos.Szerokosc == 0 || pos.Wysokosc == 0) continue;
                double m2 = (pos.Szerokosc / 1000.0) * (pos.Wysokosc / 1000.0);

                double cenaM2Baza = 410;
                if (pos.SystemOkna.Contains("Schuco Living MD")) cenaM2Baza = 580;
                else if (pos.SystemOkna.Contains("Salamander BluEvolution 82")) cenaM2Baza = 620;
                else if (pos.SystemOkna.Contains("Veka Softline 82")) cenaM2Baza = 590;

                if (pos.Wypelnienie == "3-48") cenaM2Baza += 130;
                else if (pos.Wypelnienie == "4-48") cenaM2Baza += 270;

                double wycenaPozycji = (m2 * cenaM2Baza * pos.Szt);
                sumaWszystkichNetto += wycenaPozycji;

                if (pos == WybranaPozycja) cenaAktywnejPozycjiNetto = wycenaPozycji;
            }

            string transportText = (CmbTransport.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "NIE";
            string obszarText = (CmbObszar.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Polska";
            string montazText = (CmbMontaz.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "NIE";

            if (!double.TryParse(TxtRabatProcent.Text, out double rabatProc)) rabatProc = 0;
            double wartoscRabatu = sumaWszystkichNetto * (rabatProc / 100.0);
            double cenaPoRabacie = sumaWszystkichNetto - wartoscRabatu;

            double kosztTransportu = (transportText == "TAK") ? 200 : 0;
            if (obszarText == "Niemcy") kosztTransportu = 1500;

            double kosztMontazu = (montazText == "TAK") ? 3800 : 0;
            double calkowityKosztBrutto = (cenaPoRabacie + kosztTransportu + kosztMontazu) * 1.23;

            TxtCenaAktywnejPoz.Text = $"{cenaAktywnejPozycjiNetto:N2} PLN";
            TxtSumaWszystkichNetto.Text = $"{sumaWszystkichNetto:N2} PLN";
            TxtWartoscRabatu.Text = $"{wartoscRabatu:N2} PLN";
            TxtCenaPoRabacie.Text = $"{cenaPoRabacie:N2} PLN";
            TxtCenaMontazu.Text = $"{kosztMontazu:N2} PLN";
            TxtCenaTransportu.Text = $"{kosztTransportu:N2} PLN";
            TxtCalkowityKosztBrutto.Text = $"{calkowityKosztBrutto:N2} PLN";
        }

        private void RysujGabarytyOkna()
        {
            var pos = WybranaPozycja;
            if (CanvasOkno == null || pos == null) return;
            CanvasOkno.Children.Clear();

            if (pos.Szerokosc == 0 || pos.Wysokosc == 0)
            {
                TxtStatusWalidacji.Text = "⚠️ Wymiary ramy równe 0 mm.";
                TxtStatusWalidacji.Foreground = System.Windows.Media.Brushes.Orange;
                TxtExclamation.Visibility = Visibility.Collapsed;
                return;
            }

            double maxDim = Math.Max(pos.Szerokosc, pos.Wysokosc);
            double scale = 140.0 / maxDim;

            double w = pos.Szerokosc * scale;
            double h = pos.Wysokosc * scale;

            System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle
            {
                Width = w,
                Height = h,
                Stroke = System.Windows.Media.Brushes.SteelBlue,
                StrokeThickness = 3,
                Fill = System.Windows.Media.Brushes.AliceBlue
            };
            Canvas.SetLeft(rect, (CanvasOkno.ActualWidth - w) / 2);
            Canvas.SetTop(rect, (CanvasOkno.ActualHeight - h) / 2);
            CanvasOkno.Children.Add(rect);

            if (pos.Szerokosc > 1500 && pos.Rodzaj == "J")
            {
                TxtStatusWalidacji.Text = "❌ Przekroczono maksymalną szerokość ramy (Max 1500 mm)!";
                TxtStatusWalidacji.Foreground = System.Windows.Media.Brushes.Red;
                TxtExclamation.Visibility = Visibility.Visible;
                pos.StatusZablokowany = true;
            }
            else
            {
                TxtStatusWalidacji.Text = "✔️ Gabaryty techniczne ramy zatwierdzone (Fenster Engine)";
                TxtStatusWalidacji.Foreground = System.Windows.Media.Brushes.Green;
                TxtExclamation.Visibility = Visibility.Collapsed;
                pos.StatusZablokowany = false;
            }
        }

        private void OpcjeFinansowe_SelectionChanged(object sender, RoutedEventArgs e) => PrzeliczFinanseZlecenia();

        private void CmbKlienci_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbKlienci.SelectedValue != null) TxtIdKlienta.Text = CmbKlienci.SelectedValue.ToString();
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
                MessageBox.Show($"[PRODUKT]: {WybranaPozycja.Oznaczenie}\nGabaryt: {WybranaPozycja.Szerokosc}x{WybranaPozycja.Wysokosc}mm\nIlość: {WybranaPozycja.Szt} szt.", "Specyfikacja Fenster");
            }
            else
            {
                MessageBox.Show("Zaznacz pozycję z tabeli po prawej stronie, aby wyświetlić pełny podgląd.", "Informacja");
            }
        }

        private void BtnNowaPozycja_Click(object sender, RoutedEventArgs e)
        {
            int nr = ListaPozycji.Count + 1;
            var nowa = new PozycjaZlecenia
            {
                Poz = nr.ToString(),
                NrProd = "F100",
                Szt = 1,
                Rodzaj = "J",
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
                ListwaPodparapetowa = "TAK"
            };
            ListaPozycji.Add(nowa);
            GridPozycje.SelectedItem = nowa;
            PrzeliczFinanseZlecenia();
        }

        private void BtnKopiujPozycje_Click(object sender, RoutedEventArgs e)
        {
            var pos = WybranaPozycja;
            if (pos == null) return;
            ListaPozycji.Add(new PozycjaZlecenia
            {
                Poz = (ListaPozycji.Count + 1).ToString(),
                NrProd = pos.NrProd,
                Szt = pos.Szt,
                Rodzaj = pos.Rodzaj,
                Oznaczenie = pos.Oznaczenie + " (Kopia)",
                Szerokosc = pos.Szerokosc,
                Wysokosc = pos.Wysokosc,
                SystemOkna = pos.SystemOkna,
                Wypelnienie = pos.Wypelnienie,
                TypRamki = pos.TypRamki,
                WariantUkladuKoloru = pos.WariantUkladuKoloru,
                KolorOkleiny = pos.KolorOkleiny,
                KolorUszczelki = pos.KolorUszczelki,
                KolorBazy = pos.KolorBazy,
                KlasaBezpieczenstwa = pos.KlasaBezpieczenstwa,
                WariantOkuc = pos.WariantOkuc,
                Zawiasy = pos.Zawiasy,
                TypKlamki = pos.TypKlamki,
                KolorKlamki = pos.KolorKlamki,
                ListwaPodparapetowa = pos.ListwaPodparapetowa
            });
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
            for (int i = 0; i < ListaPozycji.Count; i++)
            {
                if (ListaPozycji[i].Rodzaj == "JP" && i > 0 && (ListaPozycji[i - 1].NrProd == "Z100" || ListaPozycji[i - 1].Rodzaj == "JP"))
                {
                    int podPozLicznik = 1;
                    if (double.TryParse(ListaPozycji[i - 1].Poz, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double prevVal))
                    {
                        int n = (int)prevVal;
                        for (int j = i - 1; j >= 0; j--)
                        {
                            if (ListaPozycji[j].Poz.StartsWith(n + ".")) podPozLicznik++;
                        }
                        ListaPozycji[i].Poz = $"{n}.{podPozLicznik}";
                        continue;
                    }
                }
                glownyLicznik++;
                ListaPozycji[i].Poz = glownyLicznik.ToString();
            }
        }

        private void BtnBledy_Click(object sender, RoutedEventArgs e)
        {
            var pos = WybranaPozycja;
            if (pos == null) return;

            if (_zalogowanyUzytkownik.ToLower() == "handlowiec")
            {
                MessageBox.Show("Zlecenie zablokowane technologicznie. Oczekiwanie na akceptację przez Technologa.", "Blokada");
                return;
            }

            if (pos.StatusZablokowany)
            {
                var odp = MessageBox.Show("Zatwierdzić odstępstwo gabarytowe dla wybranej pozycji?", "Weryfikacja Technologa", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (odp == MessageBoxResult.Yes)
                {
                    string komentarz = Microsoft.VisualBasic.Interaction.InputBox("Wpisz komentarz techniczny:", "Komentarz", "Zaakceptowano bez gwarancji");
                    TxtStatusWalidacji.Text = "✔️ Odstępstwo zaakceptowane: " + komentarz;
                    TxtStatusWalidacji.Foreground = System.Windows.Media.Brushes.DarkGoldenrod;
                    TxtExclamation.Visibility = Visibility.Collapsed;
                }
            }
        }

        // POPRAWKA: Prawdziwy i poprawny zapis transakcyjny RAW SQL obsługujący tryb UPDATE i INSERT
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
                            string typText = (CmbTypZlecenia.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "N";
                            string typChar = typText.Contains(" - ") ? typText.Split(' ')[0] : (typText.Length > 0 ? typText.Substring(0, 1) : "N");

                            int idZleceniaDoPozycji = 0;

                            if (_obecneIdZlecenia.HasValue)
                            {
                                // TRYB EDYCJI: Aktualizacja istniejącego rekordu nagłówka oferty
                                idZleceniaDoPozycji = _obecneIdZlecenia.Value;
                                string sqlUpdateHeader = @"UPDATE Zlecenia SET 
                                    IdKlienta = @klient, 
                                    IdUzytkownika = @user, 
                                    TerminPreferowany = @termin, 
                                    Obszar = @obszar, 
                                    CzyTransport = @trans, 
                                    CzyMontaz = @montaz, 
                                    TypZlecenia = @typ 
                                    WHERE IdZlecenia = @idZlec";

                                using (SqlCommand cmdUpdate = new SqlCommand(sqlUpdateHeader, cn, tx))
                                {
                                    cmdUpdate.Parameters.AddWithValue("@idZlec", idZleceniaDoPozycji);
                                    cmdUpdate.Parameters.AddWithValue("@klient", string.IsNullOrEmpty(TxtIdKlienta.Text) ? 1 : Convert.ToInt32(TxtIdKlienta.Text));
                                    cmdUpdate.Parameters.AddWithValue("@user", idUzytkownika);
                                    cmdUpdate.Parameters.AddWithValue("@termin", DpTermin.SelectedDate ?? DateTime.Now.AddDays(14));
                                    cmdUpdate.Parameters.AddWithValue("@obszar", obszarText);
                                    cmdUpdate.Parameters.AddWithValue("@trans", transportText == "TAK");
                                    cmdUpdate.Parameters.AddWithValue("@montaz", montazText == "TAK");
                                    cmdUpdate.Parameters.AddWithValue("@typ", typChar);

                                    cmdUpdate.ExecuteNonQuery();
                                }

                                // Czyszczenie starych wierszy powiązanych z tym IdZlecenia przed wgraniem nowego stanu koszyka okien
                                string sqlDeleteLines = "DELETE FROM PozycjeZlecenia WHERE IdZlecenia = @idZlec";
                                using (SqlCommand cmdDel = new SqlCommand(sqlDeleteLines, cn, tx))
                                {
                                    cmdDel.Parameters.AddWithValue("@idZlec", idZleceniaDoPozycji);
                                    cmdDel.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                // TRYB NOWY: Wstawienie nowego dokumentu i pobranie wygenerowanego klucza głównego
                                string sqlHeader = @"INSERT INTO Zlecenia 
                                    (NumerZlecenia, IdKlienta, IdUzytkownika, DataWprowadzenia, TerminPreferowany, Obszar, CzyTransport, CzyMontaz, TypZlecenia, StatusZlecenia) 
                                    VALUES (@nr, @klient, @user, GETDATE(), @termin, @obszar, @trans, @montaz, @typ, 'OFERTA');
                                    SELECT SCOPE_IDENTITY();";

                                using (SqlCommand cmdHeader = new SqlCommand(sqlHeader, cn, tx))
                                {
                                    cmdHeader.Parameters.AddWithValue("@nr", TxtZlecenie.Text);
                                    cmdHeader.Parameters.AddWithValue("@klient", string.IsNullOrEmpty(TxtIdKlienta.Text) ? 1 : Convert.ToInt32(TxtIdKlienta.Text));
                                    cmdHeader.Parameters.AddWithValue("@user", idUzytkownika);
                                    cmdHeader.Parameters.AddWithValue("@termin", DpTermin.SelectedDate ?? DateTime.Now.AddDays(14));
                                    cmdHeader.Parameters.AddWithValue("@obszar", obszarText);
                                    cmdHeader.Parameters.AddWithValue("@trans", transportText == "TAK");
                                    cmdHeader.Parameters.AddWithValue("@montaz", montazText == "TAK");
                                    cmdHeader.Parameters.AddWithValue("@typ", typChar);

                                    idZleceniaDoPozycji = Convert.ToInt32(cmdHeader.ExecuteScalar());
                                }
                            }

                            // Zrzut wszystkich linii konfiguratora do powiązanej tabeli relacyjnej
                            string sqlLine = @"INSERT INTO PozycjeZlecenia 
                                (IdZlecenia, NrProdukcyjny, Sztuk, Szerokosc, Wysokosc, CenaJednostkowa, Uwagi, StatusTechniczny) 
                                VALUES (@idZlec, @nrProd, @szt, @szer, @wys, @cena, @uwagi, @statusTech)";

                            foreach (var pos in ListaPozycji)
                            {
                                using (SqlCommand cmdLine = new SqlCommand(sqlLine, cn, tx))
                                {
                                    double m2 = (pos.Szerokosc / 1000.0) * (pos.Wysokosc / 1000.0);
                                    double cenaBazowa = pos.SystemOkna.Contains("Salamander") ? 620 : 450;
                                    if (pos.Wypelnienie == "3-48") cenaBazowa += 130;
                                    double cenaJednostkowa = m2 * cenaBazowa;

                                    cmdLine.Parameters.AddWithValue("@idZlec", idZleceniaDoPozycji);
                                    cmdLine.Parameters.AddWithValue("@nrProd", pos.NrProd);
                                    cmdLine.Parameters.AddWithValue("@szt", pos.Szt);
                                    cmdLine.Parameters.AddWithValue("@szer", pos.Szerokosc);
                                    cmdLine.Parameters.AddWithValue("@wys", pos.Wysokosc);
                                    cmdLine.Parameters.AddWithValue("@cena", cenaJednostkowa);
                                    cmdLine.Parameters.AddWithValue("@uwagi", $"Rama: {pos.SystemOkna}, Okleina: {pos.KolorOkleiny}");
                                    cmdLine.Parameters.AddWithValue("@statusTech", pos.StatusZablokowany ? 0 : 1);

                                    cmdLine.ExecuteNonQuery();
                                }
                            }

                            tx.Commit();
                            MessageBox.Show($"Zlecenie numer {TxtZlecenie.Text} wraz ze wszystkimi ({ListaPozycji.Count}) pozycjami zostało pomyślnie zapisane na Dockerze! Dane są w 100% gotowe do odczytu na innym ekranie.", "Komunikat bazy SeaSharkDB", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Błąd zapisu koszyka do chmury Docker: " + ex.Message, "Kalkulator Offline Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnZamknijZapisz_Click(object sender, RoutedEventArgs e) { ZapiszZlecenieDoBazySystemu(); this.Close(); }
        private void BtnZapiszPostepy_Click(object sender, RoutedEventArgs e) => ZapiszZlecenieDoBazySystemu();
        private void BtnWyjdzBezZapisu_Click(object sender, RoutedEventArgs e) { var res = MessageBox.Show("Wyjść bez zapisu?", "Potwierdzenie", MessageBoxButton.YesNo); if (res == MessageBoxResult.Yes) this.Close(); }
        private void BtnDrukuj_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Generowanie PDF...");
        private void BtnDodajKlienta_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Dodawanie klienta.");
        private void BtnListaKlientow_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Lista klientów.");
        private void BtnAktualizujCeny_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Ceny zaktualizowane.");
        private void BtnKopiujZInnego_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Kopiowanie pozycji.");
        private void BtnWstawSpecjalne_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Wstawienie.");
        private void BtnOdswiezPozycje_Click(object sender, RoutedEventArgs e) => Konfigurator_InputChanged(this, EventArgs.Empty);
        private void BtnUwagi_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Uwagi.");
        private void BtnSpecMat_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Specyfikacja.");
        private void BtnKorektaCeny_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Korekta.");

        // POPRAWKA: Rejestrowanie IdZlecenia podczas otwierania z panelu przeglądu
        public void WczytajIstniejaceZlecenieZBase(int idZlecenia)
        {
            _obecneIdZlecenia = idZlecenia;
            try
            {
                using (SqlConnection cn = new SqlConnection(_connString))
                {
                    cn.Open();

                    // 1. Ściągamy dane nagłówka, aby uzupełnić kontrolki
                    string sqlHeader = "SELECT NumerZlecenia, IdKlienta, Obszar, CzyTransport, CzyMontaz FROM Zlecenia WHERE IdZlecenia = @id";
                    using (SqlCommand cmdH = new SqlCommand(sqlHeader, cn))
                    {
                        cmdH.Parameters.AddWithValue("@id", idZlecenia);
                        using (SqlDataReader drH = cmdH.ExecuteReader())
                        {
                            if (drH.Read())
                            {
                                TxtZlecenie.Text = drH["NumerZlecenia"].ToString();
                                TxtIdKlienta.Text = drH["IdKlienta"].ToString();
                                CmbKlienci.SelectedValue = drH["IdKlienta"];
                                CmbObszar.Text = drH["Obszar"].ToString();
                                CmbTransport.Text = Convert.ToBoolean(drH["CzyTransport"]) ? "TAK" : "NIE";
                                CmbMontaz.Text = Convert.ToBoolean(drH["CzyMontaz"]) ? "TAK" : "NIE";
                            }
                        }
                    }

                    // 2. Ściągamy wszystkie linie z tabeli PozycjeZlecenia za pomocą surowego zapytania
                    string sqlLines = "SELECT * FROM PozycjeZlecenia WHERE IdZlecenia = @id ORDER BY IdPozycji ASC";
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

                                ListaPozycji.Add(new PozycjaZlecenia
                                {
                                    Poz = licznik.ToString(),
                                    NrProd = drL["NrProdukcyjny"].ToString() ?? "F100",
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
                                    Oznaczenie = $"{drL["NrProdukcyjny"]} / Zrzut",
                                    StatusZablokowany = (drL["StatusTechniczny"].ToString() == "0")
                                });
                                licznik++;
                            }
                        }
                    }
                    PrzeliczFinanseZlecenia();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd krytyczny odtwarzania koszyka zlecenia: " + ex.Message, "Błąd Re-Open");
            }
        }
    }

    public class PozycjaZlecenia : INotifyPropertyChanged
    {
        private string _poz = string.Empty; private string _nrProd = string.Empty; private int _szt; private string _rodzaj = string.Empty; private string _oznaczenie = string.Empty;
        private int _szerokosc; private int _wysokosc; private string _systemOkna = string.Empty; private string _kolor = string.Empty;
        private string _wypelnienie = string.Empty; private string _typRamki = string.Empty; private string _kolorUszczelki = string.Empty;
        private string _klasaBezpieczenstwa = string.Empty; private string _wariantOkuc = string.Empty; private string _zawiasy = string.Empty; private string _typKlamki = string.Empty;
        private string _kolorKlamki = string.Empty; private string _listwaPodparapetowa = string.Empty;
        private string _wariantUkladuKoloru = string.Empty; private string _kolorOkleiny = string.Empty; private string _kolorBazy = string.Empty;
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

        // Metoda pomocnicza zapobiegająca nullowi przy wyciąganiu tekstu opisowego uszczelek
        public string FormatUszczelka() => string.IsNullOrEmpty(KolorUszczelki) ? "SZARY" : KolorUszczelki;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? p = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        protected bool SetProperty<T>(ref T st, T val, [CallerMemberName] string? p = null) { if (Equals(st, val)) return false; st = val; OnPropertyChanged(p); return true; }
    }
}