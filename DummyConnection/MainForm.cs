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

            Request("/form", new { Metadata = "abc", ABC = "abc" });
        }

        static async void Request(string path, object body)
        {

            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = new TimeSpan(1, 1, 1);

            MultipartFormDataContent form = new MultipartFormDataContent();
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(body);
            form.Add(new ByteArrayContent(jsonBytes), "entry");

            HttpResponseMessage response = await httpClient.PostAsync("http://localhost:8000"+path, form);

            string strContent = await response.Content.ReadAsStringAsync();

            MessageBox.Show(strContent);
        }
    }
}