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
using Microsoft.Data.SqlClient;

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
            string loginInput = TxtUser.Text;
            string hasloInput = TxtPass.Text;

            // To jest klucz do Twojej bazy na Azure
            string connectionString = "TUTAJ_WKLEJ_CONNECTION_STRING";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Szukamy roli użytkownika w bazie
                    string query = "SELECT Rola FROM Uzytkownicy WHERE Login = @login AND Haslo = @haslo";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Parametry chronią przed atakami (SQL Injection)
                        command.Parameters.AddWithValue("@login", loginInput);
                        command.Parameters.AddWithValue("@haslo", hasloInput);

                        object result = command.ExecuteScalar();

                        if (result != null)
                        {
                            string rola = result.ToString();

                            if (rola == "Handlowiec")
                            {
                                new HandlowiecWindow(loginInput).Show();
                            }
                            else if (rola == "Technolog")
                            {
                                new TechnologWindow(loginInput).Show();
                            }

                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Błędny login lub hasło!", "Błąd Bazy Azure");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Jeśli np. zapomnisz dodać IP do Firewall'a w Azure, tutaj wyskoczy błąd
                MessageBox.Show("Problem z połączeniem: " + ex.Message);
            }
        }

    }
}