using MySqlX.XDevAPI.Common;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Bcpg;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReciptServer
{
    public delegate string HttpRequest(JObject? body);

    public class HttpServer
    {
        private static Dictionary<string, HttpRequest> RequestDelegates = new();
        private static HttpListener listener = new HttpListener();
        private const string URL = "http://localhost:8000/";

        public static async Task HandleIncomingConnections()
        {
            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (true)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // Print out some info about the request
                Console.WriteLine("___Request___");
                Console.WriteLine(req.Url!.ToString());
                Console.WriteLine(req.HttpMethod);
                Console.WriteLine(req.UserHostName);
                Console.WriteLine(req.UserAgent);

                // Read the request body
                string requestBody;
                using (Stream body = req.InputStream)
                {
                    using (StreamReader reader = new StreamReader(body, req.ContentEncoding))
                    {
                        requestBody = reader.ReadToEnd();
                    }
                }

                //Find the line with JSON and parse
                JObject? jsonBody = null;

                string[] ans = requestBody.Split("\n");
                foreach(string line in ans)
                {
                    if (line.Length > 0 && line[0] == '{')
                    {
                        jsonBody = JObject.Parse(line);
                    }
                }
                
                //Find the event in the dictionary
                byte[] data;
                if (RequestDelegates.TryGetValue(req.Url.AbsolutePath, out HttpRequest? del))
                {
                    // Write the response info
                    data = Encoding.UTF8.GetBytes(String.Format(del(jsonBody)));
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;
                }
                else 
                {
                    // Write the response info
                    data = Encoding.UTF8.GetBytes(String.Format("Path not found: 404"));
                    resp.ContentType = "text/html";
                    resp.ContentEncoding = Encoding.UTF8;
                    resp.ContentLength64 = data.LongLength;
                }

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data, 0, data.Length);
                resp.Close();
            }
        }

        public HttpServer(Dictionary<string, HttpRequest> events)
        {
            // Set up the events
            RequestDelegates = events;

            // Start the server
            listener.Prefixes.Add(URL);
            listener.Start();
            Console.WriteLine("Server URL: {0}", URL);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        } 
    }
}
