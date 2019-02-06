using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Election.Api.Models
{
    public class PartyList
    {
        [BsonId]
        public string Id { get; set; }
        public string PartyName { get; set; }
        public double TotalScore { get; set; }
        public double AreaScore { get; set; }
        public double NameListScore { get; set; }
        public double PercentScore { get; set; }
    }
}