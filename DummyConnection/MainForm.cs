using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;


namespace DummyConnection
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            /*
            Server.Request(
                "/insert_user", 
                new { email = "kaelwilliam@ksu.edu", password = "abc123" },
                (string response) => { MessageBox.Show(response); }
           );
            */
            Server.Request(
                "/all_users",
                new {  },
                (string response) => {
                    List<(string, string)> result = JsonConvert.DeserializeObject<List<(string, string)>>(response);
                    foreach((string, string) l in result)
                    {
                        Debug.WriteLine(l);
                    }
                }
           );
    

            Server.Request(
                "/user_information",
                new { UserId = 2 },
                (string response) => {
                    (string, string, string, decimal, int) result = JsonConvert.DeserializeObject<List<(string, string, string, decimal, int)>>(response)[0];
                }
           );


            Server.Request(
    "/all_users",
    new { },
    (string response) => {
        List<(string, string)> result = JsonConvert.DeserializeObject<List<(string, string)>>(response);
        foreach ((string, string) l in result)
        {
            Debug.WriteLine(l);
        }
    }
);
        }


    }
}