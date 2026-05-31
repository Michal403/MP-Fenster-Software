using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace MP_Fenster_App
{
    public partial class KopiujPozycjeWindow : Window
    {
        private readonly string _connString;
        private readonly ObservableCollection<PozycjaZlecenia> _pozycje = new();

        public List<PozycjaZlecenia> WybranePozycje { get; } = new();

        public KopiujPozycjeWindow(string connString)
        {
            InitializeComponent();
            _connString = connString;
            DgPozycje.ItemsSource = _pozycje;
        }

        private void BtnSzukaj_Click(object sender, RoutedEventArgs e)
        {
            _pozycje.Clear();

            if (string.IsNullOrWhiteSpace(TxtNumerZlecenia.Text))
            {
                MessageBox.Show("Wpisz numer zlecenia.", "Informacja");
                return;
            }

            try
            {
                using var cn = new SqlConnection(_connString);
                cn.Open();

                string sql = @"SELECT p.*
                               FROM PozycjeZlecenia p
                               JOIN Zlecenia z ON p.IdZlecenia = z.IdZlecenia
                               WHERE z.NumerZlecenia = @nr OR CONVERT(varchar(30), z.IdZlecenia) = @nr
                               ORDER BY p.IdPozycji";

                using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@nr", TxtNumerZlecenia.Text.Trim());

                using var dr = cmd.ExecuteReader();
                int licznik = 1;

                while (dr.Read())
                {
                    _pozycje.Add(new PozycjaZlecenia
                    {
                        Poz = licznik.ToString(),
                        NrProd = dr["NrProdukcyjny"].ToString() ?? "F100",
                        Szt = Convert.ToInt32(dr["Sztuk"]),
                        Rodzaj = "J",
                        Oznaczenie = dr["NrProdukcyjny"].ToString() ?? "Pozycja",
                        Szerokosc = Convert.ToInt32(dr["Szerokosc"]),
                        Wysokosc = Convert.ToInt32(dr["Wysokosc"]),
                        SystemOkna = "Schuco Living MD",
                        Wypelnienie = "2-24",
                        TypRamki = "ALU",
                        WariantUkladuKoloru = "W-W",
                        KolorOkleiny = "W",
                        KolorUszczelki = "SZARY",
                        KolorBazy = "Bialy",
                        KlasaBezpieczenstwa = "Standard",
                        WariantOkuc = "UR-P",
                        Zawiasy = "Standard",
                        TypKlamki = "Klamka aluminiowa Standard",
                        KolorKlamki = "Bialy",
                        ListwaPodparapetowa = "TAK",
                        UwagiPozycji = dr["Uwagi"].ToString() ?? string.Empty,
                        StatusZablokowany = dr["StatusTechniczny"].ToString() == "0"
                    });

                    licznik++;
                }

                if (_pozycje.Count == 0)
                {
                    MessageBox.Show("Nie znaleziono pozycji dla tego zlecenia.", "Informacja");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd odczytu pozycji: " + ex.Message, "Błąd");
            }
        }

        private void BtnKopiuj_Click(object sender, RoutedEventArgs e)
        {
            foreach (PozycjaZlecenia pozycja in DgPozycje.SelectedItems.OfType<PozycjaZlecenia>())
            {
                WybranePozycje.Add(pozycja.Klonuj());
            }

            if (WybranePozycje.Count == 0)
            {
                MessageBox.Show("Zaznacz przynajmniej jedną pozycję.", "Informacja");
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnAnuluj_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
