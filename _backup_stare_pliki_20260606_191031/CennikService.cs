namespace MP_Fenster_App.Services
{
    public class CennikService
    {
        private readonly string _connString;

        public CennikService(string connString)
        {
            _connString = connString;
        }

        public double ObliczCenePozycji(PozycjaZlecenia pozycja)
        {
            if (pozycja.Szerokosc <= 0 || pozycja.Wysokosc <= 0 || pozycja.Szt <= 0)
            {
                return 0;
            }

            double m2 = (pozycja.Szerokosc / 1000.0) * (pozycja.Wysokosc / 1000.0);
            double cenaM2Baza = 410;

            if (pozycja.SystemOkna.Contains("Schuco Living MD")) cenaM2Baza = 580;
            else if (pozycja.SystemOkna.Contains("Salamander BluEvolution 82")) cenaM2Baza = 620;
            else if (pozycja.SystemOkna.Contains("Veka Softline 82")) cenaM2Baza = 590;

            if (pozycja.Wypelnienie == "3-48") cenaM2Baza += 130;
            else if (pozycja.Wypelnienie == "4-48") cenaM2Baza += 270;

            return m2 * cenaM2Baza * pozycja.Szt;
        }
    }
}