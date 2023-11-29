using System.Text.Json;

namespace DummyConnection
{

    public delegate void OnResponseRecieved(string response);

    public static class Server
    {
        public static async void Request(string path, object body, OnResponseRecieved onRecieved)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.Timeout = new TimeSpan(1, 1, 1);

            MultipartFormDataContent form = new MultipartFormDataContent();
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(body);
            form.Add(new ByteArrayContent(jsonBytes), "entry");

            try
            {
                HttpResponseMessage response = await httpClient.PostAsync("http://localhost:8000" + path, form);
                string strContent = await response.Content.ReadAsStringAsync();
                onRecieved(strContent);
            }
            catch (Exception e)
            {
                MessageBox.Show("No connection");
            }
        }
    }
}
