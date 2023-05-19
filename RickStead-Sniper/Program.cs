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
        private long prevUpdateTime;
        public readonly string hypixelBaseURL = "https://api.hypixel.net/skyblock/auctions?key=";
        public readonly string coflnetBaseURL = "https://sky.coflnet.com/api/";
        public List<CoflnetItem> itemTagList = new List<CoflnetItem>();
        public ApiResponse responseList;

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
;
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

        private long UnixTime()
        {
            return (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
        }

        private async Task<dynamic> ApiGetResponse(string destinationURL)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(destinationURL);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    return jsonResponse;
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

        /// <summary>
        /// Gets ITEM_TAGs and misc data for further use
        /// </summary>
        /// <returns>List<ColfnetItem></returns>
        public async Task<List<CoflnetItem>> GetItemNametags()
        {
            string jsonResponse = await ApiGetResponse(coflnetBaseURL+"items");
            List<CoflnetItem> items = JsonConvert.DeserializeObject<List<CoflnetItem>>(jsonResponse);

            // Remove all nulls
            items.RemoveAll(obj => obj.name != null && obj.name.Contains("null"));

            // Remove all that cannot be auctioned
            items.RemoveAll(obj => obj.flags != null && !obj.flags.Contains("AUCTION"));

            // Remove auctionable bazaar items
            items.RemoveAll(obj => obj.flags != null && obj.flags.Contains("BAZAAR"));

            foreach (CoflnetItem item in items)
            {
                Console.WriteLine(item.name + " - " + item.tag + " - " + item.flags);
            }

            if (itemTagList.Count == 0)
            {
                itemTagList = items;
            }

            return items;
        }

        /// <summary>
        /// Gets a single page from Hypixel API.
        /// </summary>
        /// <param name="pageNumber">Specified page</param>
        /// <returns>ApiResponse class with List<Auction> of one page</returns>
        public async Task<ApiResponse> GetAuctionPage(int pageNumber)
        {
            string jsonResponse = await ApiGetResponse(hypixelBaseURL + Key + "&page=" + pageNumber);
            return JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);
        }

        /// <summary>
        /// Gets auctions from all pages available (might differ from API amount of auctions).
        /// </summary>
        /// <returns>ApiResponse class with List<Auction> of all pages</returns>
        public async Task<ApiResponse> GetAllAuctions()
        {
            ApiResponse finalResponse = await GetAuctionPage(0);
            prevUpdateTime = finalResponse.lastUpdated;

            for (int i = 1; i < finalResponse.totalPages; i++)
            {
                Console.WriteLine("Getting page " + i);
                ApiResponse newPage = await GetAuctionPage(i);
                finalResponse.auctions.AddRange(newPage.auctions);
            }

            responseList = finalResponse;

            return finalResponse;
        }

        /// <summary>
        /// Gets list of auction that were posted after previous update.
        /// </summary>
        /// <param name="page">(optional) unused</param>
        /// <param name="furtherProcessing">(optional) unused</param>
        /// <returns>ApiResponse class with List<Auction> of first page filtered by start time</returns>
        public async Task<ApiResponse> GetNewAuctions(int page = 0, ApiResponse furtherProcessing = null)
        {

            List<Auction> NewAuctions = new List<Auction>();

            ApiResponse data;

            if (prevUpdateTime == 0)
            {
                //Create new timestamp
                data = await GetAuctionPage(page);
                prevUpdateTime = data.lastUpdated;

            }

            if (UnixTime() >= prevUpdateTime)
            {
                //long delta = (prevUpdateTime + 60000 - UnixTime()) < 1000 ? 1000 : prevUpdateTime + 60000 - UnixTime();
                long delta = (70000 - (UnixTime() - prevUpdateTime)) < 1000 ? 1000 : 70000 - (UnixTime() - prevUpdateTime);
                Console.WriteLine("Waiting for update for " + ( delta / 1000) + " seconds");
                await Task.Delay((int)delta);
            }

            if (furtherProcessing == null)
            {
                data = await GetAuctionPage(page);
            }
            else
            {
                data = furtherProcessing;
                ApiResponse newdata = await GetAuctionPage(page+1);
                data.auctions.AddRange(newdata.auctions);
            }

            foreach (Auction auc in data.auctions)
            {
                if (auc.start > prevUpdateTime)
                {
                    Console.WriteLine("•     " + auc.item_name + ": " + auc.starting_bid + "| BIN:" + auc.bin.ToString());
                    NewAuctions.Add(auc);
                }
                else
                {
                    Console.WriteLine(auc.uuid);
                }
            }
            Console.WriteLine("New auctions: " + NewAuctions.Count);
            data.auctions = NewAuctions;
            prevUpdateTime = data.lastUpdated;

            return data;
        }

    }

    class Program
    {
        static async Task Main()
        {
            Sniper sniper = new Sniper();
            //ApiResponse allAuctions = await sniper.GetAllAuctions();
            //Console.WriteLine($"Total auctions: {allAuctions.auctions.Count}. Total auctions by API: {allAuctions.totalAuctions}. " +
            //    $"Total pages: {allAuctions.totalPages}");

            //ApiResponse newAuctions = await sniper.GetNewAuctions();
            //allAuctions.auctions.AddRange(newAuctions.auctions);
            List<CoflnetItem> items = await sniper.GetItemNametags();
        }
    }
}
