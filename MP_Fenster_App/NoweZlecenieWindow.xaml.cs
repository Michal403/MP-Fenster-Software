using System.Collections.Generic;
using System.Windows;

namespace MP_Fenster_App
{
    public partial class NoweZlecenieWindow : Window
    {
        private string _user;

        public NoweZlecenieWindow(string user)
        {
            InitializeComponent();
            _user = user;
            StatusUser.Content = "Operator: " + _user.ToUpper();

            ZaladujDaneTestowe();
        }

        private void ZaladujDaneTestowe()
        {
            // Atrybuty - to będzie pociągnięte z tabeli 'Konfiguracja' w Azure
            var atrybuty = new List<AtrybutModel>
            {
                new AtrybutModel { Nazwa = "System okna", Wartosc = "Schuco" },
                new AtrybutModel { Nazwa = "Szerokość", Wartosc = "1000" },
                new AtrybutModel { Nazwa = "Wysokość", Wartosc = "1000" },
                new AtrybutModel { Nazwa = "Kolor", Wartosc = "Biały" },
                new AtrybutModel { Nazwa = "Okucia", Wartosc = "Standard" }
            };
            GridAtrybuty.ItemsSource = atrybuty;

            // Pozycje - to będzie tabela 'PozycjeZlecenia'
            var pozycje = new List<PozycjaModel>
            {
                new PozycjaModel { Id = 1, Kod = "F100", Ilosc = 3 },
                new PozycjaModel { Id = 2, Kod = "F201", Ilosc = 1 }
            };
            GridPozycje.ItemsSource = pozycje;
        }

        private void BtnZapisz_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Zapisywanie zlecenia do bazy Azure...");
        }
    }

    public class AtrybutModel
    {
        public string Nazwa { get; set; } = string.Empty;
        public string Wartosc { get; set; } = string.Empty;
    }

    public class PozycjaModel
    {
        public int Id { get; set; }
        public string Kod { get; set; } = string.Empty;
        public int Ilosc { get; set; }
    }
}