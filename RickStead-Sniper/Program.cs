using System;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using Newtonsoft.Json;
using System.Reflection;
using App;

namespace App
{
    public class Sniper
    {
        public readonly string Key = String.Empty;
        private readonly int verifyLots;
        private readonly int maxWorkers;
        private HttpClient client;
        public readonly string baseURL = "https://api.hypixel.net/skyblock/auctions?key=";
        public readonly string requestURL = String.Empty;

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

        private async Task<ApiResponse> ApiGetResponse(string destinationURL)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(destinationURL);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    // Parse the JSON response
                    ApiResponse parsedResponse = JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);

                    // Access the parsed data
                    if (parsedResponse.success == true)
                    {
                        return parsedResponse;
                    }
                    else
                    {
                        Console.WriteLine("Request denied with status code: " + response.StatusCode);
                        return null;
                    }
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

        public async Task<ApiResponse> GetAuctionPage(int pageNumber)
        {
            return await ApiGetResponse(baseURL+Key+"&page="+pageNumber);
        }

        public async Task<ApiResponse> GetAllAuctions()
        {
            ApiResponse finalResponse = await GetAuctionPage(0);

            for (int i = 1; i <= finalResponse.totalPages; i++)
            {
                Console.WriteLine("Getting page " + i);
                ApiResponse newPage = await GetAuctionPage(i);
                finalResponse.auctions.AddRange(newPage.auctions);
            }
            return finalResponse;
        }

        public async Task<ApiResponse> GetNewAuctions(int page = 0, int delay = 5000)
        {
            ApiResponse data = await GetAuctionPage(page);
            long previousLastUpdated = data.lastUpdated;
            List<Auction> NewAuctions = new List<Auction>();
            int counter = 0;

            while (true)
            {
                Console.WriteLine("Waiting for update..");
                await Task.Delay(delay);

                data = await GetAuctionPage(page);
                if (data.lastUpdated > previousLastUpdated)
                { 
                    foreach (Auction auc in data.auctions)
                    {
                        if (auc.start > previousLastUpdated)
                        {
                            Console.WriteLine(auc.item_name + ": " + auc.starting_bid + "| BIN:" + auc.bin.ToString() + "\n"
                                + "Start: " + auc.start + ":" + previousLastUpdated);
                            counter++;
                        }
                    }
                    Console.WriteLine("New auctions: " + counter);
                    counter = 0;
                    previousLastUpdated = data.lastUpdated;
                }
            }
        }
        
    }

    class Program
    {
        static async Task Main()
        {
            Sniper sniper = new Sniper();
            ApiResponse result = await sniper.GetAllAuctions();
            Console.WriteLine($"Total auctions: {result.auctions.Count}. Total auctions by API: {result.totalAuctions}. " +
                $"Total pages: {result.totalPages}");
            await File.WriteAllTextAsync("output.json", JsonConvert.SerializeObject(result));
        }
    }
}
