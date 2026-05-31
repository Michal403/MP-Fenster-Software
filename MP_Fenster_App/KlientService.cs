using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace MP_Fenster_App.Services
{
    public class KlientService
    {
        private readonly string _connString;

        public KlientService(string connString)
        {
            _connString = connString;
        }

        public DataTable PobierzKlientowHandlowca(string login)
        {
            using var cn = new SqlConnection(_connString);
            cn.Open();

            string sql = @"SELECT IdKlienta, NazwaKlienta, NIP, Adres, Telefon
                           FROM Klienci k
                           JOIN Uzytkownicy u ON k.IdUzytkownika = u.IdUzytkownika
                           WHERE UPPER(u.Login) = @login
                           ORDER BY NazwaKlienta";

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@login", login.ToUpper());

            using var dr = cmd.ExecuteReader();
            var table = new DataTable();
            table.Load(dr);
            return table;
        }
    }
}