using System.Windows;

namespace MP_Fenster_App
{
    public partial class HandlowiecWindow : Window
    {
        public HandlowiecWindow()
        {
            InitializeComponent();
        }

        // 1. Logika dla dużych kafelków (Buttons)

        private void BtnNowe_Click(object sender, RoutedEventArgs e)
        {
            // Tutaj w przyszłości otworzymy okno tworzenia zlecenia
            MessageBox.Show("Otwieranie formularza: Nowe zlecenie", "SeaShark ERP");
        }

        private void BtnPrzeglad_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ładowanie listy wszystkich zleceń...", "SeaShark ERP");
        }

        private void BtnStatus_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Sprawdzanie statusu produkcji i wysyłek...", "SeaShark ERP");
        }

        private void BtnKlienci_Click(object sender, RoutedEventArgs e)
        {
            // Logika, którą przygotowałeś wcześniej dla bazy klientów
            MessageBox.Show("Ładowanie bazy klientów z systemu...", "SeaShark ERP");
        }

        // 2. Logika nawigacji i wyjścia

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            // Powrót do okna logowania
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }

        // Jeśli masz przyciski z poprzedniej wersji (Dashboard/Wycena), 
        // możesz je tutaj zostawić lub usunąć, zależnie od tego, czy są w XAML
        private void BtnWycena_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Uruchamianie kalkulatora wycen...", "SeaShark ERP");
        }
    }
}