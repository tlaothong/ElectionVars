using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Election.Api.Models
{
    public class PartyList
    {
        [BsonId]
        public string Id { get; set; }
        public string IdParty { get; set; }
        public string PartyName { get; set; }
        public double TotalScore { get; set; }
        public int HaveScore { get; set; }
        public double HaveScoreDigit { get; set; }
        public int AreaScore { get; set; }
        public int NameListScore { get; set; }
        public double PercentScore { get; set; }
    }
}