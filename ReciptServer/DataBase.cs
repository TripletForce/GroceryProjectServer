using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

//using Data;
using MySql.Data.MySqlClient;


namespace ReciptServer
{
    public class DataBase
    {
        private const string LocalServer = "localhost";
        private const string RemoteServer = "<IP Address>";
        private const string UserName = "Recipt_DB";
        private const string Password = "4g88wnq2xtkk";
        private MySqlConnection Connection;

        public DataBase(string DatabaseName, bool local = true) 
        {
            string Server = local ? LocalServer : RemoteServer;
            if (String.IsNullOrEmpty(Server)) throw new Exception("No Server Name");
            string connstring = string.Format("Server={0}; database={1}; UID={2}; password={3}", Server, DatabaseName, UserName, Password);
            Connection = new MySqlConnection(connstring);
            Connection.Open();
        }

        public IEnumerable<DataRow> Query(string query)
        {
            var cmd = new MySqlCommand(query, Connection);
            var reader = cmd.ExecuteReader();

            DataTable dt = new DataTable();
            dt.Load(reader);
            int numberOfResults = dt.Rows.Count;

            foreach (DataRow dr in dt.Rows)
            {
                yield return dr;
            }
        }

        public void Close()
        {
            Connection.Close();
        }
    }
}
