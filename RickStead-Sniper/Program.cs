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
        public readonly int MAX_LIST_ELEMENTS = 75000;
        public readonly int MIN_PROFIT_PERCENT = 20;
        public readonly int MIN_PROFIT_MARGIN = 1000000;

        private HttpClient client;
        private ApiResponseShort lastResponse;
        private readonly string hypixelBaseURL = "https://api.hypixel.net/skyblock/auctions?key=";
        private readonly string coflnetBaseURL = "https://sky.coflnet.com/api/";
        public List<CoflnetItem> itemTagList = new List<CoflnetItem>();

        public Sniper()
        {
            try
            {
                string keysPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\")) + "keys.json";
                string jsonString = File.ReadAllText(keysPath);
                JsonDocument jsonDocument = JsonDocument.Parse(jsonString);
                string Key = jsonDocument.RootElement.GetProperty("apiKey").ToString();
                hypixelBaseURL += Key;

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
                    return await response.Content.ReadAsStringAsync();
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

        public async Task<List<AuctionItem>> GetSoldItemsByTag(string tag, int amount = 30)
        {
            List<CoflnetAuction> coflnetAuctions = new();

            for (int i = 0; i <= amount; i++)
            {
                string jsonResponse = await ApiGetResponse(coflnetBaseURL + $"auctions/tag/{tag}/sold?page={i}&pageSize={amount}");
                List<CoflnetAuction> result = JsonConvert.DeserializeObject<List<CoflnetAuction>>(jsonResponse);

                // Remove non-bins
                result.RemoveAll(obj => obj.bin != true);
                coflnetAuctions.AddRange(result);

                // Page is limited to 1000 results per page.
                amount -= 1000;
                if (amount < 0)
                {
                    break;
                }
            }
            List<AuctionItem> auctionItems = coflnetAuctions.Select(obj => obj.ToAuctionItem()).ToList();
            return auctionItems;
        }

        public async Task<List<Auction>> GetAuctionPage(int pageNumber)
        {
            string jsonResponse = await ApiGetResponse(hypixelBaseURL + "&page=" + pageNumber);
            ApiResponse result = JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);

            result.auctionList.RemoveAll(obj => obj.bin != true);

            lastResponse = result.ToApiResponseShort();

            return result.auctionList;
        }

        public async Task<List<Auction>> GetAllAuctions()
        {
            List<Auction> finalResponse = await GetAuctionPage(0);

            for (int i = 1; i < lastResponse.totalPages; i++)
            //for (int i = 1; i < 5; i++)
            {
                Console.Clear();
                Console.WriteLine("Getting page " + i);
                List<Auction> newPage = await GetAuctionPage(i);
                finalResponse.AddRange(newPage);
            }

            return finalResponse;
        }

    }

    class Program
    {
        static async Task Main()
        {
            Sniper sniper = new();
            List<AuctionItem> auctionItems = await sniper.GetSoldItemsByTag("WISE_DRAGON_LEGGINGS");
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            foreach (AuctionItemEnchantable auctionItem in auctionItems)
            {
                Console.WriteLine(String.Format(
                    "{0}\n" +
                    "\tReforge: {1}\n" +
                    "\tPrice:  {2}\n", auctionItem.item_name, auctionItem.reforge, auctionItem.price));
            }
        }
    }
}