using System;
using System.IO;
using TextCopy;
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
        public ApiResponse lastResponse;

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

            string jsonResponse = await ApiGetResponse(coflnetBaseURL + "items");
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

            itemTagList = items;
        }

        public async Task<long> GetItemAverageMin (string itemName)
        {
            string jsonResponse = await ApiGetResponse(coflnetBaseURL + $"item/price/{itemName}/history/day");
            List<CoflnetPrice> items = JsonConvert.DeserializeObject<List<CoflnetPrice>>(jsonResponse);
            List<long> minPrices = items.Select(item => item.min).ToList();
            return (long)minPrices.Average();
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
            lastResponse = result;

            if (result.lastUpdated != currentUpdateTime)
            {
                prevUpdateTime = currentUpdateTime;
                currentUpdateTime = result.lastUpdated;
            }

            if (prevUpdateTime == currentUpdateTime)
            {
                prevUpdateTime = 0;
            }

            if (binOnly)
            {
                result.auctions.RemoveAll(obj => obj.bin != true);
            }
            Auctions final = result.ToAuctions();
            return final;
        }

        /// <summary>
        /// Gets auctions from all pages available (might differ from API amount of auctions).
        /// </summary>
        /// <returns>ApiResponse class with Auctions of all pages</returns>
        public async Task<Auctions> GetAllAuctions()
        {
            Auctions finalResponse = await GetAuctionPage(0);

            for (int i = 1; i < lastResponse.totalPages; i++)
            //for (int i = 1; i < 5; i++)
            {
                Console.WriteLine("Getting page " + i);
                Auctions newPage = await GetAuctionPage(i);
                finalResponse.Get().AddRange(newPage.Get());
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
            long delta;

            // Wait for next update
            delta = (65000 - (UnixTime() - currentUpdateTime)) < 1000 ? 1000 : 65000 - (UnixTime() - currentUpdateTime);


            Console.WriteLine("Waiting for update for " + (delta / 1000) + " seconds");
            await Task.Delay((int)delta);

            if (furtherProcessing == null)
            {
                data = await GetAuctionPage(page);
            }
            else
            {
                data = furtherProcessing;
                Auctions newdata = await GetAuctionPage(page + 1);
                data.Get().AddRange(newdata.Get());
            }

            foreach (Auction auc in data.Get())
            {
                if (auc.start > prevUpdateTime)
                {
                    NewAuctions.Get().Add(auc);
                }
            }
            Console.WriteLine("New auctions: " + NewAuctions.Get().Count);
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
            allAuctions.RefactorItems(sniper.itemTagList);
            JsonSerializerOptions json = new JsonSerializerOptions() { WriteIndented = true};

            while (true)
            {
                Auctions newAuctions = await sniper.GetNewAuctions();
                newAuctions.RefactorItems(sniper.itemTagList);
                allAuctions.Get().AddRange(newAuctions.Get());

                var distinctNames = newAuctions.Get().Select(auction => auction.item_name).Distinct().ToList();
                distinctNames = distinctNames.Where(auction => !auction.Contains("[Lvl")).ToList();

                double minProfit = 10;
                double minMargin = 500000;

                foreach (var distinctName in distinctNames)
                {
                    List<Auction> itemSet = allAuctions.Get().Where(auction => auction.item_name.Contains(distinctName)).ToList();
                    //Auctions itemSet = (Auctions)itemSett;
                    itemSet.Sort((a, b) => a.starting_bid.CompareTo(b.starting_bid));
                    List<Auction> itemSetSorted = itemSet;

                    if (itemSetSorted.Count < 3)
                    {
                        continue;
                    }

                    if (itemSetSorted[0].category == "misc")
                    {
                        continue;
                    }

                    //Auction next = itemSetSorted.Skip(1).FirstOrDefault(item => item.starting_bid != itemSetSorted[0].starting_bid);
                    Auction next = itemSetSorted.Skip(1).Where(item => item != itemSetSorted[0]).FirstOrDefault();
                    long lowest = itemSetSorted[0].starting_bid;
                    long nextLowest = next.starting_bid;

                    if (lowest >= nextLowest)
                    {
                        continue;
                    }

                    var toJson = new
                    {
                        item = next.item_name,
                        data = itemSetSorted.Select(auction => (auction.starting_bid, auction.uuid)).ToList()
                    };

                    double profitPercent = (((double)nextLowest - (double)lowest) / (double)lowest) * 100;
                    long profit = nextLowest - lowest;
                    if (profitPercent > minProfit && profit > minMargin && profitPercent < 100)
                    {
                        long dayAverage = await sniper.GetItemAverageMin(itemSetSorted[0].item_name);
                        Console.WriteLine($"{next.item_name}: profit - {profit}({profitPercent.ToString("#.##")}%) {itemSetSorted[0].uuid}\n" +
                            $"Compared to {next.uuid}. Item day average: {dayAverage}");
                        ClipboardService.SetText($"/viewauction {itemSetSorted[0].uuid}");
                        File.WriteAllText($"data/{next.item_name}.json", JsonConvert.SerializeObject(toJson));
                    }

                }

            }
            /*
            var distinctNames = newAuctions.Get().Select(auction => auction.item_name).Distinct().ToList();
            distinctNames = distinctNames.Where(auction => !auction.Contains("[Lvl")).ToList();
            foreach (var distinctName in distinctNames)
            {
                List<Auction> itemSet = allAuctions.Get().Where(auction => auction.item_name.Contains(distinctName)).ToList();
                //Auctions itemSet = (Auctions)itemSett;
                itemSet.Sort((a, b) => a.starting_bid.CompareTo(b.starting_bid));
                List<Auction> itemSetSorted = itemSet;


                if (itemSetSorted.Count > 1)
                {
                    //Auction next = itemSetSorted.Skip(1).FirstOrDefault(item => item.starting_bid != itemSetSorted[0].starting_bid);
                    Auction next = itemSetSorted.Skip(1).Where(item => item != itemSetSorted[0]).FirstOrDefault();
                    long lowest = itemSetSorted[0].starting_bid;
                    long nextLowest = next.starting_bid;
                    if (lowest < nextLowest)
                    {
                        double profitPercent = (((double)nextLowest - (double)lowest) / (double)lowest) * 100;
                        long profit = nextLowest - lowest;
                        Console.WriteLine($"{next.item_name}: profit - {profit}({profitPercent}%)");
                    }
                }
            }
            */

        }
    }
}
