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

        /*
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
            Sniper sniper = new();
            //List<Auction> auctions = await sniper.GetAuctionPage(0);
            //List<Auction> auctionSlice = auctions.GetRange(0, 100);
            //Console.WriteLine(auctionSlice.Count);
            Functional a = new();
            a.DecodeNBT("H4sIAAAAAAAAAF1T3W7bNhj9FDet4xXI7othRNaiMdwosvwn586zXadA5gRR2mIoioCWvthEJdIQ6WW53DvsYggwYHd+Dz9KHmToR0lJfwTL4s85h4eHH2sAO+CIGgA4W7AlYqfhwPZQraRxalAxfO7AD2/lLEP+ic8SdCqwcyxifJ3wuSbS/zV4Egu9TPjNDjw6URlWafQZ/LxZ9ybIMxZGNHbENuu41wnoE+w3ux2vDvsECE2Gcm4WdjpqdDr06e83/E49xzWafttt1aFByGEmDBsuuIywADf9FwWaGg/wF3U4vAePeMrnJbjdK8H+F7DXdW0PXhLhGHlSuOCNVssvsL0S2et4bssvDI/wCqXGAtls9b5Ftry+2yXkM7u1JWJc4IJyxcD1gmI3b6TBJBFzLLfDG75XGvRKrXbHJaVd2/lA793t3/T/kaKlYPuTTF2bBXtHzVeWd5Ypg5ERSrJ3BNnbrLuvV0nCQjTsVyVX+oiFCx6razbQmmstJPxkc1JJQjRmFsh0Pq+ZurJd+IWmUWIqULMbtWKfBMkJGVEZEHtux7JcI85jZlcqy2Uy1OZeY89Or+Qcydb1QiTIrqkgLNsshGYajZtj8uO8+/cvdl8OzC7+B2Y3+bK0ITtwKhmFhkuVmSM2ybg0uigEj6jwnJpf062fzXrWtLH2NEZKxtolpR8prYGMBMoymdzBgxwdqs3z7p//2FdFZNfvE2yJVjQacsMjlc4soZeQ0cQtQ9+sk5PxZDwdDc5/Z6O308n4dMqGx+Pw4uxkcDGuwHakEpURGqrwaMpThDrR7m5v2b2p786Jap4SpdtlEGqwO/7TZHxgTCZmK4PagadlwJfCYOpUoZqqWFwJzOAJLxSr9lLDXngxOD8fjy7D48Ho9P3lIAzp92Z6+cVdDZ5SPdIVMynRdAUez/MyI7PbFagtH2rMDlj/qxUJP+fBjEdxHBzETc87aEd8dsC7zfhg5sVt3gqiiPtBFXaMSGkjPF3CbuewGRz6LdY+avfZ2W8AW/C4DJqezxKwtSSRBAAA");    
        }
    }
}