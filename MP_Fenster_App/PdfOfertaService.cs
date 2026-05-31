using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.Data.SqlClient;

namespace MP_Fenster_App.Services
{
    public class PdfOfertaService
    {
        private readonly string _connString;

        public PdfOfertaService()
            : this("Server=localhost;Database=SeaSharkDB;User Id=sa;Password=zaq1@WSX;TrustServerCertificate=True;")
        {
        }

        public PdfOfertaService(string connString)
        {
            _connString = connString;
        }

        public string UtworzRoboczyHtmlOferty(string numerZlecenia, string klient, IEnumerable<PozycjaZlecenia> pozycje)
        {
            string path = PrzygotujSciezke(numerZlecenia);
            var html = new StringBuilder();
            html.AppendLine(PoczatekHtml("Oferta " + numerZlecenia));
            html.AppendLine("<section class='hero'>");
            html.AppendLine("<div><div class='eyebrow'>Oferta handlowa</div>");
            html.AppendLine($"<h1>Oferta nr {H(numerZlecenia)}</h1>");
            html.AppendLine($"<p>Klient: <strong>{H(klient)}</strong></p></div>");
            html.AppendLine("<div class='stamp'>FENSTER 1.0</div>");
            html.AppendLine("</section>");
            html.AppendLine("<h2>Pozycje oferty</h2>");

            foreach (var p in pozycje)
            {
                html.AppendLine("<section class='position-card'>");
                html.AppendLine(ZbudujMiniaturePozycji(p.NrProd, p.Szerokosc, p.Wysokosc));
                html.AppendLine("<div class='position-info'>");
                html.AppendLine($"<h3>Pozycja {H(p.Poz)} / {H(p.NrProd)}</h3>");
                html.AppendLine($"<p><b>Ilość:</b> {p.Szt} szt.</p>");
                html.AppendLine($"<p><b>Wymiary:</b> {p.Szerokosc} x {p.Wysokosc} mm</p>");
                html.AppendLine($"<p><b>Opis:</b> {H(p.Oznaczenie)}</p>");
                html.AppendLine($"<p><b>Uwagi:</b> {H(p.UwagiPozycji)}</p>");
                html.AppendLine("</div></section>");
            }

            html.AppendLine(KoniecHtml());
            File.WriteAllText(path, html.ToString(), Encoding.UTF8);
            return path;
        }

        public string UtworzHtmlOfertyZBazy(int idZlecenia)
        {
            DataRow header = PobierzNaglowek(idZlecenia);
            DataTable pozycje = PobierzPozycje(idZlecenia);

            string numer = header["NumerZlecenia"].ToString() ?? idZlecenia.ToString();
            string path = PrzygotujSciezke(numer);
            string logoBase64 = PobierzLogoBase64();
            decimal sumaNetto = 0;

            foreach (DataRow p in pozycje.Rows)
            {
                sumaNetto += ToDecimal(p["WartoscNetto"]);
            }

            decimal vat = ToDecimal(header["StawkaVat"]);
            if (vat <= 0) vat = 0.23m;
            decimal brutto = sumaNetto * (1 + vat);
            string waluta = header["Waluta"].ToString() ?? "PLN";

            var html = new StringBuilder();
            html.AppendLine(PoczatekHtml("Oferta " + numer));

            html.AppendLine("<section class='topbar'>");
            if (!string.IsNullOrEmpty(logoBase64))
            {
                html.AppendLine($"<img class='logo' src='data:image/png;base64,{logoBase64}' alt='FENSTER'/>");
            }
            html.AppendLine("<div><div class='brand'>FENSTER 1.0</div><div class='muted'>Konfigurator stolarki okiennej</div></div>");
            html.AppendLine("</section>");

            html.AppendLine("<section class='hero'>");
            html.AppendLine("<div>");
            html.AppendLine($"<div class='eyebrow'>{H(header["StatusZlecenia"])} / {H(header["StatusTechniczny"])}</div>");
            html.AppendLine($"<h1>Oferta nr {H(numer)}</h1>");
            html.AppendLine($"<p>Dokument wygenerowany: {DateTime.Now:dd.MM.yyyy HH:mm}</p>");
            html.AppendLine("</div>");
            html.AppendLine($"<div class='stamp'>{H(header["Obszar"])}<br/><span>{H(waluta)}</span></div>");
            html.AppendLine("</section>");

            html.AppendLine("<section class='grid2'>");
            html.AppendLine("<div class='panel'><h2>Dane klienta</h2>");
            html.AppendLine($"<p><b>{H(header["NazwaKlienta"])}</b></p>");
            html.AppendLine($"<p>NIP: {H(header["NIP"])}</p>");
            html.AppendLine($"<p>Adres: {H(header["Adres"])} {H(header["KodPocztowy"])}</p>");
            html.AppendLine($"<p>Kraj: {H(header["Kraj"])}</p>");
            html.AppendLine($"<p>Telefon: {H(header["Telefon"])}</p>");
            html.AppendLine("</div>");

            html.AppendLine("<div class='panel'><h2>Dane oferty</h2>");
            html.AppendLine($"<p><b>Handlowiec:</b> {H(header["Wprowadzil"])}</p>");
            html.AppendLine($"<p><b>Data wprowadzenia:</b> {Data(header["DataWprowadzenia"])}</p>");
            html.AppendLine($"<p><b>Termin preferowany:</b> {Data(header["TerminPreferowany"])}</p>");
            html.AppendLine($"<p><b>Referencja:</b> {H(header["Referencja"])}</p>");
            html.AppendLine($"<p><b>Produkcja:</b> {H(header["StatusProdukcji"])}</p>");
            html.AppendLine("</div>");
            html.AppendLine("</section>");

            html.AppendLine("<section class='intro'>");
            html.AppendLine("<h2>Zakres oferty</h2>");
            html.AppendLine("<p>Oferta obejmuje skonfigurowane pozycje stolarki wraz z wybranymi pakietami szybowymi, kolorem profili, okuciami oraz dodatkami technicznymi.</p>");
            html.AppendLine($"<p class='note'>{H(header["StatusTechniczny"])}. Status dokumentu: {H(header["StatusZlecenia"])}.</p>");
            html.AppendLine("</section>");

            html.AppendLine("<h2>Pozycje z podglądem technicznym</h2>");

            foreach (DataRow p in pozycje.Rows)
            {
                decimal wartosc = ToDecimal(p["WartoscNetto"]);
                html.AppendLine("<section class='position-card'>");
                html.AppendLine(ZbudujMiniaturePozycji(p["NrProdukcyjny"].ToString() ?? "", ToInt(p["Szerokosc"]), ToInt(p["Wysokosc"])));
                html.AppendLine("<div class='position-info'>");
                html.AppendLine($"<h3>Pozycja {H(p["Poz"])} / {H(p["NrProdukcyjny"])}</h3>");
                html.AppendLine("<div class='position-grid'>");
                html.AppendLine($"<span>Ilość</span><b>{H(p["Sztuk"])} szt.</b>");
                html.AppendLine($"<span>Wymiary</span><b>{H(p["Szerokosc"])} x {H(p["Wysokosc"])} mm</b>");
                html.AppendLine($"<span>Status techniczny</span><b>{StatusTechnicznyPozycji(p["StatusTechniczny"])}</b>");
                html.AppendLine($"<span>Wartość netto</span><b>{Kwota(wartosc, waluta)}</b>");
                html.AppendLine("</div>");
                html.AppendLine($"<p class='desc'>{H(p["Uwagi"])}</p>");
                html.AppendLine("</div></section>");
            }

            html.AppendLine("<section class='summary'>");
            html.AppendLine("<div></div>");
            html.AppendLine("<div class='totals'>");
            html.AppendLine($"<div><span>Suma netto</span><b>{Kwota(sumaNetto, waluta)}</b></div>");
            html.AppendLine($"<div><span>VAT</span><b>{vat:P0}</b></div>");
            html.AppendLine($"<div class='grand'><span>Razem brutto</span><b>{Kwota(brutto, waluta)}</b></div>");
            html.AppendLine("</div>");
            html.AppendLine("</section>");

            html.AppendLine("<section class='footer-note'>");
            html.AppendLine("<p>Wydruk ma charakter ofertowy. Ostateczne warunki realizacji zależą od potwierdzenia technicznego i aktualnych parametrów produkcyjnych.</p>");
            html.AppendLine("</section>");

            html.AppendLine(KoniecHtml());
            File.WriteAllText(path, html.ToString(), Encoding.UTF8);
            return path;
        }

        private string ZbudujMiniaturePozycji(string kodProduktu, int szerokosc, int wysokosc)
        {
            string? base64 = PobierzGrafikeBase64(kodProduktu);
            string img = string.IsNullOrWhiteSpace(base64)
                ? "<div class='fallback-frame'><div></div></div>"
                : $"<img class='position-img' src='data:image/png;base64,{base64}' alt='{H(kodProduktu)}'/>";

            string szer = szerokosc > 0 ? $"{szerokosc} mm" : "0 mm";
            string wys = wysokosc > 0 ? $"{wysokosc} mm" : "0 mm";

            return $@"
<div class='position-visual'>
    <div class='image-wrap'>{img}</div>
    <div class='dim dim-w'><span>{H(szer)}</span></div>
    <div class='dim dim-h'><span>{H(wys)}</span></div>
</div>";
        }

        private DataRow PobierzNaglowek(int idZlecenia)
        {
            using var cn = new SqlConnection(_connString);
            cn.Open();
            using var cmd = new SqlCommand(@"
                SELECT TOP 1 z.IdZlecenia, z.NumerZlecenia, z.DataWprowadzenia, z.TerminPreferowany,
                       z.Obszar, z.TypZlecenia, z.StatusZlecenia, z.Referencja,
                       ISNULL(z.StatusProdukcji, N'Nie rozpoczęto') AS StatusProdukcji,
                       ISNULL(k.NazwaKlienta, N'Brak klienta') AS NazwaKlienta,
                       ISNULL(k.NIP, N'') AS NIP,
                       ISNULL(k.Adres, N'') AS Adres,
                       ISNULL(k.KodPocztowy, N'') AS KodPocztowy,
                       ISNULL(k.Kraj, z.Obszar) AS Kraj,
                       ISNULL(k.Telefon, N'') AS Telefon,
                       u.Login AS Wprowadzil,
                       ISNULL(pf.Waluta, N'PLN') AS Waluta,
                       ISNULL(pf.StawkaVat, 0.23) AS StawkaVat,
                       CASE
                           WHEN EXISTS (SELECT 1 FROM PozycjeZlecenia p WHERE p.IdZlecenia = z.IdZlecenia AND ISNULL(p.StatusTechniczny, 1) = 0)
                               THEN N'Wymaga akceptacji technologa'
                           WHEN EXISTS (SELECT 1 FROM PozycjeZlecenia p WHERE p.IdZlecenia = z.IdZlecenia AND ISNULL(p.StatusTechniczny, 1) = 2)
                               THEN N'Zaakceptowane odstępstwo techniczne'
                           ELSE N'Parametry techniczne poprawne'
                       END AS StatusTechniczny
                FROM Zlecenia z
                LEFT JOIN Klienci k ON z.IdKlienta = k.IdKlienta
                JOIN Uzytkownicy u ON z.IdUzytkownika = u.IdUzytkownika
                LEFT JOIN ParametryFinansowe pf ON pf.Kraj = z.Obszar
                WHERE z.IdZlecenia = @id", cn);
            cmd.Parameters.AddWithValue("@id", idZlecenia);
            using var dr = cmd.ExecuteReader();
            var dt = new DataTable();
            dt.Load(dr);
            if (dt.Rows.Count == 0)
            {
                throw new InvalidOperationException("Nie znaleziono dokumentu w bazie.");
            }
            return dt.Rows[0];
        }

        private DataTable PobierzPozycje(int idZlecenia)
        {
            using var cn = new SqlConnection(_connString);
            cn.Open();
            using var cmd = new SqlCommand(@"
                SELECT
                    CASE
                        WHEN ParentId IS NULL THEN CONVERT(NVARCHAR(20), ROW_NUMBER() OVER (ORDER BY ISNULL(SortOrder, IdPozycji), IdPozycji))
                        ELSE CONVERT(NVARCHAR(20), ParentId)
                    END AS Poz,
                    NrProdukcyjny, Sztuk, Szerokosc, Wysokosc,
                    ISNULL(Uwagi, N'') AS Uwagi,
                    ISNULL(StatusTechniczny, 1) AS StatusTechniczny,
                    CAST(ISNULL(CenaJednostkowa, 0) * ISNULL(Sztuk, 1) AS DECIMAL(18,2)) AS WartoscNetto
                FROM PozycjeZlecenia
                WHERE IdZlecenia = @id
                ORDER BY ISNULL(SortOrder, IdPozycji), IdPozycji", cn);
            cmd.Parameters.AddWithValue("@id", idZlecenia);
            using var dr = cmd.ExecuteReader();
            var dt = new DataTable();
            dt.Load(dr);
            return dt;
        }

        private string PoczatekHtml(string title)
        {
            return $@"<!doctype html>
<html lang='pl'>
<head>
<meta charset='utf-8'>
<title>{H(title)}</title>
<style>
*{{box-sizing:border-box}}
body{{font-family:'Segoe UI',Arial,sans-serif;margin:0;background:#eef2f5;color:#1f2933}}
.page{{width:1120px;margin:28px auto;background:#fff;box-shadow:0 18px 50px rgba(31,41,51,.12);padding:34px 42px 42px}}
.topbar{{display:flex;align-items:center;gap:14px;border-bottom:1px solid #d8dee6;padding-bottom:18px;margin-bottom:22px}}
.logo{{width:54px;height:54px;object-fit:contain}}
.brand{{font-size:24px;font-weight:800;letter-spacing:.3px}}
.muted{{color:#687586;font-size:13px}}
.hero{{display:flex;justify-content:space-between;align-items:flex-start;background:#183447;color:white;padding:28px 30px;margin:0 0 24px}}
.hero h1{{margin:6px 0 8px;font-size:34px}}
.hero p{{margin:0;color:#d6e3ea}}
.eyebrow{{font-size:12px;text-transform:uppercase;letter-spacing:.12em;color:#9dd6ff;font-weight:700}}
.stamp{{border:1px solid rgba(255,255,255,.45);padding:14px 18px;text-align:right;font-size:18px;font-weight:700;min-width:170px}}
.stamp span{{font-size:13px;color:#d6e3ea}}
.grid2{{display:grid;grid-template-columns:1fr 1fr;gap:18px;margin-bottom:22px}}
.panel,.intro{{border:1px solid #d8dee6;padding:18px;background:#fbfcfd;margin-bottom:22px}}
h2{{font-size:17px;margin:0 0 12px;color:#183447}}
h3{{font-size:16px;margin:0 0 12px;color:#183447}}
p{{margin:5px 0;line-height:1.45}}
.note{{padding:10px 12px;background:#fff7d6;border-left:4px solid #f1b600}}
.position-card{{display:grid;grid-template-columns:360px 1fr;gap:22px;align-items:center;border:1px solid #d8dee6;background:#fff;margin:14px 0;padding:16px;page-break-inside:avoid}}
.position-visual{{position:relative;height:245px;background:linear-gradient(#fbfdff,#eef4f8);border:1px solid #d8dee6;overflow:hidden}}
.image-wrap{{position:absolute;left:20px;right:62px;top:18px;bottom:44px;display:flex;align-items:center;justify-content:center}}
.position-img{{max-width:100%;max-height:100%;object-fit:contain;filter:drop-shadow(0 6px 7px rgba(31,41,51,.16))}}
.fallback-frame{{width:160px;height:130px;border:12px solid #f2f4f7;box-shadow:inset 0 0 0 2px #c8d0d8}}
.fallback-frame div{{height:100%;background:#c8ddeb}}
.dim{{position:absolute;color:#4f5b66;font-size:12px;font-weight:700}}
.dim::before{{content:'';position:absolute;background:#8a97a3}}
.dim::after{{content:'';position:absolute;border:5px solid transparent}}
.dim-w{{left:34px;right:78px;bottom:18px;text-align:center;border-top:1px solid #8a97a3}}
.dim-w span{{position:relative;top:-11px;background:#fff;padding:2px 8px;border:1px solid #d8dee6}}
.dim-w::after{{left:0;top:-5px;border-right-color:#8a97a3}}
.dim-w::before{{right:0;top:-1px;width:0;height:0}}
.dim-h{{right:14px;top:24px;bottom:54px;width:42px;border-left:1px solid #8a97a3;display:flex;align-items:center;justify-content:center}}
.dim-h span{{background:#fff;padding:2px 7px;border:1px solid #d8dee6;white-space:nowrap}}
.position-grid{{display:grid;grid-template-columns:150px 1fr;gap:6px 12px;margin-bottom:10px}}
.position-grid span{{color:#687586}}
.desc{{padding-top:8px;border-top:1px solid #edf2f7;color:#3b4652}}
.money{{text-align:right;white-space:nowrap}}
.summary{{display:grid;grid-template-columns:1fr 360px;margin-top:20px}}
.totals{{border:1px solid #d8dee6}}
.totals div{{display:flex;justify-content:space-between;padding:10px 14px;border-bottom:1px solid #d8dee6}}
.totals div:last-child{{border-bottom:0}}
.grand{{font-size:18px;background:#183447;color:white}}
.footer-note{{margin-top:22px;color:#687586;font-size:12px;border-top:1px solid #d8dee6;padding-top:14px}}
@media print{{body{{background:white}}.page{{box-shadow:none;margin:0;width:auto}}.position-card{{break-inside:avoid}}}}
</style>
</head>
<body><main class='page'>";
        }

        private string KoniecHtml() => "</main></body></html>";

        private string PobierzLogoBase64()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Ikony", "logo.png");
            if (!File.Exists(path))
            {
                path = Path.Combine(Directory.GetCurrentDirectory(), "Ikony", "logo.png");
            }
            return File.Exists(path) ? Convert.ToBase64String(File.ReadAllBytes(path)) : "";
        }

        private string? PobierzGrafikeBase64(string kodProduktu)
        {
            string? rel = PobierzSciezkeGrafiki(kodProduktu);
            if (string.IsNullOrWhiteSpace(rel)) return null;

            string path = ZnajdzPlik(rel);
            return File.Exists(path) ? Convert.ToBase64String(File.ReadAllBytes(path)) : null;
        }

        private string? PobierzSciezkeGrafiki(string kodProduktu)
        {
            try
            {
                using var cn = new SqlConnection(_connString);
                cn.Open();
                using var cmd = new SqlCommand(@"
                    SELECT TOP 1 SciezkaPliku
                    FROM GrafikiKonstrukcji
                    WHERE KodProduktu = @kod
                      AND CzyAktywna = 1
                      AND SciezkaPliku IS NOT NULL
                    ORDER BY CASE WHEN KolorProfilu = 'DEFAULT' THEN 0 ELSE 1 END", cn);
                cmd.Parameters.AddWithValue("@kod", kodProduktu);
                object? result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? null : result.ToString();
            }
            catch
            {
                return null;
            }
        }

        private string ZnajdzPlik(string sciezkaZBazy)
        {
            string rel = sciezkaZBazy.Replace('/', Path.DirectorySeparatorChar);
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string desktopProject = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "MP_Fenster_Software", "MP_Fenster_App");

            string[] kandydaci =
            {
                Path.Combine(baseDir, rel),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", rel)),
                Path.Combine(desktopProject, rel)
            };

            foreach (string path in kandydaci)
            {
                if (File.Exists(path)) return path;
            }

            return "";
        }

        private string PrzygotujSciezke(string numer)
        {
            string safe = string.Join("_", (numer ?? "brak").Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(Path.GetTempPath(), $"Oferta_Fenster_{safe}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
        }

        private static string H(object? value) => WebUtility.HtmlEncode(value?.ToString() ?? "");
        private static string Data(object value) => value == DBNull.Value ? "" : Convert.ToDateTime(value).ToString("dd.MM.yyyy", CultureInfo.CurrentCulture);
        private static int ToInt(object value) => value == DBNull.Value ? 0 : Convert.ToInt32(value);

        private static decimal ToDecimal(object value)
        {
            if (value == DBNull.Value || value == null) return 0;
            if (value is decimal d) return d;
            if (value is int i) return i;
            if (value is long l) return l;
            if (value is double db) return Convert.ToDecimal(db, CultureInfo.InvariantCulture);
            if (value is float f) return Convert.ToDecimal(f, CultureInfo.InvariantCulture);

            string text = value.ToString()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(text)) return 0;
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal current)) return current;
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal invariant)) return invariant;
            if (decimal.TryParse(text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal normalized)) return normalized;
            throw new FormatException($"Nie można odczytać liczby: {text}");
        }

        private static string Kwota(decimal value, string waluta) => $"{value:N2} {waluta}";

        private static string StatusTechnicznyPozycji(object value)
        {
            int status = value == DBNull.Value ? 1 : Convert.ToInt32(value);
            return status switch
            {
                0 => "Wymaga akceptacji",
                2 => "Zaakceptowane odstępstwo",
                _ => "OK"
            };
        }
    }
}
