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

        // ---------------------------------------------------------------------------------------------------------------------------------------

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
                    data.ScoreReceive = new List<DataScore>();
                }
                else
                {
                    data.Status = false;
                }
            }
            AreaCollection.InsertMany(areaDatas);
        }

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

        [HttpPost]
        public void AddMockScore()
        {
            var getArea = AreaCollection.Find(it => true).ToList();
            var rnd = new Random();
            foreach (var item in getArea)
            {
                item.ScoreReceive.Add(new DataScore
                {
                    DateElection = new DateTime(2019, 1, 28),
                    Score = rnd.Next(1000, 3000)
                });
            }
            AreaCollection.DeleteMany(it => true);
            AreaCollection.InsertMany(getArea);
        }

        //Table 2
        [HttpPost]
        public void MockDataTable2()
        {
            var getDataTable3 = AreaCollection.Find(it => true).ToList().OrderBy(it => it.IDProvince).OrderBy(it => it.LocationCode.Substring(0, 2));
            var dataGroupByNameArea = getDataTable3.GroupBy(it => it.NameArea).ToList();
            var listDataTable2 = new List<DataTable2>();
            var rnd = new Random();
            foreach (var item in dataGroupByNameArea)
            {
                var dataGroupByNameParty = item.GroupBy(it => it.NameParty).ToList();
                foreach (var datas in dataGroupByNameParty)
                {
                    var sumScore = datas.Sum(it => it.ScoreReceive.Last().Score);
                    var data = datas.FirstOrDefault();
                    listDataTable2.Add(new DataTable2
                    {
                        Id = Guid.NewGuid().ToString(),
                        NameArea = data.NameArea,
                        IDProvince = data.IDProvince,
                        LocationCode = data.LocationCode,
                        NameParty = data.NameParty,
                        NoRegister = data.NoRegister,
                        NameRegister = data.NameRegister,
                        Status = data.Status,
                        Score = sumScore,
                        TargetScore = sumScore + rnd.Next(-1500, 1500)
                    });
                }
            }
            DataTable2Collection.InsertMany(listDataTable2);
        }

        [HttpGet("{nameArea}")]
        public List<DataTable2> getArea(string nameArea)
        {
            return DataTable2Collection.Find(it => it.NameArea == nameArea).ToList();
        }

        [HttpPost]
        public void AddInitialName()
        {
            var getData = DataTable2Collection.Find(it => true).ToList();
            foreach (var item in getData)
            {
                switch (item.NameParty)
                {
                    case "เพื่อไทย":
                        item.InitialParty = "พท.";
                        break;
                    case "ประชาธิปัตย์":
                        item.InitialParty = "ปชป.";
                        break;
                    case "พลังประชารัฐ":
                        item.InitialParty = "พปชร.";
                        break;
                    case "อนาคตใหม่":
                        item.InitialParty = "อ.น.ค.";
                        break;
                    case "ภูมิใจไทย":
                        item.InitialParty = "ภท.";
                        break;
                    case "เสรีรวมไทย":
                        item.InitialParty = "สร.";
                        break;
                    case "พรรคไทยรักษาชาติ":
                        item.InitialParty = "ทษช.";
                        break;
                    case "รวมพลังประชาชาติไทย":
                        item.InitialParty = "รปช.";
                        break;
                    default:
                        break;
                }
            }
            DataTable2Collection.DeleteMany(it => true);
            DataTable2Collection.InsertMany(getData);
        }

        [HttpPost]
        public void AddTag()
        {
            var getData = DataTable2Collection.Find(it => true).ToList();
            var dataGroupByNameArea = getData.GroupBy(it => it.NameArea);
            foreach (var item in dataGroupByNameArea)
            {
                var maxScore = item.Max(it => it.Score);
                foreach (var data in item)
                {
                    data.Tag = (data.Score == maxScore) ? "ชนะ" : "แพ้";
                }
            }
            DataTable2Collection.DeleteMany(it => true);
            DataTable2Collection.InsertMany(getData);
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

        [HttpPost]
        public void GetTotalScoreOfPartry()
        {
            var getDataFromTable2 = DataTable2Collection.Find(it => true).ToList();
            var total = 0.0;
            foreach (var item in getDataFromTable2)
            {
                total += item.Score;
            }
            var scorePerRegister = total / 500;

            var groupByParty = getDataFromTable2.GroupBy(it => it.NameParty).ToList();
            var listPartyScore = new List<PartyScore>();


            foreach (var item in groupByParty)
            {

                var percentScore = item.Sum(it => it.Score) / scorePerRegister * 100 / 500;
                var totalScore = percentScore / 100 * 500;
                var areaScore = 0;
                foreach (var scoreParty in item)
                {
                    areaScore += (scoreParty.Tag == "ชนะ") ? 1 : 0;
                }
                listPartyScore.Add(new PartyScore
                {
                    Id = Guid.NewGuid().ToString(),
                    PartyName = item.Key,
                    TotalScore = totalScore,
                    AreaScore = areaScore,
                    NameListScore = totalScore - areaScore,
                    PercentScore = percentScore
                });
            }
            PartyScoreColloection.InsertMany(listPartyScore);
        }

        [HttpGet]
        public List<double> getTotalScore()
        {
            var getDataFromTable2 = DataTable2Collection.Find(it => true).ToList();
            var total = 0.0;
            var listDouble = new List<Double>();
            foreach (var item in getDataFromTable2)
            {
                total += item.Score;
            }
            var scorePerRegister = total / 500;

            var groupByParty = getDataFromTable2.GroupBy(it => it.NameParty).ToList();
            var percentScore = 0.0;
            foreach (var item in groupByParty)
            {
                percentScore = item.Sum(it => it.Score);
                listDouble.Add(percentScore);
            }
            return listDouble;
        }

        [HttpGet]
        public List<PartyScore> GetAllPartyScore()
        {
            var getData = PartyScoreColloection.Find(it => true).ToList();
            return getData;
        }
    }
}
