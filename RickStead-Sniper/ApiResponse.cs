using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App
{
    public class ApiResponse
    {
        public bool success { get; set; }
        public int page { get; set; }
        public int totalPages { get; set; }
        public int totalAuctions { get; set; }
        public long lastUpdated { get; set; }
        public List<Auction> auctions { get; set; }

        public List<Item> ConvertToItems()
        {
            List<Item> items = new List<Item>();
            foreach (Auction auction in auctions)
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
            foreach (var line in loreInput.Split('\n'))
            {
                // If the line starts with "§9", then it is an enchant.
                if (line.StartsWith("§9"))
                {
                    // Get the roman number from the line.
                    string romanNumber = line.Substring(2);

                    // Convert the roman number to an integer.
                    int number = RomanNumerals.ToInteger(romanNumber);

                    // Add the text and roman number to the dictionary.
                    enchants.Add(line.Substring(0, 2), number);
                }
            }
            return enchants;
        }

        public Item ConvertToItem()
        {
            return new Item(item_name, 
                            ParseEnchants(item_lore), 
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
        public Dictionary<string, int> enchants;
        public long price;
        public Tier tier;

        public Item(string name, Dictionary<string, int> enchants, long price, Tier tier)
        {
            this.name = name;
            this.enchants = enchants;
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
