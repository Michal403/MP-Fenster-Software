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
            okno.Show(); // Otwarte bez blokowania wątku interfejsu
        }

        // KAFELEK 2: Przegląd wszystkich zamówień w bazie Docker
        private void BtnPrzeglad_Click(object sender, RoutedEventArgs e)
        {
            // Przekazujemy rolę "Technolog" - system pominie klauzulę WHERE i pokaże rejestr globalny
            StatusZlecen oknoRejestru = new StatusZlecen(_user, "Technolog");
            oknoRejestru.Show();
        }

        // KAFELEK 3: Statusy dokumentacji technicznej
        private void BtnStatus_Click(object sender, RoutedEventArgs e)
        {
            StatusZlecen oknoRejestru = new StatusZlecen(_user, "Technolog");
            oknoRejestru.Show();
        }

        private void BtnProdukcja_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Moduł integracji z linią produkcyjną (Generowanie plików sterujących CNC).", "Status produkcji");
        }

        // KAFELEK 5: Przekierowanie do tego samego rejestru globalnego w celu weryfikacji i zdjęcia blokad
        private void BtnAkceptacja_Click(object sender, RoutedEventArgs e)
        {
            StatusZlecen oknoRejestru = new StatusZlecen(_user, "Technolog");
            oknoRejestru.Show();
        }

        private void BtnBaza_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Słowniki techniczne: Edycja tabel [SystemyProfilowe], [PakietySzybowe], [KlamkiKatalog] prosto z poziomu uprawnień Technologa.", "Baza danych");
        }
    }
}