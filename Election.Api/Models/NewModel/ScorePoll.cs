using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Election.Api.Models
{
    public class ScorePoll
    {
        [BsonId]
        public string Id { get; set; }
        public string IdParty { get; set; }
        public string IdArea { get; set; }
        public DateTime datePoll { get; set; }
        public double Score { get; set; }
        public string Source { get; set; }
        public double TargetScoreDefault { get; set; }
        public double TargetScore { get; set; }
    }
}