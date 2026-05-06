using System;
using System.Windows;
using System.Windows.Controls;

namespace MP_Fenster_App
{
    public partial class HandlowiecWindow : Window
    {
        private string _zalogowanyUser;

        public HandlowiecWindow(string user)
        {
            InitializeComponent();

            _zalogowanyUser = user;

            // Ustawiamy dane na pasku statusu (naprawa Kowalskiego)
            StatusUser.Content = _zalogowanyUser.ToUpper();
            StatusDate.Content = DateTime.Now.ToString("dd.MM.yyyy");
            StatusTime.Content = DateTime.Now.ToString("HH:mm");

            // Wywołujemy funkcję, która w przyszłości pociągnie dane z Azure
            OdswiezZleceniaZBazy();
        }

        private void OdswiezZleceniaZBazy()
        {
            // Czyścimy drzewo przed ładowaniem
            MyOrdersTree.Items.Clear();

            // Tworzymy główny węzeł
            TreeViewItem root = new TreeViewItem { Header = "Zlecenia", IsExpanded = true };

            // SYMULACJA DANYCH Z BAZY (tu wstawimy SQL Connection potem)
            // Na razie dodajemy przykłady, żebyś widział, że działa dynamicznie
            root.Items.Add(new TreeViewItem { Header = "Zlecenie 45689 - Aktywne" });
            root.Items.Add(new TreeViewItem { Header = "Zlecenie 45700 - Wycena" });
            root.Items.Add(new TreeViewItem { Header = "Zlecenie 45812 - Nowe" });

            MyOrdersTree.Items.Add(root);
        }

        // --- Obsługa menu i przycisków ---

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }

        private void BtnNowe_Click(object sender, RoutedEventArgs e)
        {
            NoweZlecenieWindow okno = new NoweZlecenieWindow(_zalogowanyUser);
            okno.ShowDialog(); // Otwiera okno kreatora
        }

        private void BtnPrzeglad_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Przegląd wszystkich zleceń użytkownika " + _zalogowanyUser);
        }

        private void BtnStatus_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Statusy zleceń");

        private void BtnKlienci_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Baza klientów");
    }
}