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
        IMongoCollection<LocationModel> LocationCollection { get; set; }
        IMongoCollection<AreaElection> AreaElectionColloection { get; set; }
        IMongoCollection<PartyScore> PartyScoreColloection { get; set; }
        IMongoCollection<AreaData> AreaCollection { get; set; }

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
            LocationCollection = database.GetCollection<LocationModel>("LocationTest");
            AreaElectionColloection = database.GetCollection<AreaElection>("AreaElection");
            PartyScoreColloection = database.GetCollection<PartyScore>("PartyScore");
            AreaCollection = database.GetCollection<AreaData>("AreaTest");
        }

        // Fill Data

        [HttpPost]
        public void FillDataArea()
        {
            ElectionCollection.DeleteMany(it => true);
            var csvReader = new ReadCsv();
            var electionData = csvReader.GetElectionData();
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

        [HttpPost]
        public void fillDataLocation()
        {
            LocationCollection.DeleteMany(it => true);
            var csvReader = new ReadCsv();
            var dataLocation = csvReader.GetDataLocation();
            var listLocation = new List<LocationModel>();
            foreach (var data in dataLocation)
            {
                data.Id = Guid.NewGuid().ToString();
                listLocation.Add(data);
            }
            LocationCollection.InsertMany(listLocation);
        }

        [HttpPost]
        public void fillDataAreaElection()
        {
            AreaElectionColloection.DeleteMany(it => true);
            var csvReader = new ReadCsv();
            var dataArea = csvReader.GetDataAreaElection();
            var listArea = new List<AreaElection>();
            foreach (var data in dataArea)
            {
                data.Id = Guid.NewGuid().ToString();
                listArea.Add(data);
            }
            AreaElectionColloection.InsertMany(listArea);
        }

        [HttpPost]
        public void fillDataPartyScore()
        {
            PartyScoreColloection.DeleteMany(it => true);
            var csvReader = new ReadCsv();
            var dataPartScore = csvReader.GetDataPartyScore();
            var listParty = new List<PartyScore>();
            foreach (var data in dataPartScore)
            {
                data.Id = Guid.NewGuid().ToString();
                listParty.Add(data);
            }
            PartyScoreColloection.InsertMany(listParty);
        }

        // Election

        [HttpGet]
        public List<ElectionModel> GetAll()
        {
            var listElection = ElectionCollection.Find(it => true).ToList();
            return listElection;
        }

        // Location

        [HttpGet]
        public List<LocationModel> GetAllLocation()
        {
            var listLocation = LocationCollection.Find(it => true).ToList();
            return listLocation;
        }

        [HttpGet("{LocationId}")]
        public LocationModel GetLocationCode(string LocationId)
        {
            var getLoation = LocationCollection.Find(it => it.LocationCode == LocationId).FirstOrDefault();
            return getLoation;
        }

        [HttpGet]
        public List<string> GetAllProvince()
        {
            var listLocation = LocationCollection.Find(it => true).ToList();
            var listProvinceGroupBy = listLocation.OrderBy(it => it.LocationCode).GroupBy(it => it.Province).ToList();
            var listProvinceName = new List<string>();
            foreach (var data in listProvinceGroupBy)
            {
                listProvinceName.Add(data.Key.ToString());
            }
            return listProvinceName;
        }

        [HttpGet("{nameProvince}")]
        public List<LocationModel> GetLocation(string nameProvince)
        {
            var listLocation = LocationCollection.Find(it => it.Province == nameProvince).ToList();
            return listLocation;
        }

        [HttpGet("{filter}")]
        public List<ElectionModel> GetFilter(string filter)
        {
            var getFilter = ElectionCollection.Find(it => it.Tag == filter).ToList();
            return getFilter;
        }

        // AreaElection

        [HttpGet]
        public List<AreaElection> GetAllAreaElection()
        {
            var getDataArea = AreaElectionColloection.Find(it => true).ToList();
            return getDataArea;
        }

        [HttpGet]
        public List<AreaElection> GetMaxAreaElection()
        {
            var getDataArea = AreaElectionColloection.Find(it => true).ToList();
            var dataElection = getDataArea.GroupBy(it => it.NameArea).ToList();
            var listMaxArea = new List<AreaElection>();
            foreach (var item in dataElection)
            {
                var itemMax = item.FirstOrDefault(it => it.Score == item.Max(i => i.Score));
                listMaxArea.Add(itemMax);
            }
            return listMaxArea;
        }

        [HttpGet("{tagName}")]
        public List<AreaElection> FilterTag(string tagName)
        {
            var getFilterPartyName = AreaElectionColloection.Find(it => it.PartyName == "เพื่อไทย").ToList();
            var getTag = getFilterPartyName.Where(it => it.Tag == tagName).ToList();
            return getTag;
        }

        // PartyScore

        [HttpGet]
        public List<PartyScore> GetAllParty()
        {
            var getPartyScore = PartyScoreColloection.Find(it => true).ToList();
            var totalScore = 0.0;
            foreach (var item in getPartyScore)
            {
                totalScore += item.TotalScore;
            }

            foreach (var item in getPartyScore)
            {
                item.PercentScore = item.TotalScore * 100 / totalScore;
            }
            return getPartyScore;
        }

        [HttpGet("{nameParty}")]
        public PartyScore GetPartyScore(string nameParty)
        {
            var getPartyScore = PartyScoreColloection.Find(it => it.PartyName == nameParty).FirstOrDefault();
            return getPartyScore;
        }

        // Location Code Test

        [HttpGet]
        public List<AreaData> GetAllArea()
        {
            var getAllArea = AreaCollection.Find(it => true).ToList();
            return getAllArea;
        }

        // Area Data
        [HttpPost]
        public void AddMockData()
        {
            AreaCollection.DeleteMany(it => true);
            var csvReader = new ReadCsv();
            var dataFile1 = csvReader.GetFile1();
            var areaDatas = csvReader.GetFile2();

            foreach (var data in areaDatas)
            {
                if (dataFile1.Any(it => it.IDProvince == data.IDProvince && it.NameParty == data.NameParty))
                {
                    var getFile1 = dataFile1.Find(it => it.IDProvince == data.IDProvince && it.NameParty == data.NameParty);
                    data.NameRegister = getFile1.NameRegister;
                    data.NoRegister = getFile1.NoRegister;
                    data.Status = true;
                }
                else
                {
                    data.Status = false;
                }
            }
            AreaCollection.InsertMany(areaDatas);
        }
    }
}
