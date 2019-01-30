using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Election.Api.Models
{
    public class DataTable2
    {
        [BsonId]
        public string Id { get; set; }
        public string NameArea { get; set; }
        public string IDProvince { get; set; }
        public string NameParty { get; set; }
        public string InitialParty { get; set; }
        public string NoRegister { get; set; }
        public string NameRegister { get; set; }
        public bool Status { get; set; }
        public string Tag { get; set; }
        public int Score { get; set; }
        public int TargetScore { get; set; }
        public int PollScore { get; set; }
    }
}