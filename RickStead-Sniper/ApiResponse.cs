using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS8600 // Suppressing "possible null reference" warnings
#pragma warning disable CS8602 
#pragma warning disable CS8603 
#pragma warning disable CS8604 
#pragma warning disable CS8618 

namespace App
{
    public class ApiResponse
    {
        public bool success { get; set; }
        public int page { get; set; }
        public int totalPages { get; set; }
        public int totalAuctions { get; set; }
        public long lastUpdated { get; set; }
        public Auctions auctions { get; set; }

        public Auctions ToAuctions()
        {
            Auctions result = (Auctions)this.auctions;
            return result;
        }
    } 

    public class Auctions: List<Auction>
    {
        public void RefactorItems(List<CoflnetItem> tagList)
        {
            Parallel.ForEach(tagList, item =>
            {
                foreach (Auction auction in this)
                {
                    if (auction.item_name.Contains(item.name))
                    {
                        auction.item_name = item.tag;
                    }
                }
            });
        }

        public List<Item> ConvertToItems()
        {
            List<Item> items = new List<Item>();
            foreach (Auction auction in this)
            {
                items.Add(auction.ConvertToItem());
            }
            return items;
        }
    }

    public class Auction
    {
        public string uuid { get; set; }
        //public string auctioneer { get; set; }
        //public string profile_id { get; set; }
        //public List<string> coop { get; set; }
        public long start { get; set; }
        public long end { get; set; }
        public string item_name { get; set; }
        public string item_lore { get; set; }
        //public string extra { get; set; }
        public string category { get; set; }
        public string tier { get; set; }
        public long starting_bid { get; set; }
        //public string item_bytes { get; set; }
        //public bool claimed { get; set; }
        //public List<dynamic>? claimed_bidders { get; set; }
        //public long highest_bid_amount { get; set; }
        public long last_updated { get; set; }
        public bool bin { get; set; }
        //public List<Bid> bids { get; set; }

        public Dictionary<string, int> ParseEnchants(string loreInput)
        {
            Dictionary<string, int> enchants = new Dictionary<string, int>();
            // Create a list to store the enchants
            //List<Enchant> enchants = new List<Enchant>();

            // Iterate over the item lore string
            foreach (string line in loreInput.Split('\n'))
            {
                // Check if the line starts with "§9"
                if (line.StartsWith("§9"))
                {

                }
                else if (line.StartsWith("§d§l")) 
                {

                }
            }
            return enchants;
        }

        public Item ConvertToItem()
        {
            return new Item(item_name, 
                            //ParseEnchants(item_lore), 
                            starting_bid,
                            (Tier)Enum.Parse(typeof(Tier), tier)
                            );
        }
    }

    public class CoflnetItem
    {
        public string name { get; set; }
        public string tag { get; set; }
        public string flags { get; set; }
    }

    public class Item
    {
        public string name;
        //public Dictionary<string, int> enchants;
        public long price;
        public Tier tier;

        public Item(string name, 
            //Dictionary<string, int> enchants, 
            long price, Tier tier)
        {
            this.name = name;
            //this.enchants = enchants;
            this.price = price;
            this.tier = tier;
        }
    }

    public class Bid
    {
        public string auction_id { get; set; }
        public string bidder { get; set; }
        public string profile_id { get; set; }
        public long amount { get; set; }
        public long timestamp { get; set; }
    }

    public enum Tier
    {
        COMMON = 1,
        UNCOMMON = 2,
        RARE = 3,
        EPIC = 4,
        LEGENDARY = 5,
        MYTHIC = 6,
        SPECIAL = 7
    }

    public class RomanNumerals
    {
        private static readonly Dictionary<char, int> romanNumeralValues = new Dictionary<char, int>
    {
        { 'I', 1 },
        { 'V', 5 },
        { 'X', 10 },
        { 'L', 50 },
        { 'C', 100 },
        { 'D', 500 },
        { 'M', 1000 }
    };

        public static int ToInteger(string romanNumeral)
        {
            int result = 0;
            int currentValue = 0;

            foreach (char c in romanNumeral)
            {
                int value = romanNumeralValues[c];

                if (value > currentValue)
                {
                    result -= currentValue;
                }

                result += value;
                currentValue = value;
            }

            return result;
        }
    }
}
