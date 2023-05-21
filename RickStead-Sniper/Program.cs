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

        /*
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
        

        public async Task<Auctions> GetNewAuctions(int page = 0, Auctions? furtherProcessing = null)
        {

            Auctions NewAuctions = new Auctions();

            Auctions data;
            long delta;

            // Wait for next update
            delta = (65000 - (UnixTime() - currentUpdateTime)) < 1000 ? 100 : 65000 - (UnixTime() - currentUpdateTime);


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

        */

    }

    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("Sniper");
        }
    }
}
