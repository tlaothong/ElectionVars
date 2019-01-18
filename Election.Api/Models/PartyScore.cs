using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Election.Api.Models
{
    public class PartyScore
    {
        [BsonId]
        public string Id { get; set; }
        public string PartyName { get; set; }
        public int TotalScore { get; set; }
        public int AreaScore { get; set; }
        public int NameListScore { get; set; }
    }
}