using System.Data;
using MySql.Data.MySqlClient;
using ReciptServer;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;


namespace sqltest
{
    class Program
    {
        static void Main(string[] args)
        {
        

            //Local DataBase 
            DataBase? db;
            try 
            {
                Console.WriteLine("___Trying to Connect to Local Database___");
                db = new DataBase("tracker", true);
                Console.WriteLine("___Database Connected___");
            }
            catch (Exception ex)
            {
                Console.WriteLine("___Unable to connect to Local Database___");
                Console.WriteLine(ex.Message);
                db = null;
            }

            //HTTP Server
            Dictionary<string, HttpRequest> events = new();

            //Status of Server
            events.Add("/test", (JObject? body) => "The server is running");

            //Status of Database
            events.Add("/db_status", (JObject? body) => null == db ? "Could not find Database" : "Found Database");

            //Example Form: retuns Metadata value as string
            events.Add("/form", (JObject? body) =>
            {
                if (body == null) return "No body.";
                if(body["Metadata"] != null) { return body["Metadata"]!.ToString(); }
                return "No Metadata";
            });
            
            //Inserts recipt into database
            events.Add("/log_receipt", (JObject? body) =>
            {
                //Deserialize
                if (body == null) 
                    return "Body not found";
                //Recipt
                if (body["Receipt"] == null) 
                    return "Receipt object not found";
                string receiptString = body["Receipt"]!.ToString();
                Receipt? recipt = JsonConvert.DeserializeObject<Receipt>(receiptString);
                if (recipt == null) 
                    return "";
                
                //UserId
                if (body["UserId"] == null)
                    return "UserId object not found";
                string userId = body["UserId"]!.ToString();

                Console.WriteLine("Recipt read sucessfully!");
                Console.WriteLine("Recipt at store: "+recipt.StoreName);

                //Insert store
                MySqlCommand comm = db.Connection.CreateCommand();
                //comm.CommandText = "INSERT INTO Stores(Name, State, City, PostalCode, Address) VALUES (?name, ?state, ?city, ?postal, ?address);";

                comm.CommandText = "INSERT INTO Stores(Name, State, City, PostalCode, Address) VALUES(?name, ?state, ?city, ?postal, ?address) " +
                    "ON DUPLICATE KEY UPDATE " +
                    "Name = ?name, State = ?state, City = ?city, PostalCode = ?postal, Address = ?address;";

                comm.Parameters.Add("?name", MySqlDbType.VarChar).Value = recipt.StoreName;
                comm.Parameters.Add("?state", MySqlDbType.VarChar).Value = recipt.State;
                comm.Parameters.Add("?city", MySqlDbType.VarChar).Value = recipt.City;
                comm.Parameters.Add("?postal", MySqlDbType.VarChar).Value = recipt.PostalCode;
                comm.Parameters.Add("?address", MySqlDbType.VarChar).Value = recipt.Street;

                comm.ExecuteNonQuery();

                //Get the store id
                int storeId = -1;
                foreach (DataRow row in db.Query("SELECT StoreId FROM tracker.stores WHERE Address = ?address", new () { ("?address", MySqlDbType.VarChar, recipt.Street) })) 
                    storeId = int.Parse(row[0].ToString());

                //Insert recipt
                comm = db.Connection.CreateCommand();
                comm.CommandText = "INSERT INTO Receipts(StoreId, UserId, ReceiptDate, Subtotal, Tax, Total, PhoneNumber, PaymentType) VALUE (?store_id, ?user, CURRENT_TIMESTAMP, ?sub_total, ?tax, ?total, ?phone, ?payment);";

                comm.Parameters.Add("?user", MySqlDbType.VarChar).Value = userId;
                comm.Parameters.Add("?phone", MySqlDbType.VarChar).Value = recipt.PhoneNumber;
                comm.Parameters.Add("?sub_total", MySqlDbType.Decimal).Value = recipt.SubTotal;
                comm.Parameters.Add("?total", MySqlDbType.Decimal).Value = recipt.Total;
                comm.Parameters.Add("?tax", MySqlDbType.Decimal).Value = recipt.Tax1;
                comm.Parameters.Add("?payment", MySqlDbType.Enum).Value = recipt.PaymentType;
                comm.Parameters.Add("?store_id", MySqlDbType.Int64).Value = storeId;

                comm.ExecuteNonQuery();

                //Get the id
                int reciptId = -1;
                foreach (DataRow row in db.Query("SELECT MAX(ReceiptId) FROM tracker.receipts", new() { })) reciptId = int.Parse(row[0].ToString());

                //Insert the Items
                foreach(PurchasedItem item in recipt.PurchasedItems)
                {
                    comm = db.Connection.CreateCommand();
                    comm.CommandText = "INSERT INTO Items(ReceiptId, Name, Price, Quantity) VALUE (?receipt_id, ?name, ?price, ?quantity);";

                    comm.Parameters.Add("?name", MySqlDbType.VarChar).Value = item.Name;
                    comm.Parameters.Add("?price", MySqlDbType.Decimal).Value = item.Price;
                    comm.Parameters.Add("?quantity", MySqlDbType.VarChar).Value = 1; // item.Quantity;
                    comm.Parameters.Add("?receipt_id", MySqlDbType.Int64).Value = reciptId;

                    comm.ExecuteNonQuery();
                }

                return "";
            });

            //Inserts user into database
            events.Add("/update_user", (JObject? body) =>
            {
                //Needs Database
                if (db == null) return "No Database";

                //Deserialize
                if (body == null) return "Body not found";
                //User
                if (body["email"] == null) return "User object not found";
                //Password
                if (body["password"] == null) return "Password object not found";

                //Insert into Database
                MySqlCommand comm = db.Connection.CreateCommand();
                comm.CommandText = "INSERT INTO Users(Email, Password) VALUES (?email, ?password);";
                comm.Parameters.Add("?email", MySqlDbType.VarChar).Value = body["email"]!.ToString();
                comm.Parameters.Add("?password", MySqlDbType.VarChar).Value = body["password"]!.ToString();
                comm.ExecuteNonQuery();

                //Get the id
                try
                {
                    string? id = "-1";
                    foreach (DataRow row in db.Query("SELECT MAX(UserId) FROM tracker.users", new() { })) id = row[0].ToString();
                    if (id == null) return "-1";
                    return id;
                }
                catch
                {
                    return "-1";
                }
            });

            events.Add("/all_items", (JObject? body) =>
            {
                //Needs Database
                if (db == null) return "No Database";

                List<(string, decimal)> items = new(); 
                foreach (DataRow row in db.Query("SELECT Name, SUM(Price*Quantity) AS Spent FROM tracker.items GROUP BY Name;", new() { }))
                {
                    items.Add( (row[0].ToString(), Convert.ToDecimal(row[1].ToString())) );
                }
                string response = JsonConvert.SerializeObject(items);
                return response;
            });

            events.Add("/all_users", (JObject? body) =>
            {
                //Needs Database
                if (db == null) return "No Database";

                List<(string, string)> items = new();
                foreach (DataRow row in db.Query("Select Email, Password FROM tracker.users;", new() { }))
                {
                    items.Add((row[0].ToString(), row[1].ToString()));
                }
                string response = JsonConvert.SerializeObject(items);
                return response;
            });

            //TODO:
            //pie graphs
            //datetime / spent
            //% of stores spent

            //Select

            //Update store
            //update items

            //So I need to make a select statement that returns items group by name with the total cost (quantity and price differ)

            //Ranking all items 

            //Start the server
            HttpServer server = new HttpServer(events);

            //Close the database after server closes
            if(db != null) db.Close();
        }
    }
}