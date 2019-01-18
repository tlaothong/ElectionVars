using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Election.Api.Models
{
    public class AreaElection
    {
        [BsonId]
        public string Id { get; set; }
        public string NameArea { get; set; }
        public string PartyName { get; set; }
        public int Score { get; set; }
        public string Tag { get; set; }
        public string PartyWinner { get; set; }
    }
}