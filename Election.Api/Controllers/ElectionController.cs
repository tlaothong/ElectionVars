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
        IMongoCollection<DataTable2> DataTable2Collection { get; set; }

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
            // Use now
            PartyScoreColloection = database.GetCollection<PartyScore>("PartyScore");
            AreaCollection = database.GetCollection<AreaData>("AreaTest");
            DataTable2Collection = database.GetCollection<DataTable2>("Table2");
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

        [HttpPost("{id}")]
        public void AddScoreElection(string id, [FromBody]DataScore addScore)
        {
            var getArea = AreaCollection.Find(it => it.Id == id).FirstOrDefault();

            addScore.DateElection = DateTime.Now;
            getArea.ScoreReceive.Add(new DataScore
            {
                DateElection = addScore.DateElection,
                Score = addScore.Score
            });
            AreaCollection.ReplaceOne(it => it.Id == id, getArea);
        }

        [HttpGet("{id}")]
        public AreaData GetAreaData(string id)
        {
            var getArea = AreaCollection.Find(it => it.Id == id).FirstOrDefault();
            return getArea;
        }

        [HttpGet("{nameArea}")]
        public List<DataTable2> getArea(string nameArea)
        {
            return DataTable2Collection.Find(it => it.NameArea == nameArea).ToList();
        }

        [HttpGet]
        public List<DataTable2> GetAllTable2()
        {
            var getData = DataTable2Collection.Find(it => true).ToList();
            return getData;
        }

        [HttpGet("{tag}")]
        public List<DataTable2> FilterTagTable2(string tag)
        {
            var getData = DataTable2Collection.Find(it => it.NameParty == "เพื่อไทย" && it.Tag == tag).ToList();
            return getData;
        }

        [HttpPost]
        public void AddPollScore([FromBody]List<DataTable2> getDataArea)
        {
            foreach (var item in getDataArea)
            {
                DataTable2Collection.ReplaceOne(it => it.Id == item.Id, item);
            }
        }

        //Get max Score
        [HttpGet]
        public List<DataTable2> GetMaxScoreArea()
        {
            var getData = DataTable2Collection.Find(it => true).ToList().OrderBy(it => it.IDProvince).OrderBy(it => it.LocationCode.Substring(0, 2));
            var groupByArea = getData.GroupBy(it => it.NameArea).ToList();
            var listMax = new List<DataTable2>();
            foreach (var item in groupByArea)
            {
                var getMaxScore = item.FirstOrDefault(it => it.Score == item.Max(i => i.Score));
                listMax.Add(getMaxScore);
            }
            return listMax;
        }

        [HttpGet]
        public List<string> GetAreaAll()
        {
            var getData = AreaCollection.Find(it => true).ToList().OrderBy(it => it.IDProvince).OrderBy(it => it.LocationCode.Substring(0, 2));
            var groupByArea = getData.GroupBy(it => it.NameArea).ToList();
            var listAreaName = new List<string>();
            foreach (var item in groupByArea)
            {
                listAreaName.Add(item.Key);
            }
            return listAreaName;
        }

        [HttpGet("{nameArea}")]
        public List<AreaData> GetDistrictAll(string nameArea)
        {
            var getData = AreaCollection.Find(it => it.NameArea == nameArea).ToList();
            var listDistrict = getData.Where(it => it.NameParty == "เพื่อไทย").ToList();
            return listDistrict;
        }

        [HttpGet]
        public List<PartyScore> GetAllPartyScore()
        {
            var getData = PartyScoreColloection.Find(it => true).ToList();
            return getData;
        }
    }
}
