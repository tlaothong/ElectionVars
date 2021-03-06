using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Election.Api.Models
{
    public class LocationData
    {
        [BsonId]
        public string Id { get; set; }
        public string NameParty { get; set; }
        public string NameArea { get; set; }
        public string LocationCode { get; set; }
        public string IDProvince { get; set; }
        public string District { get; set; }
        public string SubDistrict { get; set; }
        public string NameRegister { get; set; }      
    }
}