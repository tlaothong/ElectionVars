using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Election.Api.Models
{
    public class ScoreArea
    {
        [BsonId]
        public string Id { get; set; }
        public string IdArea { get; set; }
        public string NameArea { get; set; }
        public string IdParty { get; set; }
        public string NameParty { get; set; }
        public string NoRegister { get; set; }
        public string NameRegister { get; set; }
        public bool Status { get; set; }
        public string NameInitial { get; set; }
        public List<string> Tags { get; set; }
        public double Score { get; set; }
        public string Source { get; set; }
        public bool StatusEdit { get; set; }
        public bool StatusAreaEdit { get; set; }
        public string Region { get; set; }
        public string IdRegion { get; set; }
    }
}