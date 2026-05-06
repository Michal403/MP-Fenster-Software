using System;
using System.Windows;
using System.Windows.Controls;

namespace MP_Fenster_App
{
    public partial class TechnologWindow : Window
    {
        private string _user;

        public TechnologWindow(string user)
        {
            InitializeComponent();
            _user = user;

            // Ustawienia paska statusu
            StatusUser.Content = _user.ToUpper();
            StatusDate.Content = DateTime.Now.ToString("dd.MM.yyyy");
            StatusTime.Content = DateTime.Now.ToString("HH:mm");

            WczytajKolejkeTechnologa();
        }

        private void WczytajKolejkeTechnologa()
        {
            TechOrdersTree.Items.Clear();
            TreeViewItem root = new TreeViewItem { Header = "Zlecenia", IsExpanded = true };

            // Symulacja danych z obrazka (Zlecenia z ikonami)
            root.Items.Add(new TreeViewItem { Header = "Zlecenie 45689" });
            root.Items.Add(new TreeViewItem { Header = "Zlecenie 45690" });
            root.Items.Add(new TreeViewItem { Header = "⚠ Zlecenie 45720 (Błąd wymiarów)" });
            root.Items.Add(new TreeViewItem { Header = "Zlecenie 45725" });
            root.Items.Add(new TreeViewItem { Header = "⚠ Zlecenie 45725 (Brak profilu)" });
            root.Items.Add(new TreeViewItem { Header = "Zlecenie 45726" });

            TechOrdersTree.Items.Add(root);
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            this.Close();
        }

        // --- OBSŁUGA PRZYCISKÓW TECHNOLOGA ---

        private void BtnNowe_Click(object sender, RoutedEventArgs e)
        {
            NoweZlecenieWindow okno = new NoweZlecenieWindow(_user);
            okno.ShowDialog(); // Otwiera okno kreatora
        }

        private void BtnPrzeglad_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show("Przegląd zleceń w toku...");

        private void BtnStatus_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show("Sprawdzanie statusów dokumentacji...");

        private void BtnProdukcja_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show("Przesyłanie zleceń na produkcję...");

        private void BtnAkceptacja_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show("Lista zleceń wymagających zatwierdzenia technicznego");

        private void BtnBaza_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show("Łączenie z bazą danych profili i okuć...");
    }
}