using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using fNbt;
using fNbt.Serialization;

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
        public long price { get; set; }
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

    public class Enchantment
    {
        public string enchantName;
        public short enchantLevel;
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

    public enum Category
    {
        WEAPON = 1,
        ARMOR = 2,
        ACCESSORIES = 3,
        BLOCKS = 4,
        MISC = 5
    }

    public class Functional
    {
        public void DecodeNBT(string encodedNBT)
        {
            byte[] decodedBytes = Convert.FromBase64String(encodedNBT);

            fNbt.NbtFile nbtFile = new fNbt.NbtFile();
            nbtFile.LoadFromBuffer(decodedBytes, 0, decodedBytes.Length, NbtCompression.AutoDetect);

            // Ensure that the root tag is a TAG_Compound
            if (nbtFile.RootTag.TagType != NbtTagType.Compound)
            {
                Console.WriteLine("Invalid NBT format. Expected root tag to be a TAG_Compound.");
                return;
            }

            // Unpack and access the NBT data
            NbtCompound rootCompound = nbtFile.RootTag;
            if (rootCompound != null)
            {
                Console.WriteLine(rootCompound["i"][0]["tag"]["display"]["Name"].ToString("   "));
                NbtTag qqq = rootCompound["i"][0]["tag"]["display"]["Lore"];
                Console.WriteLine(qqq.ToString());
            }
            else
            {
                Console.WriteLine("Invalid NBT format. Root tag is not a compound.");
            }
        }
    }
}
