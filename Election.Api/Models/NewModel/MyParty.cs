using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Election.Api.Models
{
    public class test
    {
        [BsonId]
        public string Id { get; set; }
        public string IdArea { get; set; }
        public string NameArea { get; set; }
        public string PartyWin { get; set; }
        public double scoreMax { get; set; }
        public double scoreMyParty { get; set; }
        public bool StatusAreaEdit { get; set; }
    }
}