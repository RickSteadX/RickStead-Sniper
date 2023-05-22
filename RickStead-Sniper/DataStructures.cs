using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using fNbt;
using fNbt.Serialization;
using System.Text.Json;
using System.Collections;
using System.Text.RegularExpressions;

#pragma warning disable CS8600 // Suppressing "possible null reference" warnings
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8618

namespace App
{
    public class ApiResponseShort
    {
        [JsonProperty("success")]
        public bool success { get; set; }

        //public int page { get; set; }

        [JsonProperty("totalPages")]
        public int totalPages { get; set; }

        //public int totalAuctions { get; set; }

        [JsonProperty("lastUpdated")]
        public long lastUpdated { get; set; }
    }

    public class ApiResponse : ApiResponseShort
    {

        [JsonProperty("auctions")]
        public List<Auction> auctionList { get; set; }

        public ApiResponseShort ToApiResponseShort()
        {
            return new ApiResponseShort { success = success, lastUpdated = lastUpdated, totalPages = totalPages };
        }
    }

    public class Auction
    {
        public string uuid { get; set; }
        //public string auctioneer { get; set; }
        //public string profile_id { get; set; }
        //public List<string> coop { get; set; }
        public long start { get; set; }
        //public long end { get; set; }
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
    }

    public class AuctionItem
    {
        public string uuid { get; set; }
        public string item_name { get; set; }
        public string tag { get; set; }
        public long price { get; set; }
        public string lore { get; set; }
        public Category category { get; set; }
        public Tier tier { get; set; }
    }

    public class AuctionItemPet : AuctionItem
    {
        public short level;
    }

    public class AuctionItemEnchantable : AuctionItem
    {
        public List<Enchantment> enchantments { get; set; }
        public string reforge { get; set; }
    }

    public class AuctionItemDungeons : AuctionItemEnchantable
    {
        public short stars;
    }

    public class CoflnetItem
    {
        public string name { get; set; }
        public string tag { get; set; }
        public string flags { get; set; }
    }

    public class CoflnetAuction
    {
        public string uuid { get; set; }
        public int count { get; set; }
        public long startingBid { get; set; }
        public string tag { get; set; }
        public string itemName { get; set; }
        public string startTimeString { get; set; }
        public string endTimeString { get; set; }
        public string auctioneerId { get; set; }
        public string profileId { get; set; }
        public List<string>? coop { get; set; }
        public List<string>? coopMembers { get; set; }
        public long highestBidAmount { get; set; }
        public List<dynamic>? bids { get; set; }
        public short anvilUses { get; set; }
        public List<Dictionary<string, dynamic>> enchantments { get; set; }
        public Dictionary<string, Dictionary<string, dynamic>> nbtData { get; set; }
        public string itemCreatedAt { get; set; }
        public string reforge { get; set; }
        public string category { get; set; }
        public string tier { get; set; }
        public bool bin { get; set; }
        public Dictionary<string, string> flatNbt { get; set; }

        public AuctionItem ToAuctionItem()
        {
            AuctionItem item = new AuctionItem()
            {
                uuid = uuid,
                item_name = itemName,
                tag = tag,
                price = startingBid,
                category = Functions.EnumFromString<Category>(category),
                tier = Functions.EnumFromString<Tier>(tier),

            };

            if (itemName.Contains("✪"))
            {
                AuctionItemDungeons dungeonItem = Functions.CreateAuctionItem<AuctionItemDungeons>(item);
                dungeonItem.reforge = reforge;
                dungeonItem.stars = (short)dungeonItem.item_name.Count(c => c == '✪');
                dungeonItem.enchantments = enchantments
                    .Select(e => new Enchantment { enchantName = e["type"], enchantLevel = e["level"] })
                    .ToList();
                return dungeonItem;
            }
            else if (item.category == Category.ARMOR || item.category == Category.WEAPON)
            {
                AuctionItemEnchantable enchantItem = Functions.CreateAuctionItem<AuctionItemEnchantable>(item);
                enchantItem.reforge = reforge;
                enchantItem.enchantments = enchantments
                    .Select(e => new Enchantment { enchantName = e["type"], enchantLevel = e["level"] })
                    .ToList();
                return enchantItem;
            }
            else if (item.item_name.Contains("[Lvl"))
            {
                AuctionItemPet petItem = Functions.CreateAuctionItem<AuctionItemPet>(item);
                petItem.level = Functions.ParsePetLevelFromName(item.item_name);
                return petItem;
            }

            return item;
        }


    }

    public class Enchantment
    {
        public string enchantName;
        public long enchantLevel;
    }

    public enum Tier
    {
        NONE = 0,
        COMMON = 1,
        UNCOMMON = 2,
        RARE = 3,
        EPIC = 4,
        LEGENDARY = 5,
        MYTHIC = 6,
        SPECIAL = 7
    }

    public enum Category
    {
        NONE = 0,
        WEAPON = 1,
        ARMOR = 2,
        ACCESSORIES = 3,
        BLOCKS = 4,
        MISC = 5
    }

    public static class Functions
    {
        public static long ToUnixTimeSeconds(string timestamp)
        {
            return (long)(DateTime.Parse(timestamp) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }
        public static TEnum EnumFromString<TEnum>(string input) where TEnum : struct, Enum
        {
            if (Enum.GetValues(typeof(TEnum)).Cast<TEnum>().Any(e => e.ToString().Equals(input, StringComparison.OrdinalIgnoreCase)))
            {
                if (Enum.TryParse<TEnum>(input, true, out TEnum parsedValue))
                {
                    return parsedValue;
                }
            }

            return default(TEnum);
        }
        public static List<Enchantment> ParseEnchantments(string loreInput)
        {
            List<Enchantment> parsedData = new();

            string pattern = @"§9([\w\s]+) ([IVX]+)|§d§l([\w\s]+)";
            Regex regex = new Regex(pattern);

            List<string> blacklist = new List<string> { "§d§l§ka§r" };

            string[] lines = loreInput.Split('\n');
            foreach (string line in lines)
            {
                if (blacklist.Any(blacklisted => line.StartsWith(blacklisted)))
                    continue;

                string[] enchants = line.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string enchant in enchants)
                {
                    Match match = regex.Match(enchant);
                    if (match.Success)
                    {
                        string word = match.Groups[1].Value.Trim();
                        string romanNumeral = match.Groups[2].Value.Trim();
                        if (string.IsNullOrEmpty(word) || string.IsNullOrEmpty(romanNumeral))
                        {
                            word = match.Groups[3].Value.Trim();
                            romanNumeral = "I";
                        }
                        int parsedNumber = ParseRomanNumeral(romanNumeral);

                        parsedData.Add(new Enchantment { enchantName = word, enchantLevel = (short)parsedNumber });
                    }
                }
            }

            return parsedData;
        }
        private static readonly Dictionary<char, int> RomanValues = new Dictionary<char, int>
    {
        { 'I', 1 },
        { 'V', 5 },
        { 'X', 10 },
        { 'L', 50 },
        { 'C', 100 },
        { 'D', 500 },
        { 'M', 1000 }
    };
        public static int ParseRomanNumeral(string romanNumeral)
        {
            int result = 0;

            for (int i = 0; i < romanNumeral.Length; i++)
            {
                if (i < romanNumeral.Length - 1 && RomanValues[romanNumeral[i]] < RomanValues[romanNumeral[i + 1]])
                {
                    result -= RomanValues[romanNumeral[i]];
                }
                else
                {
                    result += RomanValues[romanNumeral[i]];
                }
            }

            return result;
        }
        public static short ParsePetLevelFromName(string nameInput)
        {
            string pattern = @"\[Lvl (\d+)\]";
            Match match = Regex.Match(nameInput, pattern);

            if (match.Success)
            {
                string numberString = match.Groups[1].Value;
                short number;
                if (short.TryParse(numberString, out number))
                {
                    return number;
                }
            }

            return 1; // Default value if no match or parsing error
        }
        public static T CreateAuctionItem<T>(AuctionItem item) where T : AuctionItem, new()
        {
            T auctionItem = new T()
            {
                uuid = item.uuid,
                item_name = item.item_name,
                tag = item.tag,
                price = item.price,
                category = item.category,
                tier = item.tier
            };

            return auctionItem;
        }
    }
}
