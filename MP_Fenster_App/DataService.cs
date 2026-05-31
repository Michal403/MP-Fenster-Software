using Microsoft.Data.SqlClient;
using System;

namespace MP_Fenster_App
{
    public class DataService
    {
        private readonly string _connString;

        public DataService(string connString)
        {
            _connString = connString;
        }

        public double PobierzCene(string tabela, string kluczKolumna, string wartosc, string cenaKolumna)
        {
            try
            {
                using (var cn = new SqlConnection(_connString))
                {
                    cn.Open();
                    string sql = $"SELECT {cenaKolumna} FROM {tabela} WHERE {kluczKolumna} = @Val";
                    using (var cmd = new SqlCommand(sql, cn))
                    {
                        cmd.Parameters.AddWithValue("@Val", wartosc);
                        var res = cmd.ExecuteScalar();
                        return res != null ? Convert.ToDouble(res) : 0.0;
                    }
                }
            }
            catch { return 0.0; }
        }
    }
}