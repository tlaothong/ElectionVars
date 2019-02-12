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
    public class ElectionV3Controller : Controller
    {
        IMongoCollection<ScorePollCsv> ScorePollCsvCollection { get; set; }
        IMongoCollection<ScorePoll> ScorePollV2Collection { get; set; }
        IMongoCollection<ScoreArea> Table4Collection { get; set; }
        IMongoCollection<PartyList> PartyScoreCollection { get; set; }
        IMongoCollection<ScorePollV2> ScorePollV3Collection { get; set; }
        // test
        IMongoCollection<ScorePollV2> ScorePollV4Collection { get; set; }
        IMongoCollection<ScoreArea> TestTable4Collection { get; set; }
        IMongoCollection<PartyList> TestPartyScoreCollection { get; set; }

        public ElectionV3Controller()
        {
            var settings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://guntza22:guntza220938@ds026558.mlab.com:26558/electionmana"));
            settings.SslSettings = new SslSettings()
            {
                EnabledSslProtocols = SslProtocols.Tls12
            };
            var mongoClient = new MongoClient(settings);
            var database = mongoClient.GetDatabase("electionmana");
            ScorePollCsvCollection = database.GetCollection<ScorePollCsv>("ScorePollCsv");
            ScorePollV2Collection = database.GetCollection<ScorePoll>("ScorePollV2");
            Table4Collection = database.GetCollection<ScoreArea>("Table4");
            PartyScoreCollection = database.GetCollection<PartyList>("PartyScore");
            ScorePollV3Collection = database.GetCollection<ScorePollV2>("ScorePollV3");
            // Test
            ScorePollV4Collection = database.GetCollection<ScorePollV2>("ScorePollV4");
            TestTable4Collection = database.GetCollection<ScoreArea>("TestTable4");
            TestPartyScoreCollection = database.GetCollection<PartyList>("TestPartyScore");
        }

        [HttpGet]
        public List<ScorePollV2> GetAllScorePoll()
        {
            return ScorePollV4Collection.Find(it => true).ToList();
        }

        [HttpGet("{idArea}")]
        public List<ScorePollV2> GetAreaScorePoll(string idArea)
        {
            var getData = ScorePollV4Collection.Find(it => it.IdArea == idArea.ToUpper()).ToList();
            var groupByParty = getData.GroupBy(it => it.IdParty);
            var listCurrentScorePollOfArea = new List<ScorePollV2>();
            foreach (var item in groupByParty)
            {
                var getCurrent = item.OrderByDescending(it => it.datePoll).ToList().FirstOrDefault();
                listCurrentScorePollOfArea.Add(getCurrent);
            }
            return listCurrentScorePollOfArea;
        }

        [HttpGet]
        public List<ScoreArea> GetAllScoreTable4()
        {
            var getData = Table4Collection.Find(it => true).ToList();
            return getData;
        }


        [HttpPost("{id}")]
        public void EditScore([FromBody]ScoreArea scorePartyModel, string id)
        {
            var getData = Table4Collection.Find(it => true).ToList();
            var getParty = getData.FirstOrDefault(it => it.Id == id);
            getParty.Score = scorePartyModel.Score;
            Table4Collection.ReplaceOne(it => it.Id == getParty.Id, getParty);

            var getDataUpdate = Table4Collection.Find(it => true).ToList();
            var groupByArea = getData.GroupBy(it => it.IdArea).ToList();
            var listUpdate = new List<ScoreArea>();
            foreach (var item in groupByArea)
            {
                var maxScore = item.Max(it => it.Score);
                foreach (var data in item)
                {
                    if (data.Score == maxScore)
                    {
                        if (data.Tags.Any(i => i != "ชนะ"))
                        {
                            data.Tags.Remove("แพ้");
                            data.Tags.Add("ชนะ");
                            data.StatusEdit = true;
                            listUpdate.Add(data);
                        }
                        else
                        {
                            data.StatusEdit = true;
                            listUpdate.Add(data);
                        }
                    }
                    else
                    {
                        if (data.Tags.Any(i => i == "ชนะ"))
                        {
                            data.Tags.Remove("ชนะ");
                            data.Tags.Add("แพ้");
                            data.StatusEdit = true;
                            listUpdate.Add(data);
                        }
                        else
                        {
                            data.StatusEdit = true;
                            listUpdate.Add(data);
                        }
                    }
                }
            }
            Table4Collection.DeleteMany(it => true);
            Table4Collection.InsertMany(listUpdate);
        }

        [HttpGet]
        public List<ScoreArea> GetAllArea()
        {
            var getData = Table4Collection.Find(it => true).ToList().GroupBy(it => it.IdArea).ToList();
            var listArea = new List<ScoreArea>();
            foreach (var item in getData)
            {
                var getArea = item.FirstOrDefault(it => it.IdArea == item.Key);
                listArea.Add(getArea);
            }
            return listArea.OrderBy(it => it.IdArea).ToList();
        }

        [HttpGet("{idParty}")]
        public List<ScoreArea> GetAreaWinScoreParty(string idParty)
        {
            var getData = Table4Collection.Find(it => it.IdParty == idParty).ToList()
            .Where(it => it.Tags.Any(i => i == "ชนะ")).ToList();
            return getData;
        }

        [HttpGet]
        public List<ScoreArea> GetAllAreaMaxScore()
        {
            var getData = Table4Collection.Find(it => true).ToList().GroupBy(it => it.IdArea);
            var listWinnerArea = new List<ScoreArea>();
            foreach (var item in getData)
            {
                var getWinnerArea = item.FirstOrDefault(it => it.Score == item.Max(i => i.Score));
                listWinnerArea.Add(getWinnerArea);
            }
            return listWinnerArea.OrderBy(it => it.IdArea).ToList();
        }

        [HttpGet("{idArea}")]
        public List<ScoreArea> GetScoreAreasWithArea(string idArea)
        {
            var getData = Table4Collection.Find(it => it.IdArea == idArea.ToUpper()).ToList();
            return getData;
        }

        [HttpGet]
        public List<PartyList> GetAllPartyScore()
        {
            return PartyScoreCollection.Find(it => true).ToList().OrderByDescending(it => it.PercentScore).ToList();
        }

        // [HttpPost("{id}")]
        // public void Edititem([FromBody]ScoreArea model, string id)
        // {
        //     var data = Table4Collection.Find(x => x.Id == id).FirstOrDefault();
        //     data.Score = model.Score;

        //     Table4Collection.ReplaceOne(x => x.Id == data.Id, data);
        // }

        [HttpPost]
        public void UploadFile()
        {
            // Read
            var listScoreCsv = new List<ScorePollCsv>();
            using (var csvReader = new StreamReader(Request.Form.Files.FirstOrDefault().OpenReadStream()))
            {
                var getFormCsv = csvReader.ReadLine();
                var getLine = getFormCsv.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var item in getLine)
                {
                    var getData = item.Split(',').ToList();
                    if (getData[0] != "รหัสพรรค" && getData[1] != "ชื่อเขต" &&
                    getData[2] != "รหัสเขต " && getData[3] != "ชื่อพรรค" && getData[4] != "เปอร์เซ็น/คะแนน"
                    && getData[5] != "ภูมิภาค")
                    {
                        Double.TryParse(getData[4], out Double score);
                        listScoreCsv.Add(new ScorePollCsv
                        {
                            Id = Guid.NewGuid().ToString(),
                            IdParty = getData[0],
                            NameParty = getData[3],
                            IdArea = getData[2],
                            NameArea = getData[1],
                            Score = score,
                            Region = getData[5]
                        });
                    }
                }
            }
            // Fill in ScorePoll
            var groupByArea = listScoreCsv.GroupBy(it => it.IdArea).ToList();
            var listScorePoll = new List<ScorePollV2>();
            foreach (var getList in groupByArea)
            {
                var totalScore = getList.FirstOrDefault(it => it.IdParty == "999").Score;
                foreach (var datas in getList)
                {
                    if (datas.IdParty != "999")
                    {
                        var ScoreParty = datas.Score / 100.0 * totalScore;
                        listScorePoll.Add(new ScorePollV2
                        {
                            Id = datas.Id,
                            IdParty = datas.IdParty,
                            NameParty = datas.NameParty,
                            IdArea = datas.IdArea,
                            NameArea = datas.NameArea,
                            datePoll = DateTime.Now,
                            Score = ScoreParty,
                            PercentScore = datas.Score,
                            Source = "Poll"
                        });
                    }
                }
            }
            ScorePollV4Collection.InsertMany(listScorePoll);
            //update Score Table 4
            var getDataFromScorePoll = ScorePollV4Collection.Find(it => true).ToList();
            var getTable4 = TestTable4Collection.Find(it => true).ToList();
            var listTable4 = new List<ScoreArea>();
            var groupByAreaTable4 = getDataFromScorePoll.GroupBy(it => it.IdArea).ToList();
            foreach (var item in groupByAreaTable4)
            {
                var groupByParty = item.GroupBy(it => it.IdParty).ToList();
                foreach (var datas in groupByParty)
                {
                    var getCurrentData = datas.OrderByDescending(it => it.datePoll).FirstOrDefault();
                    var getTable4Update = getTable4.FirstOrDefault(it => it.IdArea == getCurrentData.IdArea && it.IdParty == getCurrentData.IdParty);
                    getTable4Update.Score = getCurrentData.Score;
                    TestTable4Collection.ReplaceOne(it => it.Id == getTable4Update.Id, getTable4Update);
                }
            }
            //update Show Score Party
        }

        [HttpPost]
        public void SetTags()
        {
            var getData = TestTable4Collection.Find(it => true).ToList().GroupBy(it => it.IdArea).ToList();
            foreach (var data in getData)
            {
                var maxScoreOfArea = data.Max(it => it.Score);
                foreach (var item in data)
                {
                    var tagDefault = (item.Score == maxScoreOfArea) ? "ชนะ" : "แพ้";
                    var updateTag = new ScoreArea
                    {
                        Id = item.Id,
                        IdArea = item.IdArea,
                        NameArea = item.NameArea,
                        IdParty = item.IdParty,
                        NameParty = item.NameParty,
                        NoRegister = item.NoRegister,
                        NameRegister = item.NameRegister,
                        Status = item.Status,
                        NameInitial = item.IdArea,
                        Tags = { tagDefault },
                        Score = item.Score,
                        Source = item.Source,
                        StatusEdit = false,
                    };
                    TestTable4Collection.ReplaceOne(it => it.Id == item.Id, updateTag);
                }
            }
        }

        [HttpPost]
        public void TestDataScoreParty()
        {
            var getData = TestTable4Collection.Find(it => true).ToList();
            var listParty = new List<PartyList>();
            var totalScore = getData.Sum(it => it.Score);
            var groupByParty = getData.GroupBy(it => it.IdParty).ToList();
            foreach (var item in groupByParty)
            {
                var percentScoreParty = item.Sum(it => it.Score) * 100.0 / totalScore;
                var haveScore = Math.Round(percentScoreParty / 100.0 * 500);
                var areaScore = item.Count(it => it.Tags.Any(i => i == "ชนะ"));
                var scorePartyList = haveScore - areaScore;
                var getOneData = item.FirstOrDefault();
                listParty.Add(new PartyList
                {
                    Id = Guid.NewGuid().ToString(),
                    IdParty = getOneData.IdParty,
                    PartyName = getOneData.NameParty,
                    TotalScore = haveScore,
                    AreaScore = areaScore,
                    NameListScore = scorePartyList,
                    PercentScore = percentScoreParty
                });
            }
            var sortData = listParty.OrderByDescending(it => it.PercentScore).ToList();
            TestPartyScoreCollection.InsertMany(sortData);
        }
    }
}


