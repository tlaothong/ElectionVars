using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Election.Api.Models;
using MongoDB.Driver;
using System.Security.Authentication;
using System.IO;

namespace Election.Api.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("api/[controller]/[action]")]
    public class ElectionController : Controller
    {
        IMongoCollection<ElectionModel> ElectionCollection { get; set; }

        public ElectionController()
        {
            var settings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://guntza22:guntza220938@ds026558.mlab.com:26558/electionmana"));
            settings.SslSettings = new SslSettings()
            {
                EnabledSslProtocols = SslProtocols.Tls12
            };
            var mongoClient = new MongoClient(settings);
            var database = mongoClient.GetDatabase("electionmana");
            ElectionCollection = database.GetCollection<ElectionModel>("Election");
        }

        [HttpGet]
        public IEnumerable<ElectionModel> GetAll()
        {
            var listElection = ElectionCollection.Find(it => true).ToList();
            return listElection;
        }

        [HttpPost]
        public void FillDataArea()
        {
            ElectionCollection.DeleteMany(it => true);
            var csvReader = new ReadCsv();
            csvReader.GetElectionData();
            var electionData = csvReader.ListElection;
            var grouping = electionData.GroupBy(it => it.NameArea).ToList();
            var selectData = new List<ElectionModel>();
            foreach (var item in grouping)
            {
                var data = item.FirstOrDefault(it => it.Party == "เพื่อไทย");
                if (data != null)
                {
                    data.Id = Guid.NewGuid().ToString();
                    selectData.Add(data);
                }
            }
            ElectionCollection.InsertMany(selectData);
        }
    }
}
