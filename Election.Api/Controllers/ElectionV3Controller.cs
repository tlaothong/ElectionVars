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
        IMongoCollection<ScorePollV2> FinalScorePollCollection { get; set; }
        IMongoCollection<ScoreArea> TestTable4Collection { get; set; }
        IMongoCollection<PartyList> FinalPartyScoreCollection { get; set; }

        public ElectionV3Controller()
        {
            var settings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://guntza22:guntza220938@ds026558.mlab.com:26558/electionmana"));
            settings.SslSettings = new SslSettings()
            {
                EnabledSslProtocols = SslProtocols.Tls12
            };
            var mongoClient = new MongoClient(settings);
            var database = mongoClient.GetDatabase("electionmana");
            // ScorePollCsvCollection = database.GetCollection<ScorePollCsv>("ScorePollCsv");
            // ScorePollV2Collection = database.GetCollection<ScorePoll>("ScorePollV2");
            Table4Collection = database.GetCollection<ScoreArea>("Table4");
            // PartyScoreCollection = database.GetCollection<PartyList>("PartyScore");
            // ScorePollV3Collection = database.GetCollection<ScorePollV2>("ScorePollV3");
            // Test
            FinalScorePollCollection = database.GetCollection<ScorePollV2>("FinalScorePoll");
            // TestTable4Collection = database.GetCollection<ScoreArea>("FinalTable4");
            FinalPartyScoreCollection = database.GetCollection<PartyList>("FinalPartyScore");
        }

        [HttpGet]
        public List<ScorePollV2> GetAllScorePoll()
        {
            return FinalScorePollCollection.Find(it => true).ToList();
        }

        [HttpGet("{idArea}")]
        public List<ScorePollV2> GetAreaScorePoll(string idArea)
        {
            var getData = FinalScorePollCollection.Find(it => it.IdArea == idArea.ToUpper()).ToList();
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
            // var getData = Table4Collection.Find(it => true).ToList();
            // var getParty = getData.FirstOrDefault(it => it.Id == id);
            var getParty = Table4Collection.Find(it => it.Id == id).FirstOrDefault();
            getParty.Score = scorePartyModel.Score;
            getParty.StatusEdit = true;
            Table4Collection.ReplaceOne(it => it.Id == getParty.Id, getParty);
            //set status Area Edit
            var getDataUpdate = Table4Collection.Find(it => true).ToList();
            var groupByArea = getDataUpdate.GroupBy(it => it.IdArea).ToList();
            foreach (var item in groupByArea)
            {
                if (item.Key == getParty.IdArea)
                {
                    foreach (var datas in item)
                    {
                        datas.StatusAreaEdit = true;
                        Table4Collection.ReplaceOne(it => it.Id == datas.Id, datas);
                    }
                }
            }
            // set tag
            var getDataUpdate2 = Table4Collection.Find(it => true).ToList();
            var groupByArea2 = getDataUpdate2.GroupBy(it => it.IdArea).ToList();
            var listUpdate = new List<ScoreArea>();
            foreach (var item in groupByArea2)
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
                            listUpdate.Add(data);
                        }
                        else
                        {
                            listUpdate.Add(data);
                        }
                    }
                    else
                    {
                        if (data.Tags.Any(i => i == "ชนะ"))
                        {
                            data.Tags.Remove("ชนะ");
                            data.Tags.Add("แพ้");
                            listUpdate.Add(data);
                        }
                        else
                        {
                            listUpdate.Add(data);
                        }
                    }
                }
            }
            Table4Collection.DeleteMany(it => true);
            Table4Collection.InsertMany(listUpdate);
            //update score Have
            var getDataT42 = Table4Collection.Find(it => true).ToList();
            var listParty = new List<PartyList>();
            var totalScore = getDataT42.Sum(it => it.Score);
            var groupByParty = getDataT42.GroupBy(it => it.IdParty).ToList();
            foreach (var item in groupByParty)
            {
                var percentScoreParty = item.Sum(it => it.Score) * 100.0 / totalScore;
                var haveScore = Math.Round(percentScoreParty / 100.0 * 500);
                var areaScore = item.Count(it => it.Tags.Any(i => i == "ชนะ"));
                var scorePartyList = (haveScore - areaScore >= 0) ? haveScore - areaScore : 0;
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
            FinalPartyScoreCollection.DeleteMany(it => true);
            var sortData = listParty.OrderByDescending(it => it.PercentScore).ToList();
            FinalPartyScoreCollection.InsertMany(sortData);
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
        public List<MyParty> GetMaxScoreAndMyScore()
        {
            var getData = Table4Collection.Find(it => true).ToList().GroupBy(it => it.IdArea);
            var listScore = new List<MyParty>();
            foreach (var item in getData)
            {
                var getWinnerArea = item.FirstOrDefault(it => it.Score == item.Max(i => i.Score));
                var getMyParty = item.FirstOrDefault(it => it.IdParty == "034");
                listScore.Add(new MyParty
                {
                    Id = Guid.NewGuid().ToString(),
                    IdArea = item.Key,
                    NameArea = getMyParty.NameArea,
                    PartyWin = getWinnerArea.NameParty,
                    scoreMax = getWinnerArea.Score,
                    scoreMyParty = getMyParty.Score,
                    StatusAreaEdit = getMyParty.StatusAreaEdit
                });
            }
            var sortData = listScore.OrderBy(it => it.IdArea).ToList();
            return sortData;
        }

        [HttpGet("{idArea}")]
        public List<ScoreArea> GetScoreAreasWithArea(string idArea)
        {
            var getData = Table4Collection.Find(it => it.IdArea == idArea.ToUpper()).ToList().OrderByDescending(it => it.Score).ToList();
            return getData;
        }

        [HttpGet]
        public List<PartyList> GetAllPartyScore()
        {
            return FinalPartyScoreCollection.Find(it => true).ToList().OrderByDescending(it => it.PercentScore).ToList();
        }

        [HttpPost]
        public void UploadFile()
        {
            // Read
            var listScoreCsv = new List<ScorePollCsv>();
            using (var csvReader = new StreamReader(Request.Form.Files.FirstOrDefault().OpenReadStream()))
            {
                while (!csvReader.EndOfStream)
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
                            float.TryParse(getData[4], out float score);
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
            }
            // Fill in ScorePoll
            // FillDataIntoScorePollTable(listScoreCsv);
            var groupByArea = listScoreCsv.GroupBy(it => it.IdArea).ToList();
            var listScorePoll = new List<ScorePollV2>();
            foreach (var getList in groupByArea)
            {
                var totalScore1 = getList.FirstOrDefault(it => it.IdParty == "999").Score;
                foreach (var datas in getList)
                {
                    if (datas.IdParty != "999")
                    {
                        var ScoreParty = Math.Round(datas.Score / 100.0 * totalScore1);
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
            FinalScorePollCollection.InsertMany(listScorePoll);
            //update Score Table 4
            // UpdateTable4();
            var getDataFromScorePoll = FinalScorePollCollection.Find(it => true).ToList();
            var getTable4 = Table4Collection.Find(it => true).ToList();
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
                    getTable4Update.StatusEdit = false;
                    getTable4Update.StatusAreaEdit = false;
                    listTable4.Add(getTable4Update);
                }
            }
            Table4Collection.DeleteMany(it => true);
            Table4Collection.InsertMany(listTable4);
            // Set Tags
            // SetTags();
            var getDataT4 = Table4Collection.Find(it => true).ToList().GroupBy(it => it.IdArea).ToList();
            var listUpdateTag = new List<ScoreArea>();
            foreach (var data in getDataT4)
            {
                var maxScoreOfArea = data.Max(it => it.Score);
                foreach (var item in data)
                {
                    var tagDefault = (item.Score == maxScoreOfArea) ? "ชนะ" : "แพ้";
                    if (item.Score == maxScoreOfArea)
                    {
                        if (item.Tags.Any(i => i != "ชนะ"))
                        {
                            item.Tags.Remove("แพ้");
                            item.Tags.Add("ชนะ");
                            listUpdateTag.Add(item);
                        }
                        else
                        {
                            listUpdateTag.Add(item);
                        }
                    }
                    else
                    {
                        if (item.Tags.Any(i => i == "ชนะ"))
                        {
                            item.Tags.Remove("ชนะ");
                            item.Tags.Add("แพ้");
                            listUpdateTag.Add(item);
                        }
                        else
                        {
                            listUpdateTag.Add(item);
                        }
                    }
                }
            }
            Table4Collection.DeleteMany(it => true);
            Table4Collection.InsertMany(listUpdateTag);
            // update Score Have
            var getDataT42 = Table4Collection.Find(it => true).ToList();
            var listParty = new List<PartyList>();
            var totalScore = getDataT42.Sum(it => it.Score);
            var groupByParty2 = getDataT42.GroupBy(it => it.IdParty).ToList();
            foreach (var item in groupByParty2)
            {
                var percentScoreParty = item.Sum(it => it.Score) * 100.0 / totalScore;
                var haveScore = Math.Round(percentScoreParty / 100.0 * 500);
                var areaScore = item.Count(it => it.Tags.Any(i => i == "ชนะ"));
                var scorePartyList = (haveScore - areaScore >= 0) ? haveScore - areaScore : 0;
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
            FinalPartyScoreCollection.DeleteMany(it => true);
            var sortData = listParty.OrderByDescending(it => it.PercentScore).ToList();
            FinalPartyScoreCollection.InsertMany(sortData);
        }

        [HttpGet]
        public void UpdatePartyScore()
        {
            // var getDataT42 = Table4Collection.Find(it => true).ToList();
            // var listParty = new List<PartyList>();
            // var totalScore = getDataT42.Sum(it => it.Score);
            // var groupByParty = getDataT42.GroupBy(it => it.IdParty).ToList();
            // foreach (var item in groupByParty)
            // {
            //     var percentScoreParty = item.Sum(it => it.Score) * 100.0 / totalScore;
            //     var haveScore = Math.Round(percentScoreParty / 100.0 * 500);
            //     var areaScore = item.Count(it => it.Tags.Any(i => i == "ชนะ"));
            //     var scorePartyList = (haveScore - areaScore >= 0) ? haveScore - areaScore : 0;
            //     var getOneData = item.FirstOrDefault();
            //     listParty.Add(new PartyList
            //     {
            //         Id = Guid.NewGuid().ToString(),
            //         IdParty = getOneData.IdParty,
            //         PartyName = getOneData.NameParty,
            //         TotalScore = haveScore,
            //         AreaScore = areaScore,
            //         NameListScore = scorePartyList,
            //         PercentScore = percentScoreParty
            //     });
            // }
            // FinalPartyScoreCollection.DeleteMany(it => true);
            // var sortData = listParty.OrderByDescending(it => it.PercentScore).ToList();
            // FinalPartyScoreCollection.InsertMany(sortData);
        }

        // [HttpPost]
        // public void FillDataIntoScorePollTable([FromBody]List<ScorePollCsv> listScoreCsv)
        // {
        //     var groupByArea = listScoreCsv.GroupBy(it => it.IdArea).ToList();
        //     var listScorePoll = new List<ScorePollV2>();
        //     foreach (var getList in groupByArea)
        //     {
        //         var totalScore = getList.FirstOrDefault(it => it.IdParty == "999").Score;
        //         foreach (var datas in getList)
        //         {
        //             if (datas.IdParty != "999")
        //             {
        //                 var ScoreParty = Math.Round(datas.Score / 100.0 * totalScore);
        //                 listScorePoll.Add(new ScorePollV2
        //                 {
        //                     Id = datas.Id,
        //                     IdParty = datas.IdParty,
        //                     NameParty = datas.NameParty,
        //                     IdArea = datas.IdArea,
        //                     NameArea = datas.NameArea,
        //                     datePoll = DateTime.Now,
        //                     Score = ScoreParty,
        //                     PercentScore = datas.Score,
        //                     Source = "Poll"
        //                 });
        //             }
        //         }
        //     }
        //     FinalScorePollCollection.InsertMany(listScorePoll);
        // }

        // [HttpPost]
        // public void UpdateTable4()
        // {
        //     var getDataFromScorePoll = FinalScorePollCollection.Find(it => true).ToList();
        //     var getTable4 = Table4Collection.Find(it => true).ToList();
        //     var listTable4 = new List<ScoreArea>();
        //     var groupByAreaTable4 = getDataFromScorePoll.GroupBy(it => it.IdArea).ToList();
        //     foreach (var item in groupByAreaTable4)
        //     {
        //         var groupByParty = item.GroupBy(it => it.IdParty).ToList();
        //         foreach (var datas in groupByParty)
        //         {
        //             var getCurrentData = datas.OrderByDescending(it => it.datePoll).FirstOrDefault();
        //             var getTable4Update = getTable4.FirstOrDefault(it => it.IdArea == getCurrentData.IdArea && it.IdParty == getCurrentData.IdParty);
        //             getTable4Update.Score = getCurrentData.Score;
        //             listTable4.Add(getTable4Update);
        //             // Table4Collection.ReplaceOne(it => it.Id == getTable4Update.Id, getTable4Update);
        //         }
        //     }
        //     Table4Collection.DeleteMany(it => true);
        //     Table4Collection.InsertMany(listTable4);
        // }

        // [HttpPost]
        // public void SetTags()
        // {
        //     var getData = Table4Collection.Find(it => true).ToList().GroupBy(it => it.IdArea).ToList();
        //     foreach (var data in getData)
        //     {
        //         var maxScoreOfArea = data.Max(it => it.Score);
        //         foreach (var item in data)
        //         {
        //             var tagDefault = (item.Score == maxScoreOfArea) ? "ชนะ" : "แพ้";
        //             var updateTag = new ScoreArea
        //             {
        //                 Id = item.Id,
        //                 IdArea = item.IdArea,
        //                 NameArea = item.NameArea,
        //                 IdParty = item.IdParty,
        //                 NameParty = item.NameParty,
        //                 NoRegister = item.NoRegister,
        //                 NameRegister = item.NameRegister,
        //                 Status = item.Status,
        //                 NameInitial = item.IdArea,
        //                 Tags = { tagDefault },
        //                 Score = item.Score,
        //                 Source = item.Source,
        //                 StatusEdit = false,
        //             };
        //             Table4Collection.ReplaceOne(it => it.Id == item.Id, updateTag);
        //         }
        //     }
        // }
    }
}


