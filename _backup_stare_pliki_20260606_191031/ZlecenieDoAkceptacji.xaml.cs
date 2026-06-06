using System.Windows;

namespace MP_Fenster_App
{
    public partial class ZlecenieDoAkceptacji : Window
    {
        // Pole przechowujące referencję do pozycji przekazanej z głównego okna
        private MP_Fenster_App.PozycjaZlecenia _pozycja;

        // Konstruktor okna akceptacji
        public ZlecenieDoAkceptacji(MP_Fenster_App.PozycjaZlecenia pos)
        {
            InitializeComponent();
            _pozycja = pos;
        }

        // Kliknięcie przycisku "Zatwierdź odstępstwo"
        private void BtnZatwierdz_Click(object sender, RoutedEventArgs e)
        {
            if (_pozycja != null)
            {
                _pozycja.CzyZatwierdzone = true;

                // Sprawdzamy czy pole tekstowe zostało poprawnie zainicjalizowane i odczytane z XAML
                if (TxtKomentarzTechnologa != null && !string.IsNullOrWhiteSpace(TxtKomentarzTechnologa.Text))
                {
                    _pozycja.UwagiTechnologa = TxtKomentarzTechnologa.Text;
                }
                else
                {
                    _pozycja.UwagiTechnologa = "Zaakceptowano odstępstwo gabarytowe (Technolog)";
                }

                this.DialogResult = true;
            }
        }

        // Kliknięcie przycisku "Anuluj"
        private void BtnAnuluj_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}