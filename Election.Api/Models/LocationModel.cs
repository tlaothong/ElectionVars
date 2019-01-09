using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Election.Api.Models
{
    public class LocationModel
    {
        [BsonId]
        public string Id { get; set; }
        public string IDProvince { get; set; }
        public string LocationCode { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string SubDistrict { get; set; }
        public string ZipCode { get; set; }
        public string Note { get; set; }
    }
}