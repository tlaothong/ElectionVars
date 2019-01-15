using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Election.Api.Models
{
    public class LocationCodeModel
    {
        [BsonId]
        public string Id { get; set; }
        public string NameArea { get; set; }
        public string IdArea { get; set; }
        public string PartyListName { get; set; }
        public string NumberRegister { get; set; }
        public string NameRegister { get; set; }
        public bool HasRegister { get; set; }
    }
}