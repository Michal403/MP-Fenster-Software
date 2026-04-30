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

        // LOGIKA LOGOWANIA - Zostawiamy tylko tę wersję, która otwiera nowe okno
        private void BtnZaloguj_Click(object sender, RoutedEventArgs e)
        {
            string login = TxtUser.Text;
            string haslo = TxtPass.Text;

            // Statyczne sprawdzenie danych
            if (login == "admin" && haslo == "123")
            {
                // 1. Tworzymy instancję nowego okna (musisz je najpierw dodać do projektu!)
                HandlowiecWindow oknoHandlowca = new HandlowiecWindow();

                // 2. Pokazujemy nowe okno
                oknoHandlowca.Show();

                // 3. Zamykamy okno logowania
                this.Close();
            }
            else
            {
                MessageBox.Show("Nieprawidłowe dane! Spróbuj: admin / 123", "Błąd logowania",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // OBSŁUGA PLACEHOLDERÓW (Login)
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

        // OBSŁUGA PLACEHOLDERÓW (Hasło)
        private void TxtPass_GotFocus(object sender, RoutedEventArgs e)
        {
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
        private void BtnZaloguj_Click(object sender, RoutedEventArgs e)
        {
            string login = TxtUser.Text;
            string haslo = TxtPass.Text;

            if (login == "admin" || login == "Michał") // Dodajmy Twoje imię do testów
            {
                // Przekazujemy login do konstruktora nowego okna
                HandlowiecWindow oknoHandlowca = new HandlowiecWindow(login);
                oknoHandlowca.Show();
                this.Close();
            }
            // ... reszta Twojego kodu erroru
        }
    }
}