using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
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

        // OBSŁUGA PLACEHOLDERÓW (Tylko dla Loginu - TextBox)
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

        // UWAGA: Metody TxtPass_GotFocus i TxtPass_LostFocus zostały usunięte, 
        // ponieważ PasswordBox nie współpracuje z nimi w ten sposób (zamieniałby placeholder na kropki).

        private void BtnZaloguj_Click(object sender, RoutedEventArgs e)
        {
            // Pobieramy dane z pól
            string loginInput = TxtUser.Text.Trim();

            // KLUCZOWA ZMIANA: Pobieramy hasło z PasswordBox używając .Password
            string hasloInput = TxtPass.Password;

            // Połączenie do Twojego Dockera
            string connectionString = "Server=localhost;Database=SeaSharkDB;User Id=sa;Password=zaq1@WSX;TrustServerCertificate=True;";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT Rola FROM Uzytkownicy WHERE Login = @login AND Haslo = @haslo";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@login", loginInput);
                        command.Parameters.AddWithValue("@haslo", hasloInput);

                        object result = command.ExecuteScalar();

                        if (result != null)
                        {
                            // Naprawiamy ostrzeżenie o nullu
                            string rola = result?.ToString() ?? "";

                            if (rola == "Handlowiec")
                            {
                                new HandlowiecWindow(loginInput).Show();
                            }
                            else if (rola == "Technolog")
                            {
                                new TechnologWindow(loginInput).Show();
                            }
                            else if (rola == "Admin")
                            {
                                MessageBox.Show("Witaj Adminie! Masz pełne uprawnienia.", "Panel Administratora");
                                new HandlowiecWindow(loginInput).Show();
                            }

                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("Błędny login lub hasło!", "Błąd logowania SeaShark");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Problem z połączeniem (Docker): " + ex.Message, "Błąd Bazy");
            }
        }
    }
}