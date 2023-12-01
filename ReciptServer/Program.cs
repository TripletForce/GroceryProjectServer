using System.Data;
using MySql.Data.MySqlClient;
using ReciptServer;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.Bcpg;

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
                Receipt? receipt = JsonConvert.DeserializeObject<Receipt>(receiptString);
                if (receipt == null) 
                    return "";
                
                //UserId
                if (body["UserId"] == null)
                    return "UserId object not found";
                string userId = body["UserId"]!.ToString();

                Console.WriteLine("Recipt read sucessfully!");
                Console.WriteLine("Recipt at store: "+receipt.StoreName);

                //Insert store
                MySqlCommand comm = db.Connection.CreateCommand();
                //comm.CommandText = "INSERT INTO Stores(Name, State, City, PostalCode, Address) VALUES (?name, ?state, ?city, ?postal, ?address);";

                comm.CommandText = "INSERT INTO Stores(Name, State, City, PostalCode, Address) VALUES(?name, ?state, ?city, ?postal, ?address) " +
                    "ON DUPLICATE KEY UPDATE " +
                    "Name = ?name, State = ?state, City = ?city, PostalCode = ?postal, Address = ?address;";

                comm.Parameters.Add("?name", MySqlDbType.VarChar).Value = receipt.StoreName;
                comm.Parameters.Add("?state", MySqlDbType.VarChar).Value = receipt.State;
                comm.Parameters.Add("?city", MySqlDbType.VarChar).Value = receipt.City;
                comm.Parameters.Add("?postal", MySqlDbType.VarChar).Value = receipt.PostalCode;
                comm.Parameters.Add("?address", MySqlDbType.VarChar).Value = receipt.Street;

                comm.ExecuteNonQuery();

                //Get the store id
                int storeId = -1;
                foreach (DataRow row in db.Query("SELECT StoreId FROM tracker.stores WHERE Address = ?address", new () { ("?address", MySqlDbType.VarChar, receipt.Street) })) 
                    storeId = int.Parse(row[0].ToString());

                //Insert recipt
                comm = db.Connection.CreateCommand();
                comm.CommandText = "INSERT INTO Receipts(StoreId, UserId, ReceiptDate, Subtotal, Tax, Total, PhoneNumber, PaymentType) VALUE (?store_id, ?user, ?date, ?sub_total, ?tax, ?total, ?phone, ?payment);";

                comm.Parameters.Add("?user", MySqlDbType.VarChar).Value = userId;
                comm.Parameters.Add("?phone", MySqlDbType.VarChar).Value = receipt.PhoneNumber;
                comm.Parameters.Add("?sub_total", MySqlDbType.Decimal).Value = receipt.SubTotal;
                comm.Parameters.Add("?total", MySqlDbType.Decimal).Value = receipt.Total;
                comm.Parameters.Add("?tax", MySqlDbType.Decimal).Value = receipt.Tax1;
                comm.Parameters.Add("?payment", MySqlDbType.Enum).Value = receipt.PaymentType;
                comm.Parameters.Add("?store_id", MySqlDbType.Int64).Value = storeId;
                comm.Parameters.Add("?date", MySqlDbType.DateTime).Value = receipt.ReceiptDate;

                comm.ExecuteNonQuery();

                //Get the id
                int reciptId = -1;
                foreach (DataRow row in db.Query("SELECT MAX(ReceiptId) FROM tracker.receipts", new() { })) reciptId = int.Parse(row[0].ToString());

                //Insert the Items
                foreach(PurchasedItem item in receipt.PurchasedItems)
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
            events.Add("/insert_user", (JObject? body) =>
            {
                //Needs Database
                if (db == null) return "No Database";

                //Deserialize
                if (body == null) return "Body not found";
                //User
                if (body["email"] == null) return "User object not found";
                //Password
                if (body["password"] == null) return "Password object not found";

                //If username found, grab the id
                foreach(DataRow dr in db.Query("SELECT UserId FROM tracker.users WHERE Email = ?email && Password = ?password", new() { 
                    ("?email", MySqlDbType.VarChar, body["email"].ToString() ),
                    ("?password", MySqlDbType.VarChar, body["password"].ToString() )
                }))
                {
                    return dr[0].ToString();
                }

                try
                {
                    //Insert into Database
                    MySqlCommand comm = db.Connection.CreateCommand();
                    comm.CommandText = "INSERT INTO Users(Email, Password) VALUES (?email, ?password);";
                    comm.Parameters.Add("?email", MySqlDbType.VarChar).Value = body["email"]!.ToString();
                    comm.Parameters.Add("?password", MySqlDbType.VarChar).Value = body["password"]!.ToString();
                    comm.ExecuteNonQuery();

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

            //Brings a list of all the items back with the total that people payed for that Item
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

            //Number of Items
            events.Add("/all_items_from_user", (JObject? body) =>
            {
                //Needs Database
                if (db == null) return "No Database";

                //User Id
                if (body["UserId"] == null)
                    return "UserId object not found";
                string userId = body["UserId"]!.ToString();

                List<(string, decimal)> items = new();
                foreach (DataRow row in db.Query(@"
                        SELECT Name,
	                        SUM(Price*Quantity) AS Spent
                        FROM tracker.items I
	                        INNER JOIN tracker.receipts R ON I.ReceiptId = R.ReceiptId
                        WHERE R.UserId = ?user_id
                        GROUP BY Name
                        ORDER BY Spent DESC;
                    ", new() { ("?user_id", MySqlDbType.VarChar, userId) }))
                {
                    items.Add((row[0].ToString(), Convert.ToDecimal(row[1].ToString())));
                }
                string response = JsonConvert.SerializeObject(items);
                return response;
            });

            //Brings back all of the users 
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

            //Number of Items
            events.Add("/count_items", (JObject? body) =>
            {
                //Needs Database
                if (db == null) return "No Database";

                int result = -1;
                foreach (DataRow row in db.Query("SELECT COUNT(*) FROM tracker.items;", new() { }))
                {
                    result = int.Parse(row[0].ToString());
                }

                string response = JsonConvert.SerializeObject(result);
                return response;
            });

            //Number of Items
            events.Add("/count_users", (JObject? body) =>
            {
                //Needs Database
                if (db == null) return "No Database";

                int result = -1;
                foreach (DataRow row in db.Query("SELECT COUNT(*) FROM tracker.users;", new() { }))
                {
                    result = int.Parse(row[0].ToString());
                }

                string response = JsonConvert.SerializeObject(result);
                return response;
            });
            
            //Number of Items
            events.Add("/count_receipts", (JObject? body) =>
            {
                //Needs Database
                if (db == null) return "No Database";

                int result = -1;
                foreach (DataRow row in db.Query("SELECT COUNT(*) FROM tracker.receipts;", new() { }))
                {
                    result = int.Parse(row[0].ToString());
                }

                string response = JsonConvert.SerializeObject(result);
                return response;
            });

            //Number of Items
            events.Add("/count_spent", (JObject? body) =>
            {
                //Needs Database
                if (db == null) return "No Database";

                decimal result = -1;
                foreach (DataRow row in db.Query("SELECT SUM(Total) AS Spent FROM tracker.receipts;", new() { }))
                {
                    result = decimal.Parse(row[0].ToString());
                }

                string response = JsonConvert.SerializeObject(result);
                return response;
            });

            //Brings back items rank by money spent on item
            events.Add("/rank_items", (JObject? body) =>
            {
                //Needs Database
                if (db == null) return "No Database";

                List<(string, decimal)> items = new();
                foreach (DataRow row in db.Query("SELECT Name, SUM(Price * Quantity) AS Spent FROM tracker.items GROUP BY Name ORDER BY Spent DESC", new() { }))
                {
                    items.Add((row[0].ToString(), Convert.ToDecimal(row[1].ToString())));
                }
                string response = JsonConvert.SerializeObject(items);
                return response;
            });
            
            //Brings back stores rank by money spent on store
            events.Add("/rank_stores", (JObject? body) =>
            {
                //Needs Database
                if (db == null) return "No Database";

                //Name, street, phone, money spent
                List<(string, string, string, decimal)> items = new();
                foreach (DataRow row in db.Query(@"
                    SELECT S.Name,
	                    S.Address,
                        MIN(R.PhoneNumber) AS Phone,
                        COALESCE(SUM(I.Price*I.Quantity), 0) AS Spent
                    FROM tracker.stores S
	                    LEFT JOIN tracker.receipts R ON R.StoreId = S.StoreId
	                    LEFT JOIN tracker.items I ON I.ReceiptId = R.ReceiptId
                    GROUP BY S.Name, S.Address
                    ORDER BY Spent DESC;
                ", new() { }))
                {
                    items.Add((row[0].ToString(), row[1].ToString(), row[2].ToString(), Convert.ToDecimal(row[3].ToString())));
                }
                string response = JsonConvert.SerializeObject(items);
                return response;
            });

            //Brings back users rank by money spent by user
            events.Add("/rank_user", (JObject? body) =>
            {
                //Needs Database
                if (db == null) return "No Database";

                List<(string, decimal)> items = new();
                foreach (DataRow row in db.Query(@"
                    SELECT U.Email,
	                    COALESCE(SUM(I.Price*I.Quantity), 0) AS Spent
                    FROM tracker.users U
	                    LEFT JOIN tracker.receipts R ON R.UserId = U.UserId
	                    LEFT JOIN tracker.items I ON I.ReceiptId = R.ReceiptId
                    GROUP BY U.Email
                    ORDER BY Spent DESC;
                ", new() { }))
                {
                    items.Add((row[0].ToString(), Convert.ToDecimal(row[1].ToString())));
                }
                string response = JsonConvert.SerializeObject(items);
                return response;
            });

            //User information
            events.Add("/user_information", (JObject? body) =>
            {
                //Needs Database
                if (db == null) return "No Database";
                //UserId
                if (body["UserId"] == null)
                    return "UserId object not found";
                string userId = body["UserId"]!.ToString();

                //UserId, Email, JoinDate, Spent, UploadedRecipts
                List<(string, string, string, decimal, int)> result = new();
                foreach (DataRow row in db.Query(@"
                    SELECT U.UserId,
	                    U.Email,
	                    DATE(U.JoinDate) AS JoinDate,
	                    COALESCE(SUM(I.Price*I.Quantity), 0) AS Spent,
	                    COUNT(DISTINCT R.ReceiptId) AS UploadedRecipts
                    FROM tracker.users U
	                    LEFT JOIN tracker.receipts R ON R.UserId = U.UserId
	                    LEFT JOIN tracker.items I ON I.ReceiptId = R.ReceiptId
                    WHERE U.UserId = ?user_id
                    GROUP BY U.Email, U.JoinDate;
                ", new() {( "?user_id", MySqlDbType.VarChar, userId )}))
                {
                    result.Add(new
                    (
                        row[0].ToString()!, //UserId
                        row[1].ToString()!, //Email 
                        row[2].ToString()!.Substring(0, 11), //JoinDate
                        Convert.ToDecimal(row[3].ToString())!, // TotalSpent
                        
                        int.Parse(row[4].ToString()!) //Recipts
                    ));
                }
                string response = JsonConvert.SerializeObject(result);
                return response;
            });

            //Purchased Items from user
            events.Add("/item_dates_from_user", (JObject? body) =>
            {
                //Needs Database
                if (db == null) return "No Database";

                //User Id
                if (body["UserId"] == null)
                    return "UserId object not found";
                string userId = body["UserId"]!.ToString();

                List<(string, decimal, DateTime)> items = new();
                foreach (DataRow row in db.Query(@"
                        SELECT Name,
	                        SUM(Price*Quantity) AS Spent,
                            ReceiptDate
                        FROM tracker.items I
	                        INNER JOIN tracker.receipts R ON I.ReceiptId = R.ReceiptId
                        WHERE R.UserId = ?user_id
                        GROUP BY Name, ReceiptDate
                        ORDER BY ReceiptDate ASC;
                    ", new() { ("?user_id", MySqlDbType.VarChar, userId) }))
                {
                    items.Add((row[0].ToString(), Convert.ToDecimal(row[1].ToString()), Convert.ToDateTime(row[2].ToString())));
                }
                string response = JsonConvert.SerializeObject(items);
                return response;
            });

            //Purcases from user of date
            events.Add("/user_purchases", (JObject? body) =>
            {
                //Needs Database
                if (db == null) return "No Database";

                //User Id
                if (body["UserId"] == null)
                    return "UserId object not found";
                string userId = body["UserId"]!.ToString();

                List<(string, decimal, DateTime)> items = new();
                foreach (DataRow row in db.Query(@"
                        SELECT ReceiptId,
	                        Total,
	                        ReceiptDate
                        FROM tracker.receipts
                        WHERE UserId = ?user_id
                        ORDER BY ReceiptDate;
                    ", new() { ("?user_id", MySqlDbType.VarChar, userId) }))
                {
                    items.Add((row[0].ToString(), Convert.ToDecimal(row[1].ToString()), Convert.ToDateTime(row[2].ToString())));
                }
                string response = JsonConvert.SerializeObject(items);
                return response;
            });


            //Start the server
            HttpServer server = new HttpServer(events);

            //Close the database after server closes
            if(db != null) db.Close();
        }
    }
}