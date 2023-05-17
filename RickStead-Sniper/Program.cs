using System;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using Newtonsoft.Json;
using System.Reflection;

namespace App
{
    public class Sniper
    {
        private readonly string Key = String.Empty;
        private readonly int verifyLots;
        private readonly int maxWorkers;
        private readonly string baseURL = "https://api.hypixel.net/skyblock/auctions?key=";
        private readonly string requestURL = String.Empty;
        private HttpClient client;

        public Sniper()
        {
            try
            {
                string configPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\")) + "config.json";
                string keysPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\")) + "keys.json";

                string jsonString = File.ReadAllText(configPath);
                JsonDocument jsonDocument = JsonDocument.Parse(jsonString);
                verifyLots = jsonDocument.RootElement.GetProperty("lotVerifyQuantity").GetInt32();
                maxWorkers = jsonDocument.RootElement.GetProperty("maxWorkers").GetInt32();

                jsonString = File.ReadAllText(keysPath);
                jsonDocument = JsonDocument.Parse(jsonString);
                Key = jsonDocument.RootElement.GetProperty("apiKey").ToString();

                requestURL = baseURL + Key;
                client = new HttpClient();
            }
            catch (IOException ex)
            {
                // Handle file IO errors
                Console.WriteLine("Error reading the JSON file: " + ex.Message);
            }
            catch (System.Text.Json.JsonException ex)
            {
                // Handle JSON parsing errors
                Console.WriteLine("Error parsing the JSON file: " + ex.Message);
            }
        }

        public async Task<dynamic> GetAuctions()
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(requestURL);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    // Parse the JSON response
                    var parsedResponse = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

                    // Access the parsed data
                    bool success = parsedResponse.success;
                    if (success) {
                        for (int i = 0; i < parsedResponse.totalPages; i++)
                        {

                        }
                    }

                    // Create an array with the parsed data
                    string[] data = new string[] { };

                    return data;
                }
                else
                {
                    Console.WriteLine("Request failed with status code: " + response.StatusCode);
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP request errors
                Console.WriteLine("HTTP request failed: " + ex.Message);
                return null;
            }
        }

        public async Task<dynamic> GetAuctionPage(int pageNumber)
        {

            try
            {
                HttpResponseMessage response = await client.GetAsync(requestURL+"&page="+pageNumber);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    // Parse the JSON response
                    var parsedResponse = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

                    // Access the parsed data
                    bool success = parsedResponse.success;
                    if (success)
                    {

                    }

                    // Create an array with the parsed data
                    string[] data = new string[] { };

                    return data;
                }
                else
                {
                    Console.WriteLine("Request failed with status code: " + response.StatusCode);
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP request errors
                Console.WriteLine("HTTP request failed: " + ex.Message);
                return null;
            };
        }
    }

    class Program
    {
        static async Task Main()
        {
            Sniper sniper = new Sniper();
            await sniper.GetAuctions();
        }
    }
}
