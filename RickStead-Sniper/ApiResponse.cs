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
        public int  page { get; set; }
        public int totalPages { get; set; }
        public int totalAuctions { get; set; }
        public long lastUpdated { get; set; }
        public List<Auction> auctions { get; set; }
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
        //public string item_lore { get; set; }
        //public string extra { get; set; }
        //public string category { get; set; }
        //public string tier { get; set; }
        public long starting_bid { get; set; }
        //public string item_bytes { get; set; }
        public bool claimed { get; set; }
        //public List<dynamic>? claimed_bidders { get; set; }
        //public long highest_bid_amount { get; set; }
        public long last_updated { get; set; }
        public bool bin { get; set; }
        //public List<Bid> bids { get; set; }
    }

    public class CoflnetItem
    {
        public string name { get; set; }
        public string tag { get; set; }
        public string flags { get; set; }
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
}
