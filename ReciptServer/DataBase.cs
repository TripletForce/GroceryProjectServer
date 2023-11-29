﻿using System;
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
        private const string UserName = "Recipt_DB";
        private const string Password = "4g88wnq2xtkk";
        public MySqlConnection Connection;

        public DataBase(string DatabaseName, bool local = true) 
        {
            string Server =  LocalServer;
            if (String.IsNullOrEmpty(Server)) throw new Exception("No Server Name");
            string connstring = string.Format("Server={0}; database={1}; UID={2}; password={3}", Server, DatabaseName, UserName, Password);
            Connection = new MySqlConnection(connstring);
            Connection.Open();
        }

        public DataTable Query(string query)
        {
            var cmd = new MySqlCommand(query, Connection);
            var reader = cmd.ExecuteReader();

            DataTable dt = new DataTable();
            return dt;
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
