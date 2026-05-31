using Microsoft.Data.SqlClient;
using System;

namespace MP_Fenster_App.Services
{
    public class ZlecenieService
    {
        private readonly string _connString;

        public ZlecenieService(string connString)
        {
            _connString = connString;
        }

        public int PobierzIdUzytkownika(string login)
        {
            using var cn = new SqlConnection(_connString);
            cn.Open();

            using var cmd = new SqlCommand("SELECT IdUzytkownika FROM Uzytkownicy WHERE UPPER(Login) = @login", cn);
            cmd.Parameters.AddWithValue("@login", login.ToUpper());

            object? result = cmd.ExecuteScalar();
            return result == null ? 1 : Convert.ToInt32(result);
        }

        public int PobierzKolejnyNumerZlecenia()
        {
            using var cn = new SqlConnection(_connString);
            cn.Open();

            using var cmd = new SqlCommand("SELECT ISNULL(MAX(NumerZlecenia), 2999) + 1 FROM Zlecenia", cn);
            object? result = cmd.ExecuteScalar();
            return result == null ? 100 : Convert.ToInt32(result);
        }
    }
}