# M+P Fenster Software

## Opis projektu

**M+P Fenster Software** to desktopowa aplikacja bazodanowa wspierająca przygotowywanie ofert, tworzenie zleceń oraz obsługę procesu produkcyjnego w firmie zajmującej się stolarką okienną i drzwiową.

Głównym celem projektu jest zastąpienie ręcznie prowadzonych ofert, arkuszy kalkulacyjnych oraz niespójnych słowników technicznych jednym, centralnym systemem.

Aplikacja pozwala przeprowadzić cały proces od utworzenia klienta i przygotowania oferty, przez konfigurację produktu i kontrolę techniczną, aż do przekształcenia oferty w zlecenie produkcyjne.

Dane wykorzystywane przez aplikację przechowywane są w relacyjnej bazie danych Microsoft SQL Server o nazwie:

```text
SeaSharkDB
```

Projekt został wykonany jako aplikacja WPF przeznaczona dla systemu Windows.

---

## Główne cele projektu

Najważniejsze cele projektu to:

* centralne przechowywanie danych klientów, ofert i zleceń,
* automatyzacja procesu przygotowywania ofert,
* ograniczenie liczby błędów podczas konfiguracji produktów,
* automatyczne pobieranie cen ze słowników bazodanowych,
* obliczanie wartości netto i brutto dokumentu,
* kontrolowanie ograniczeń technicznych produktów,
* obsługa decyzji Technologa,
* rozdzielenie funkcji według roli użytkownika,
* zapewnienie spójnego zapisu danych,
* umożliwienie edycji słowników bez zmiany kodu aplikacji.

---

## Technologie

W projekcie wykorzystano następujące technologie:

| Technologia              | Zastosowanie                      |
| ------------------------ | --------------------------------- |
| C#                       | implementacja logiki aplikacji    |
| .NET 8                   | platforma uruchomieniowa          |
| WPF                      | interfejs aplikacji desktopowej   |
| XAML                     | projektowanie widoków             |
| Microsoft SQL Server     | relacyjna baza danych             |
| Microsoft.Data.SqlClient | komunikacja z bazą danych         |
| ADO.NET                  | wykonywanie operacji bazodanowych |
| Docker                   | lokalne uruchomienie SQL Server   |
| HTML/CSS                 | generowanie dokumentu oferty      |
| Git i GitHub             | kontrola wersji projektu          |

Projekt nie korzysta z Entity Framework ani innego systemu ORM.

Komunikacja z bazą danych odbywa się bezpośrednio przy użyciu klas:

* `SqlConnection`,
* `SqlCommand`,
* `SqlDataReader`,
* `SqlDataAdapter`,
* `SqlTransaction`.

Dzięki temu zapytania SQL oraz sposób zapisywania danych są bezpośrednio kontrolowane w kodzie aplikacji.

---

## Funkcjonalności

### Logowanie użytkowników

Aplikacja posiada moduł logowania, który sprawdza dane użytkownika zapisane w tabeli `Uzytkownicy`.

Po poprawnym zalogowaniu system pobiera rolę użytkownika i otwiera odpowiedni panel.

System obsługuje trzy główne role:

* Handlowiec,
* Technolog,
* Administrator.

W przypadku podania niepoprawnego loginu lub hasła użytkownik otrzymuje komunikat o błędzie.

### Zarządzanie klientami

Aplikacja umożliwia:

* dodawanie nowych klientów,
* zapisywanie nazwy klienta lub firmy,
* przechowywanie adresu, numeru telefonu oraz numeru NIP,
* przypisywanie klientów do Handlowca,
* przeglądanie zapisanych klientów,
* wybieranie klienta podczas tworzenia oferty.

### Tworzenie ofert i zleceń

Handlowiec może:

* utworzyć nową ofertę,
* otworzyć istniejącą ofertę,
* zapisywać postęp pracy,
* dodawać wiele pozycji,
* kopiować pozycje,
* usuwać pozycje,
* zmieniać kolejność pozycji,
* zapisać ofertę w bazie danych,
* przekształcić poprawną ofertę w zlecenie.

Każda oferta zawiera nagłówek dokumentu oraz przypisane do niego pozycje.

### Konfiguracja produktów

Dla każdej pozycji użytkownik może określić między innymi:

* rodzaj produktu,
* system profilowy,
* szerokość,
* wysokość,
* liczbę sztuk,
* pakiet szybowy,
* ramkę dystansową,
* kolor okleiny,
* kolor uszczelki,
* kolor bazy,
* klasę bezpieczeństwa,
* wariant okuć,
* rodzaj zawiasów,
* typ klamki,
* kolor klamki,
* listwę podparapetową.

Większość dostępnych opcji jest pobierana bezpośrednio ze słowników znajdujących się w bazie danych.

### Kalkulacja finansowa

Aplikacja automatycznie oblicza wartość przygotowywanej oferty.

Obliczenia uwzględniają:

* powierzchnię produktu,
* cenę bazową systemu profilowego,
* dopłaty za wybrane komponenty,
* liczbę sztuk,
* rabat procentowy,
* koszt transportu,
* koszt montażu,
* podatek VAT,
* wartość netto,
* całkowitą wartość brutto.

Ceny i stawki są pobierane z bazy danych, dzięki czemu ich zmiana nie wymaga przebudowania całej aplikacji.

### Walidacja techniczna

System sprawdza wymiary produktów na podstawie danych zapisanych w tabeli:

```text
OgraniczeniaSystemowe
```

Jeżeli szerokość lub wysokość produktu przekracza dopuszczalne wartości, pozycja zostaje oznaczona jako problem techniczny.

Taka pozycja:

* zostaje odpowiednio oznaczona w aplikacji,
* może zostać zablokowana przed zapisaniem lub zaksięgowaniem,
* trafia do kolejki Technologa,
* wymaga zatwierdzenia odstępstwa technicznego.

Oferta posiadająca niezaakceptowany błąd techniczny nie może zostać przekształcona w zlecenie produkcyjne.

### Akceptacja Technologa

Technolog może:

* przeglądać oferty wymagające decyzji,
* analizować parametry produktu,
* sprawdzać przekroczone ograniczenia,
* dodawać komentarze techniczne,
* akceptować odstępstwa,
* odblokować dokument do dalszej realizacji.

Informacja o akceptacji oraz komentarz Technologa są zapisywane w systemie.

### Obsługa statusów

System pozwala rozróżniać dokumenty oraz etapy ich realizacji.

Przykładowe statusy:

* oferta,
* zlecenie,
* nie rozpoczęto,
* produkcja rozpoczęta,
* w trakcie realizacji,
* produkcja zakończona.

Technolog może aktualizować status produkcji zgodnie z aktualnym etapem realizacji zlecenia.

### Edycja słowników technicznych

Technolog może edytować wybrane słowniki techniczne zapisane w bazie danych.

Słowniki mogą zawierać między innymi:

* systemy profilowe,
* pakiety szybowe,
* ramki dystansowe,
* kolory,
* okucia,
* klamki,
* szprosy,
* listwy,
* ograniczenia gabarytowe,
* parametry finansowe,
* ceny transportu i montażu.

Dzięki przechowywaniu słowników w bazie danych można aktualizować ofertę produktową bez ręcznego zmieniania kodu aplikacji.

### Panel Administratora

Administrator posiada dostęp do panelu zarządzania bazą danych.

Panel umożliwia:

* pobieranie listy tabel,
* przeglądanie zawartości tabel,
* edytowanie rekordów,
* zapisywanie zmian,
* tworzenie tabel,
* usuwanie tabel,
* dodawanie kolumn,
* usuwanie kolumn,
* zarządzanie danymi systemowymi.

### Generowanie dokumentu oferty

Aplikacja pozwala wygenerować dokument oferty w formacie HTML.

Dokument może zostać:

* otwarty w przeglądarce,
* wydrukowany,
* zapisany jako plik PDF przy użyciu funkcji drukowania przeglądarki.

Wygenerowany dokument zawiera informacje o kliencie, dane oferty oraz skonfigurowane pozycje.

---

## Role użytkowników

### Handlowiec

Handlowiec może:

* zarządzać klientami,
* tworzyć nowe oferty,
* edytować istniejące oferty,
* konfigurować produkty,
* dodawać i usuwać pozycje,
* obliczać ceny,
* przeglądać własne dokumenty,
* sprawdzać statusy zleceń,
* generować dokument oferty.

### Technolog

Technolog może:

* przeglądać dokumenty wymagające decyzji,
* akceptować odstępstwa techniczne,
* dodawać komentarze,
* edytować słowniki techniczne,
* przeglądać zlecenia,
* aktualizować status produkcji.

### Administrator

Administrator może:

* przeglądać tabele bazy danych,
* edytować dane,
* zarządzać strukturą tabel,
* wykonywać operacje administracyjne,
* kontrolować dane systemowe.

---

## Baza danych

Główna baza danych projektu nosi nazwę:

```text
SeaSharkDB
```

Do najważniejszych tabel należą:

| Tabela                  | Przeznaczenie                    |
| ----------------------- | -------------------------------- |
| `Uzytkownicy`           | konta i role użytkowników        |
| `Klienci`               | dane klientów                    |
| `Zlecenia`              | nagłówki ofert i zleceń          |
| `PozycjeZlecenia`       | pozycje przypisane do dokumentów |
| `SystemyProfilowe`      | systemy profili i ceny bazowe    |
| `RamkiDystansowe`       | rodzaje ramek oraz dopłaty       |
| `CennikUslug`           | ceny transportu i montażu        |
| `OgraniczeniaSystemowe` | ograniczenia wymiarowe produktów |

Baza danych zawiera także dodatkowe słowniki opisujące szyby, kolory, okucia, klamki oraz inne elementy produktu.

---

## Bezpieczeństwo i spójność danych

W projekcie zastosowano:

* parametryzowane zapytania SQL,
* obsługę wyjątków,
* transakcje bazodanowe,
* wycofywanie operacji przy użyciu `Rollback`,
* rozdzielenie funkcji według roli użytkownika,
* kontrolę statusów technicznych,
* blokadę zapisu niepoprawnych dokumentów,
* filtrowanie danych według zalogowanego użytkownika.

Nagłówek zlecenia oraz jego pozycje są zapisywane w ramach jednej transakcji.

Jeżeli podczas zapisu wystąpi błąd, wykonane operacje zostają wycofane. Zapobiega to zapisaniu niekompletnego dokumentu.

---

## Wymagania systemowe

Do uruchomienia projektu potrzebne są:

* Windows 10 lub Windows 11,
* .NET 8 SDK,
* Visual Studio 2022,
* pakiet do tworzenia aplikacji desktopowych .NET,
* Microsoft SQL Server,
* opcjonalnie Docker Desktop,
* baza danych `SeaSharkDB`,
* pakiet NuGet `Microsoft.Data.SqlClient`.

---

## Instalacja

### 1. Pobranie repozytorium

Repozytorium można pobrać jako plik ZIP z GitHuba lub sklonować przy użyciu Git:

```bash
git clone ADRES_REPOZYTORIUM
```

### 2. Przejście do katalogu aplikacji

```bash
cd MP-Fenster-Software/MP_Fenster_App
```

### 3. Przywrócenie pakietów

```bash
dotnet restore
```

### 4. Przygotowanie bazy danych

Należy uruchomić Microsoft SQL Server lokalnie lub w kontenerze Docker.

Następnie należy utworzyć lub odtworzyć bazę:

```text
SeaSharkDB
```

Baza musi zawierać wymagane tabele, relacje oraz dane słownikowe.

### 5. Konfiguracja połączenia

W aplikacji należy skonfigurować connection string zgodny z lokalną konfiguracją SQL Server.

Przykład:

```csharp
Server=localhost;
Database=SeaSharkDB;
User Id=NAZWA_UZYTKOWNIKA;
Password=HASLO;
TrustServerCertificate=True;
```

> Prawdziwego loginu i hasła do SQL Server nie należy umieszczać w publicznym repozytorium GitHub.

---

## Uruchamianie projektu

### Budowanie aplikacji

```bash
dotnet build
```

### Uruchomienie aplikacji

```bash
dotnet run
```

Projekt można również otworzyć w Visual Studio 2022 i uruchomić przyciskiem `Start`.

Przed uruchomieniem należy upewnić się, że:

* SQL Server działa,
* baza `SeaSharkDB` istnieje,
* dane połączenia są poprawne,
* użytkownik SQL Server ma uprawnienia do odczytu i zapisu.

---

## Struktura projektu

```text
MP_Fenster_App/
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── HandlowiecWindow.xaml
├── HandlowiecWindow.xaml.cs
├── TechnologWindow.xaml
├── TechnologWindow.xaml.cs
├── AdminPanelWindow.xaml
├── AdminPanelWindow.xaml.cs
├── NoweZlecenieWindow.xaml
├── NoweZlecenieWindow.xaml.cs
├── ZarzadzanieKlientamiWindow.xaml
├── ZarzadzanieKlientamiWindow.xaml.cs
├── StatusZlecen.xaml
├── StatusZlecen.xaml.cs
├── TechnologBazaDanychWindow.xaml
├── TechnologBazaDanychWindow.xaml.cs
├── PdfOfertaService.cs
├── KlientService.cs
├── Ikony/
└── MP_Fenster_App.csproj
```

### Najważniejsze moduły

| Moduł                        | Odpowiedzialność              |
| ---------------------------- | ----------------------------- |
| `MainWindow`                 | logowanie użytkowników        |
| `HandlowiecWindow`           | panel Handlowca               |
| `TechnologWindow`            | panel Technologa              |
| `AdminPanelWindow`           | panel Administratora          |
| `NoweZlecenieWindow`         | tworzenie i edycja ofert      |
| `ZarzadzanieKlientamiWindow` | obsługa klientów              |
| `StatusZlecen`               | przegląd statusów dokumentów  |
| `TechnologBazaDanychWindow`  | edycja słowników technicznych |
| `PdfOfertaService`           | generowanie dokumentu oferty  |
| `KlientService`              | operacje dotyczące klientów   |

---

## Ograniczenia obecnej wersji

Projekt ma charakter akademicki i demonstracyjny.

Obecna wersja posiada następujące ograniczenia:

* aplikacja jest przeznaczona głównie do pracy lokalnej,
* połączenie z bazą odbywa się bezpośrednio,
* część danych połączenia znajduje się w kodzie,
* system nie posiada osobnej warstwy API,
* hasła użytkowników powinny zostać dodatkowo zabezpieczone,
* dokument oferty jest generowany jako HTML,
* część operacji administracyjnych wymaga zachowania ostrożności,
* aplikacja nie jest obecnie przeznaczona do zastosowania produkcyjnego.

---

## Możliwe kierunki rozwoju

Projekt może zostać w przyszłości rozbudowany o:

* przeniesienie connection stringa do pliku konfiguracyjnego,
* wykorzystanie zmiennych środowiskowych,
* bezpieczne haszowanie haseł,
* dodanie osobnej warstwy API,
* historię zmian danych,
* bardziej rozbudowany system uprawnień,
* skrypt automatycznie tworzący bazę danych,
* natywne generowanie plików PDF,
* testy jednostkowe,
* testy integracyjne,
* obsługę wielu stanowisk,
* instalator aplikacji,
* automatyczne tworzenie kopii zapasowych.

---

## Informacje akademickie

Projekt wykonany w ramach zajęć związanych z projektowaniem i implementacją baz danych.

**Akademia Nauk Stosowanych w Nowym Sączu**
Wydział Nauk Inżynieryjnych
Katedra Informatyki

Prowadzący: **mgr inż. Nikodem Bulanda**

Rok realizacji: **2026**

---

## Autorzy

* **Michał Marecik**
* **Dawid Piech**
