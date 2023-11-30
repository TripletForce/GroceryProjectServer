using System.Data;
using MySql.Data.MySqlClient;
using ReciptServer;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

//using Data;
using MySql.Data.MySqlClient;
using System.Runtime.InteropServices;

namespace ReciptServer
{
    public class DataBase
    {
        private const string LocalServer = "localhost";
        private const string UserName = "Recipt_DB";
        private const string Password = "4g88wnq2xtkk";
        public MySqlConnection Connection;

        public DataBase(string DatabaseName, bool local = true)
        {
            string Server = LocalServer;
            if (String.IsNullOrEmpty(Server)) throw new Exception("No Server Name");
            string connstring = string.Format("Server={0}; database={1}; UID={2}; password={3}", Server, DatabaseName, UserName, Password);
            Connection = new MySqlConnection(connstring);
            Connection.Open();
        }

        public IEnumerable<DataRow> Query(string query , List<(string, MySqlDbType, string)> paramValues)
        {
            var cmd = new MySqlCommand(query, Connection);

            foreach ((string, MySqlDbType, string) paramValue in paramValues)
            {
                cmd.Parameters.Add(paramValue.Item1, paramValue.Item2).Value = paramValue.Item3;
            }

            var reader = cmd.ExecuteReader();

            DataTable dt = new DataTable();
            dt.Load(reader);
            int numberOfResults = dt.Rows.Count;

            foreach (DataRow dr in dt.Rows)
            {
                if (dr == null) break;
                yield return dr;
            }
        }

        /*
        public 
        */
        public void Close()
        {
            Connection.Close();
        }
        
    }
}
