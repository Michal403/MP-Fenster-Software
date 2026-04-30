using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MP_Fenster_App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Pozwala złapać okno w dowolnym miejscu i je przesunąć
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        // Zamknięcie aplikacji
        private void ButtonAnuluj_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // LOGIKA LOGOWANIA
        private void BtnZaloguj_Click(object sender, RoutedEventArgs e)
        {
            string login = TxtUser.Text;
            string haslo = TxtPass.Text;

            // Tymczasowe sprawdzenie (zanim podłączymy bazę danych)
            if (login == "admin" && haslo == "123")
            {
                MessageBox.Show("Zalogowano pomyślnie!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                // Tu w przyszłości otworzymy nowe okno
            }
            else
            {
                MessageBox.Show("Wpisz poprawny login lub hasło!", "Błąd logowania", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // OBSŁUGA PLACEHOLDERÓW (Znikający tekst)
        private void TxtUser_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TxtUser.Text == "wpisz login...")
            {
                TxtUser.Text = "";
                TxtUser.Foreground = Brushes.Black;
            }
        }

        private void TxtUser_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtUser.Text))
            {
                TxtUser.Text = "wpisz login...";
                TxtUser.Foreground = Brushes.Gray;
            }
        }

        // Poprawiona obsługa hasła
        private void TxtPass_GotFocus(object sender, RoutedEventArgs e)
        {
            // Sprawdź dokładnie, czy tekst w "" jest identyczny jak w pliku XAML
            if (TxtPass.Text == "wpisz hasło...")
            {
                TxtPass.Text = "";
                TxtPass.Foreground = Brushes.Black;
            }
        }

        private void TxtPass_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtPass.Text))
            {
                TxtPass.Text = "wpisz hasło...";
                TxtPass.Foreground = Brushes.Gray;
            }
        }
    }
}