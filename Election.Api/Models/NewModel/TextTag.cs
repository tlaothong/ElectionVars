using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Election.Api.Models
{
    public class TextTag
    {
        public string Text { get; set; }
    }
}