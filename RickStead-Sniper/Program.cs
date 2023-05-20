using System;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using Newtonsoft.Json;
using System.Reflection;
using System.Linq;
using App;

#pragma warning disable CS8600 // Suppressing "possible null reference" warnings
#pragma warning disable CS8602 
#pragma warning disable CS8603 
#pragma warning disable CS8604 
#pragma warning disable CS8618 

namespace App
{
    public class Sniper
    {
        public readonly string Key = String.Empty;
        private HttpClient client;
        private long currentUpdateTime;
        private long prevUpdateTime;
        private readonly bool binOnly = false;
        public readonly string hypixelBaseURL = "https://api.hypixel.net/skyblock/auctions?key=";
        public readonly string coflnetBaseURL = "https://sky.coflnet.com/api/";
        public List<CoflnetItem> itemTagList = new List<CoflnetItem>();
        public ApiResponse responseList;

        public Sniper(bool binOnly = true)
        {
            try
            {
                string keysPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\")) + "keys.json";

                string jsonString = File.ReadAllText(keysPath);
                JsonDocument jsonDocument = JsonDocument.Parse(jsonString);
                Key = jsonDocument.RootElement.GetProperty("apiKey").ToString();
;
                client = new HttpClient();
                this.binOnly = binOnly;
                GetItemNametags();
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
        public async void GetItemNametags()
        {
            if (itemTagList.Count != 0)
            {
                return;
            }

            string jsonResponse = await ApiGetResponse(coflnetBaseURL+"items");
            List<CoflnetItem> items = JsonConvert.DeserializeObject<List<CoflnetItem>>(jsonResponse);

            // Remove all nulls
            items.RemoveAll(obj => obj.name != null && obj.name.Contains("null"));

            // Remove all that cannot be auctioned
            items.RemoveAll(obj => obj.flags != null && !obj.flags.Contains("AUCTION"));

            // Remove auctionable bazaar items
            items.RemoveAll(obj => obj.flags != null && obj.flags.Contains("BAZAAR"));

            // Set all items with no names to their tag
            items = items.Select(item =>
            {
                if (item.name == null)
                {
                    item.name = item.tag;
                }
                return item;
            }).ToList();
        }

        /// <summary>
        /// Gets a single page from Hypixel API.
        /// </summary>
        /// <param name="pageNumber">Specified page</param>
        /// <returns>ApiResponse class with Auctions of one page</returns>
        public async Task<Auctions> GetAuctionPage(int pageNumber)
        {
            string jsonResponse = await ApiGetResponse(hypixelBaseURL + Key + "&page=" + pageNumber);
            ApiResponse result = JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);

            if (currentUpdateTime == 0)
            {
                currentUpdateTime = result.lastUpdated;
            }
            else if (currentUpdateTime != 0 && prevUpdateTime != 0 && currentUpdateTime != prevUpdateTime)
            {
                prevUpdateTime = currentUpdateTime;
                currentUpdateTime = result.lastUpdated;
            }

            if (binOnly)
            {
                result.auctions.RemoveAll(obj => obj.bin != true);
            }
            return result.auctions;
        }

        /// <summary>
        /// Gets auctions from all pages available (might differ from API amount of auctions).
        /// </summary>
        /// <returns>ApiResponse class with Auctions of all pages</returns>
        public async Task<Auctions> GetAllAuctions()
        {
            Auctions finalResponse = await GetAuctionPage(0);

            //for (int i = 1; i < finalResponse.totalPages; i++)
            for (int i = 1; i < 5; i++)
            {
                Console.WriteLine("Getting page " + i);
                Auctions newPage = await GetAuctionPage(i);
                finalResponse.AddRange(newPage);
            }

            return finalResponse;
        }

        /// <summary>
        /// Gets list of auction that were posted after previous update.
        /// </summary>
        /// <param name="page">(optional) unused</param>
        /// <param name="furtherProcessing">(optional) unused</param>
        /// <returns>ApiResponse class with Auctions of first page filtered by start time</returns>
        public async Task<Auctions> GetNewAuctions(int page = 0, Auctions? furtherProcessing = null)
        {

            Auctions NewAuctions = new Auctions();

            Auctions data;

            if (prevUpdateTime == 0)
            {
                //Create new timestamp
                data = await GetAuctionPage(page);
            }

            if (UnixTime() >= prevUpdateTime)
            {
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
                Auctions newdata = await GetAuctionPage(page+1);
                data.AddRange(newdata);
            }

            foreach (Auction auc in data)
            {
                if (auc.start > prevUpdateTime)
                {
                    NewAuctions.Add(auc);
                }
            }
            Console.WriteLine("New auctions: " + NewAuctions.Count);
            data = NewAuctions;

            return data;
        }
   
    }

    class Program
    {
        static async Task Main()
        {
            Sniper sniper = new Sniper();
            Auctions allAuctions = await sniper.GetAllAuctions();
            Auctions newAuctions = await sniper.GetNewAuctions();
            allAuctions.AddRange(newAuctions);

            //Console.WriteLine(RomanNumerals.ToInteger("III"));

            
            //newAuctions = (Auctions)newAuctions.Distinct().ToList();
            foreach (Auction auction in newAuctions)
            {
                List<Item> newAuctionsItems = newAuctions.ConvertToItems();
                foreach (Item item in newAuctionsItems)
                {
                    Console.WriteLine(item.name);
                    Console.WriteLine("BIN: " + item.price);
                }
                //Auctions priceRange = allAuctions.auctions.Where(obj => obj.item_name == auction.item_name).ToList();
                //List<long> prices = priceRange.Select(item => item.starting_bid).Distinct().ToList();
                //prices.Sort();

            }
            
        }
    }
}
