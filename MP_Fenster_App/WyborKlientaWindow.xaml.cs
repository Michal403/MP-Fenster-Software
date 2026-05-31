using MP_Fenster_App.Services;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace MP_Fenster_App
{
    public partial class WyborKlientaWindow : Window
    {
        private readonly DataView _view;

        public int WybraneId { get; private set; } = -1;
        public string WybranaNazwa { get; private set; } = string.Empty;

        public WyborKlientaWindow(string connString, string login)
        {
            InitializeComponent();

            try
            {
                var service = new KlientService(connString);
                _view = service.PobierzKlientowHandlowca(login).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie udało się pobrać klientów: " + ex.Message, "Błąd");
                _view = new DataTable().DefaultView;
            }

            DgKlienci.ItemsSource = _view;
        }

        private void TxtSzukaj_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_view.Table.Columns.Contains("NazwaKlienta") || !_view.Table.Columns.Contains("NIP"))
            {
                return;
            }

            string text = TxtSzukaj.Text.Replace("'", "''");
            _view.RowFilter = string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : $"NazwaKlienta LIKE '%{text}%' OR NIP LIKE '%{text}%'";
        }

        private void Zatwierdz()
        {
            if (DgKlienci.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Wybierz klienta z listy.", "Informacja");
                return;
            }

            WybraneId = Convert.ToInt32(row["IdKlienta"]);
            WybranaNazwa = row["NazwaKlienta"].ToString() ?? string.Empty;
            DialogResult = true;
            Close();
        }

        private void BtnWybierz_Click(object sender, RoutedEventArgs e) => Zatwierdz();
        private void DgKlienci_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => Zatwierdz();

        private void BtnAnuluj_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
