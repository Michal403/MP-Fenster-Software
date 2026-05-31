using System.Windows;

namespace MP_Fenster_App
{
    public partial class UwagiPozycjiWindow : Window
    {
        public string Uwagi => TxtUwagi.Text.Trim();

        public UwagiPozycjiWindow(PozycjaZlecenia pozycja)
        {
            InitializeComponent();
            TxtNaglowek.Text = $"Pozycja {pozycja.Poz}: {pozycja.Oznaczenie}";
            TxtUwagi.Text = pozycja.UwagiPozycji;
        }

        private void BtnZapisz_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnAnuluj_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
