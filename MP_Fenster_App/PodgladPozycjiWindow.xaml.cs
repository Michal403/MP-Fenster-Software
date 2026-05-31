using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Data.SqlClient;

namespace MP_Fenster_App
{
    public partial class PodgladPozycjiWindow : Window
    {
        private readonly string _connString = "Server=localhost;Database=SeaSharkDB;User Id=sa;Password=zaq1@WSX;TrustServerCertificate=True;";
        private readonly List<PozycjaZlecenia> _pozycje;

        public PodgladPozycjiWindow(PozycjaZlecenia pozycja)
        {
            InitializeComponent();
            _pozycje = new List<PozycjaZlecenia> { pozycja };
            DgPozycje.ItemsSource = _pozycje;
            DgPozycje.SelectedItem = pozycja;
            TxtOpis.Text = BudujOpis(pozycja);
            Loaded += (_, _) => Rysuj(pozycja);
            DgPozycje.SelectionChanged += (_, _) => PokazZaznaczonaPozycje();
        }

        public PodgladPozycjiWindow(IEnumerable<PozycjaZlecenia> pozycje)
        {
            InitializeComponent();
            _pozycje = pozycje.ToList();
            DgPozycje.ItemsSource = _pozycje;
            DgPozycje.SelectedItem = _pozycje.FirstOrDefault();
            TxtOpis.Text = _pozycje.Count == 0 ? "Brak pozycji w zleceniu." : string.Join("\n\n", _pozycje.Select(BudujOpis));
            Loaded += (_, _) => { if (_pozycje.FirstOrDefault() != null) Rysuj(_pozycje.First()); };
            DgPozycje.SelectionChanged += (_, _) => PokazZaznaczonaPozycje();
        }

        private void PokazZaznaczonaPozycje()
        {
            if (DgPozycje.SelectedItem is PozycjaZlecenia p)
            {
                TxtOpis.Text = BudujOpis(p);
                Rysuj(p);
            }
        }

        private static string BudujOpis(PozycjaZlecenia p)
        {
            return $"Pozycja {p.Poz} | {p.NrProd}\n" +
                   $"Rodzaj: {p.Rodzaj}\n" +
                   $"System: {p.SystemOkna}\n" +
                   $"Wymiary: {p.Szerokosc} x {p.Wysokosc} mm\n" +
                   $"Szyba: {p.Wypelnienie}, ramka: {p.TypRamki}\n" +
                   $"Kolor: {p.WariantUkladuKoloru}, okleina: {p.KolorOkleiny}, baza: {p.KolorBazy}\n" +
                   $"Okucia: {p.WariantOkuc}, klasa: {p.KlasaBezpieczenstwa}, zawiasy: {p.Zawiasy}\n" +
                   $"Klamka: {p.TypKlamki}, kolor: {p.KolorKlamki}\n" +
                   $"Uwagi: {p.UwagiPozycji}";
        }

        private void Rysuj(PozycjaZlecenia p)
        {
            CanvasPodglad.Children.Clear();
            double canvasW = Math.Max(520, CanvasPodglad.ActualWidth);
            double canvasH = Math.Max(240, CanvasPodglad.ActualHeight);
            Rect obszarGrafiki = ObliczObszarGrafiki(p, canvasW, canvasH);

            bool pokazanoGrafike = PokazGrafike(p, obszarGrafiki);
            if (!pokazanoGrafike)
            {
                RysujProstyGabaryt(obszarGrafiki);
            }

            DodajStrzalkiWymiarowe(p, obszarGrafiki, canvasW, canvasH);
        }

        private Rect ObliczObszarGrafiki(PozycjaZlecenia p, double canvasW, double canvasH)
        {
            double maxW = Math.Max(180, Math.Min(canvasW - 220, 380));
            double maxH = Math.Max(150, Math.Min(canvasH - 86, 245));
            double ratio = 1.0;
            if (p.Szerokosc > 0 && p.Wysokosc > 0)
            {
                ratio = Math.Max(0.55, Math.Min(2.8, p.Szerokosc / (double)p.Wysokosc));
            }

            double w = maxW;
            double h = w / ratio;
            if (h > maxH)
            {
                h = maxH;
                w = h * ratio;
            }

            w = Math.Max(140, w);
            h = Math.Max(130, h);
            double left = Math.Max(42, (canvasW - w) / 2 - 18);
            double top = Math.Max(22, (canvasH - h) / 2 - 8);
            return new Rect(left, top, w, h);
        }

        private bool PokazGrafike(PozycjaZlecenia p, Rect obszar)
        {
            string? plik = ZnajdzGrafike(p);
            if (string.IsNullOrWhiteSpace(plik)) return false;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(plik, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                var obraz = new Image
                {
                    Source = bitmap,
                    Width = obszar.Width,
                    Height = obszar.Height,
                    Stretch = Stretch.Uniform
                };

                Canvas.SetLeft(obraz, obszar.Left);
                Canvas.SetTop(obraz, obszar.Top);
                CanvasPodglad.Children.Add(obraz);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void RysujProstyGabaryt(Rect obszar)
        {
            var rect = new Rectangle
            {
                Width = obszar.Width,
                Height = obszar.Height,
                Stroke = Brushes.SteelBlue,
                StrokeThickness = 4,
                Fill = Brushes.AliceBlue
            };

            Canvas.SetLeft(rect, obszar.Left);
            Canvas.SetTop(rect, obszar.Top);
            CanvasPodglad.Children.Add(rect);
        }

        private void DodajStrzalkiWymiarowe(PozycjaZlecenia p, Rect obszar, double canvasW, double canvasH)
        {
            string szerokosc = p.Szerokosc > 0 ? $"{p.Szerokosc} mm" : "0 mm";
            string wysokosc = p.Wysokosc > 0 ? $"{p.Wysokosc} mm" : "0 mm";

            double y = Math.Min(canvasH - 28, obszar.Bottom + 24);
            double x1 = obszar.Left;
            double x2 = obszar.Right;
            double x = Math.Min(canvasW - 74, obszar.Right + 38);
            double y1 = obszar.Top;
            double y2 = obszar.Bottom;

            DodajLinieZeStrzalkami(x1, y, x2, y);
            DodajLinieZeStrzalkami(x, y1, x, y2);
            DodajEtykiete(szerokosc, (x1 + x2) / 2, y - 18, true);
            DodajEtykiete(wysokosc, x + 4, (y1 + y2) / 2, false);
        }

        private void DodajLinieZeStrzalkami(double x1, double y1, double x2, double y2)
        {
            var pen = Brushes.Gray;
            var line = new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = pen, StrokeThickness = 1.2 };
            CanvasPodglad.Children.Add(line);

            double angle = Math.Atan2(y2 - y1, x2 - x1);
            DodajGrot(x1, y1, angle + Math.PI);
            DodajGrot(x2, y2, angle);
        }

        private void DodajGrot(double x, double y, double angle)
        {
            const double len = 9;
            const double spread = Math.PI / 7;
            var p1 = new Line
            {
                X1 = x,
                Y1 = y,
                X2 = x - len * Math.Cos(angle - spread),
                Y2 = y - len * Math.Sin(angle - spread),
                Stroke = Brushes.Gray,
                StrokeThickness = 1.2
            };
            var p2 = new Line
            {
                X1 = x,
                Y1 = y,
                X2 = x - len * Math.Cos(angle + spread),
                Y2 = y - len * Math.Sin(angle + spread),
                Stroke = Brushes.Gray,
                StrokeThickness = 1.2
            };
            CanvasPodglad.Children.Add(p1);
            CanvasPodglad.Children.Add(p2);
        }

        private void DodajEtykiete(string text, double x, double y, bool center)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(240, 255, 255, 255)),
                BorderBrush = Brushes.Silver,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(7, 2, 7, 2),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.DimGray
                }
            };
            border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(border, center ? x - border.DesiredSize.Width / 2 : x);
            Canvas.SetTop(border, y - border.DesiredSize.Height / 2);
            CanvasPodglad.Children.Add(border);
        }

        private string? ZnajdzGrafike(PozycjaZlecenia p)
        {
            string? sciezka = PobierzSciezkeGrafiki(p);
            if (string.IsNullOrWhiteSpace(sciezka)) return null;
            string rel = sciezka.Replace('/', System.IO.Path.DirectorySeparatorChar);
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string desktopProject = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "MP_Fenster_Software", "MP_Fenster_App");

            string[] kandydaci =
            {
                System.IO.Path.Combine(baseDir, rel),
                System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", rel)),
                System.IO.Path.Combine(desktopProject, rel)
            };

            return kandydaci.FirstOrDefault(File.Exists);
        }

        private string? PobierzSciezkeGrafiki(PozycjaZlecenia p)
        {
            try
            {
                using var cn = new SqlConnection(_connString);
                cn.Open();
                using var cmd = new SqlCommand(@"
                    SELECT TOP 1 SciezkaPliku
                    FROM GrafikiKonstrukcji
                    WHERE KodProduktu = @kod
                      AND CzyAktywna = 1
                      AND SciezkaPliku IS NOT NULL
                    ORDER BY
                        CASE
                            WHEN KolorProfilu = @kolor THEN 0
                            WHEN KolorProfilu = 'DEFAULT' THEN 1
                            ELSE 2
                        END", cn);
                cmd.Parameters.AddWithValue("@kod", p.NrProd);
                cmd.Parameters.AddWithValue("@kolor", string.IsNullOrWhiteSpace(p.KolorOkleiny) ? "DEFAULT" : p.KolorOkleiny);
                object? result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? null : result.ToString();
            }
            catch
            {
                return null;
            }
        }

        private void BtnZamknij_Click(object sender, RoutedEventArgs e) => Close();
    }
}
