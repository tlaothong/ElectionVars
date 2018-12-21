using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Election.Api.Models
{
    public class ElectionModel
    {
        [BsonId]
        public string Id { get; set; }
        public string NameArea { get; set; }
        public int NumberArea { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Party { get; set; }
        public string NameRegister { get; set; }
        public int Score { get; set; }
        public int TargetScore { get; set; }
        public string Tag { get; set; }
    }
}