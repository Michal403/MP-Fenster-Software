using System.Windows;

namespace MP_Fenster_App
{
    public partial class BledyOgraniczenWindow : Window
    {
        private readonly PozycjaZlecenia _pozycja;
        private readonly bool _czyTechnolog;

        public BledyOgraniczenWindow(PozycjaZlecenia pozycja, bool czyTechnolog)
        {
            InitializeComponent();
            _pozycja = pozycja;
            _czyTechnolog = czyTechnolog;

            TxtKomentarz.Text = pozycja.KomentarzTechnologa;
            UstawWidok();
        }

        private void UstawWidok()
        {
            if (_pozycja.OdstepstwoZaakceptowane)
            {
                TxtStatus.Text = $"Pozycja {_pozycja.Poz}: odstępstwo zostało zaakceptowane przez technologa.";
                BtnAkceptuj.Visibility = Visibility.Collapsed;
                TxtKomentarz.IsReadOnly = true;
                TxtKomentarz.IsEnabled = false;
                TxtKomentarz.Text = string.IsNullOrWhiteSpace(_pozycja.KomentarzTechnologa)
                    ? "Brak komentarza technologa."
                    : _pozycja.KomentarzTechnologa;
                return;
            }

            if (_pozycja.StatusZablokowany)
            {
                TxtStatus.Text = _czyTechnolog
                    ? $"Pozycja {_pozycja.Poz} przekracza ograniczenia gabarytowe. Możesz zaakceptować odstępstwo i dopisać komentarz."
                    : $"Pozycja {_pozycja.Poz} przekracza ograniczenia gabarytowe. Zmień wymiary albo przekaż zlecenie technologowi do akceptacji.";

                BtnAkceptuj.Visibility = _czyTechnolog ? Visibility.Visible : Visibility.Collapsed;
                TxtKomentarz.IsReadOnly = !_czyTechnolog;
                TxtKomentarz.IsEnabled = _czyTechnolog;

                if (_czyTechnolog)
                {
                    if (string.IsNullOrWhiteSpace(TxtKomentarz.Text))
                    {
                        TxtKomentarz.Text = "Zaakceptowano bez gwarancji.";
                    }
                }
                else
                {
                    TxtKomentarz.Text = string.IsNullOrWhiteSpace(_pozycja.KomentarzTechnologa)
                        ? "Komentarz technologa pojawi się po akceptacji odstępstwa."
                        : _pozycja.KomentarzTechnologa;
                }

                return;
            }

            TxtStatus.Text = "Brak błędów gabarytowych dla wybranej pozycji.";
            BtnAkceptuj.Visibility = Visibility.Collapsed;
            TxtKomentarz.IsReadOnly = true;
            TxtKomentarz.IsEnabled = false;
            TxtKomentarz.Text = string.Empty;
        }

        private void BtnAkceptuj_Click(object sender, RoutedEventArgs e)
        {
            if (!_czyTechnolog)
            {
                MessageBox.Show("Tylko technolog albo administrator może zaakceptować odstępstwo.", "Brak uprawnień", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _pozycja.StatusZablokowany = false;
            _pozycja.OdstepstwoZaakceptowane = true;
            _pozycja.KomentarzTechnologa = TxtKomentarz.Text.Trim();
            DialogResult = true;
            Close();
        }

        private void BtnZamknij_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}



