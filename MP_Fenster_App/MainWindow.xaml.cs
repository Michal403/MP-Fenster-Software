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

            // 1. Sprawdzamy Handlowca/Admina
            if (login.ToLower() == "admin" || login.ToLower() == "michał" || login.ToLower() == "michal")
            {
                HandlowiecWindow oknoHandlowca = new HandlowiecWindow(login);
                oknoHandlowca.Show();
                this.Close();
            }
            // 2. JEŚLI NIE HANDLOWIEC, to sprawdzamy Technologa (używamy ELSE IF)
            else if (login.ToLower() == "technolog")
            {
                TechnologWindow oknoTech = new TechnologWindow(login);
                oknoTech.Show();
                this.Close();
            }
            // 3. JEŚLI NIKT Z POWYŻSZYCH, to błąd
            else
            {
                MessageBox.Show("Błędny login lub hasło!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}