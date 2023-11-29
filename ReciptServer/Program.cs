//using Data;
using System.Data;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;
using MySql.Data;
using MySql.Data.MySqlClient;
using ReciptServer;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Abstractions;
//https://stackoverflow.com/questions/49035178/unable-to-locate-system-data-sqlclient-reference

namespace sqltest
{
    class Program
    {
        
        static void Main(string[] args)
        {
            /*
            //DataBase
            DataBase db = new DataBase("world");
            string query = "use world; SELECT * FROM city; ";

            foreach (DataRow row in db.Query(query))
            {
                Console.WriteLine(row[0]+": "+row[1]);
            }

            db.Close();
            */

            //HTTP Server
            Dictionary<string, HttpRequest> events = new();
            events.Add("/test", (JObject? body) => "The server is running");

            events.Add("/form", (JObject? body) =>
            {
                if (body == null) return "No body.";
                if(body["Metadata"] != null) { return body["Metadata"]!.ToString(); }
                return "No metadata";
            });

            events.Add("/log_receipt", (JObject? body) =>
            {
                //Deserialize
                if (body == null) 
                    return "Body not found";
                if (body["Receipt"] == null) 
                    return "Receipt object not found";
                string receiptString = body["Receipt"]!.ToString();
                Receipt? recipt = JsonConvert.DeserializeObject<Receipt>(receiptString);
                if (recipt == null) 
                    return "";

                //Do something with the reciept
                Console.WriteLine("Recipt read sucessfully!");
                Console.WriteLine("Recipt at store: "+recipt.StoreName);

                return "";
            });

            HttpServer server = new HttpServer(events);

        }
    }
}