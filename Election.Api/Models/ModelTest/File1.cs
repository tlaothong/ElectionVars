using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Election.Api.Models
{
    public class File1
    {
        public string NameParty { get; set; }
        public string IDProvince { get; set; }
        public string NoRegister { get; set; }
        public string NameRegister { get; set; }
    }
}