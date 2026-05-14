using System.Collections.ObjectModel;
using System.Windows;

namespace MP_Fenster_App
{
    public partial class NoweZlecenieWindow : Window
    {
        // To jest nasza lista, która automatycznie odświeża tabelę jak coś dodasz/usuniesz
        public ObservableCollection<PozycjaZlecenia> ListaPozycji { get; set; }

        public NoweZlecenieWindow(string wprowadzilLogin)
        {
            InitializeComponent();

            // Ładujemy testowe dane z Twojego obrazka
            ListaPozycji = new ObservableCollection<PozycjaZlecenia>
            {
                new PozycjaZlecenia { Poz = "1", NrProd = "F100", Szt = 3, Rodzaj = "J", Oznaczenie = "Okno 1 kw." },
                new PozycjaZlecenia { Poz = "2", NrProd = "F201", Szt = 1, Rodzaj = "J", Oznaczenie = "Okno 2 kw. ze słup. ruc." },
                new PozycjaZlecenia { Poz = "3", NrProd = "Z100", Szt = 1, Rodzaj = "JP", Oznaczenie = "Zestaw" },
                new PozycjaZlecenia { Poz = "3.1", NrProd = "B100", Szt = 1, Rodzaj = "JP", Oznaczenie = "Drzwi balkonowe" },
                new PozycjaZlecenia { Poz = "3.1", NrProd = "F200", Szt = 1, Rodzaj = "JP", Oznaczenie = "Okno 2 kw. ze słup. sta." }
            };

            // Przypinamy listę do tabeli z prawej strony
            GridPozycje.ItemsSource = ListaPozycji;
        }

        // GRUPA 1: Zapis i wyjście
        private void BtnZamknijZapisz_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Zlecenie zapisane w bazie danych.", "Zapis i Wyjście");
            this.Close();
        }

        private void BtnWyjdzBezZapisu_Click(object sender, RoutedEventArgs e)
        {
            var odpowiedz = MessageBox.Show("Czy na pewno chcesz wyjść ze zlecenia bez zapisu?", "Potwierdzenie", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (odpowiedz == MessageBoxResult.Yes)
            {
                this.Close(); // Wychodzimy
            }
        }

        private void BtnZapiszPostepy_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Postępy zapisane poprawnie.", "Zapis");
        }

        private void BtnDrukuj_Click(object sender, RoutedEventArgs e)
        {
            var odpowiedz = MessageBox.Show("Czy chcesz wydrukować ofertę PDF?", "Drukuj", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (odpowiedz == MessageBoxResult.Yes)
            {
                MessageBox.Show("Generuję PDF...", "Drukuj");
            }
        }

        // GRUPA 2: Klienci i Ceny
        private void BtnDodajKlienta_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Otwieram okno szybkiego dodawania klienta.", "Nowy Klient");
        }

        private void BtnListaKlientow_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Otwieram listę Twoich klientów.", "Klienci");
        }

        private void BtnAktualizujCeny_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Aktualizuję ceny w ofercie...", "Aktualizacja cen");
        }

        // GRUPA 3: Operacje na pozycjach
        private void BtnPodgladPozycji_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Podgląd szczegółów pozycji.", "Podgląd");
        }

        private void BtnNowaPozycja_Click(object sender, RoutedEventArgs e)
        {
            // Dodajemy nową pozycję do tabeli w locie
            ListaPozycji.Add(new PozycjaZlecenia { Poz = "4", NrProd = "NEW", Szt = 1, Rodzaj = "J", Oznaczenie = "Nowe okno" });
        }

        private void BtnKopiujPozycje_Click(object sender, RoutedEventArgs e)
        {
            var zaznaczona = GridPozycje.SelectedItem as PozycjaZlecenia;
            if (zaznaczona != null)
            {
                ListaPozycji.Add(new PozycjaZlecenia
                {
                    Poz = "Kopia",
                    NrProd = zaznaczona.NrProd,
                    Szt = zaznaczona.Szt,
                    Rodzaj = zaznaczona.Rodzaj,
                    Oznaczenie = zaznaczona.Oznaczenie + " (Kopia)"
                });
            }
            else
            {
                MessageBox.Show("Zaznacz pozycję w tabeli, aby ją skopiować.", "Info");
            }
        }

        private void BtnWstawSpecjalne_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funkcja wstawiania (kratka)...", "Wstaw");
        }

        private void BtnUsunPozycje_Click(object sender, RoutedEventArgs e)
        {
            var zaznaczona = GridPozycje.SelectedItem as PozycjaZlecenia;
            if (zaznaczona != null)
            {
                var odpowiedz = MessageBox.Show("Czy na pewno chcesz usunąć tę pozycję?", "Usuwanie", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (odpowiedz == MessageBoxResult.Yes)
                {
                    ListaPozycji.Remove(zaznaczona);
                }
            }
            else
            {
                MessageBox.Show("Zaznacz pozycję w tabeli, aby ją usunąć.", "Info");
            }
        }

        private void BtnKopiujZInnego_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Wybierz zlecenie do skopiowania okna.", "Kopiuj z innego");
        }

        // GRUPA 4: Sortowanie i odświeżanie
        private void BtnPrzesunNaGore_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Przesuwam na szczyt listy.");
        private void BtnPrzesunWGore_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Przesuwam o jedno pole w górę.");
        private void BtnPrzesunWDol_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Przesuwam o jedno pole w dół.");
        private void BtnPrzesunNaDol_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Przesuwam na sam dół listy.");
        private void BtnOdswiezPozycje_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Odświeżam wybraną pozycję.");

        // GRUPA 5: Walidacja
        private void BtnBledy_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Panel ograniczeń i wykrzykników.");
    }

    // Klasa opisująca to, co siedzi w prawym DataGridzie
    public class PozycjaZlecenia
    {
        public string Poz { get; set; }
        public string NrProd { get; set; }
        public int Szt { get; set; }
        public string Rodzaj { get; set; }
        public string Oznaczenie { get; set; }
    }
}