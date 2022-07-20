using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;

namespace PortfolioPal.Models
{
    public class Clock
    {

        private HttpClient _client;
        private string APCA_API_KEY = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_key").ToString();
        private string APCA_API_SECRET = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_secret").ToString();
        private string APCA_API_URL = JObject.Parse(File.ReadAllText("keychain.json")).GetValue("alpaca_url").ToString();

        public static bool marketOpen { get; set; }
        public static string nextOpen { get; set; }
        public static string nextClose { get; set; }
        public static string clockStamp { get; set; }

        public Clock()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("APCA-API-KEY-ID", APCA_API_KEY);
            _client.DefaultRequestHeaders.Add("APCA-API-SECRET-KEY", APCA_API_SECRET);
            var clockURL = $"{APCA_API_URL}/v2/clock";
            var clockResponse = _client.GetStringAsync(clockURL).Result;
            var clockInfo = JObject.Parse(clockResponse).ToString();
            marketOpen = bool.Parse(JObject.Parse(clockInfo).GetValue("is_open").ToString());
            nextOpen = JObject.Parse(clockInfo).GetValue("next_open").ToString();
            nextClose = JObject.Parse(clockInfo).GetValue("next_close").ToString();
            clockStamp = JObject.Parse(clockInfo).GetValue("timestamp").ToString();
        }
    }
}
