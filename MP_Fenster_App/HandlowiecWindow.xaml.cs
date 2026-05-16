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

            // Inicjalizacja paska stanu systemu Fenster
            StatusUser.Content = _zalogowanyUser.ToUpper();
            StatusDate.Content = DateTime.Now.ToString("dd.MM.yyyy");
            StatusTime.Content = DateTime.Now.ToString("HH:mm");

            OdswiezZleceniaZBazy();
        }

        private void OdswiezZleceniaZBazy()
        {
            MyOrdersTree.Items.Clear();
            TreeViewItem root = new TreeViewItem { Header = "Zlecenia", IsExpanded = true };

            root.Items.Add(new TreeViewItem { Header = "Zlecenie 45689 - Aktywne" });
            root.Items.Add(new TreeViewItem { Header = "Zlecenie 45700 - Wycena" });
            root.Items.Add(new TreeViewItem { Header = "Zlecenie 45812 - Nowe" });

            MyOrdersTree.Items.Add(root);
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            MainWindow loginWindow = new MainWindow();
            loginWindow.Show();
            this.Close();
        }

        // KAFELEK 1: NOWE ZLECENIE (Kreator)
        private void BtnNowe_Click(object sender, RoutedEventArgs e)
        {
            NoweZlecenieWindow okno = new NoweZlecenieWindow(_zalogowanyUser);
            okno.Show(); // Otwieramy normalnie w tle, żeby pulpit nie zamrażał
        }

        // KAFELEK 2: PRZEGLĄD ZLECEŃ (Podpięcie pod dynamiczny panel SQL)
        private void BtnPrzeglad_Click(object sender, RoutedEventArgs e)
        {
            // Przekazujemy login i rolę "Handlowiec" - system automatycznie odfiltruje tylko jego zlecenia
            StatusZlecen oknoRejestru = new StatusZlecen(_zalogowanyUser, "Handlowiec");
            oknoRejestru.Show();
        }

        // KAFELEK 3: STATUS ZLECEŃ (Przekierowanie do tego samego modułu statusowego bazy)
        private void BtnStatus_Click(object sender, RoutedEventArgs e)
        {
            StatusZlecen oknoRejestru = new StatusZlecen(_zalogowanyUser, "Handlowiec");
            oknoRejestru.Show();
        }

        private void BtnKlienci_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Moduł bazy klientów (CRM za pomocą Entity Framework Core).", "Baza klientów");
        }
    }
}