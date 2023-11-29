using System.Diagnostics;
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

            Server.Request(
                "/insert_user", 
                new { Email = "kael@ksu.edu", Password = "abc123" },
                (string response) => { MessageBox.Show(response); }
           );
        }


    }
}