using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace Election.Api.Models
{
    public class ScorePollCsv
    {
        [BsonId]
        public string Id { get; set; }
        public string IdParty { get; set; }
        public string NameParty { get; set; }
        public string IdArea { get; set; }
        public string NameArea { get; set; }
        public string Region { get; set; }
        public string IdRegion { get; set; }
        public float Score { get; set; }
    }
}