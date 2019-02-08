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
    public class MockElectionV2Controller : Controller
    {
        IMongoCollection<ScorePollCsv> ScorePollCsvCollection { get; set; }
        IMongoCollection<ScorePoll> ScorePollV2Collection { get; set; }
        IMongoCollection<ScoreArea> Table4Collection { get; set; }
        IMongoCollection<PartyList> PartyScoreCollection { get; set; }
        IMongoCollection<ScorePollV2> ScorePollV3Collection { get; set; }

        public MockElectionV2Controller()
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
        }

        [HttpPost]
        public void MockPrototypeTableScorePollCsv()
        {
            var readerCsv = new ReadCsv();
            var getFromCsv = readerCsv.MockDataScorePoll();
            var rnd = new Random();
            var list = new List<ScorePollCsv>();
            foreach (var item in getFromCsv)
            {
                if (item.IdParty != "999")
                {
                    item.Score = rnd.Next(25000, 55000);
                    list.Add(item);
                }
                else
                {
                    list.Add(item);
                }
            }
            var groupByArea = list.GroupBy(it => it.IdArea).ToList();
            var listScorePollCsv = new List<ScorePollCsv>();
            foreach (var item in groupByArea)
            {
                var TotalGoodScore = item.Sum(it => it.Score);
                foreach (var data in item)
                {
                    if (data.IdParty == "999")
                    {
                        data.Score = TotalGoodScore;
                        listScorePollCsv.Add(data);
                    }
                    else
                    {
                        listScorePollCsv.Add(data);
                    }
                }
            }
            ScorePollCsvCollection.InsertMany(listScorePollCsv);
        }

        [HttpPost]
        public void MockPrototypeTableScorePoll()
        {
            var getData = ScorePollV3Collection.Find(it => true).ToList();
            var rnd = new Random();
            var listScorePoll = new List<ScorePollV2>();
            foreach (var item in getData)
            {
                if (item.IdParty != "999")
                {
                    if (item.IdParty != "000" && item.IdParty != "888")
                    {
                        var randomDiff = rnd.Next(500, 3500);
                        var ScoreTarget = (rnd.Next(0, 1) == 0) ? item.Score + randomDiff : item.Score - randomDiff;
                        listScorePoll.Add(new ScorePollV2
                        {
                            Id = Guid.NewGuid().ToString(),
                            IdParty = item.IdParty,
                            IdArea = item.IdArea,
                            datePoll = new DateTime(2019, 1, 1),
                            Score = item.Score,
                            Source = "Poll",
                            TargetScoreDefault = ScoreTarget,
                            TargetScore = ScoreTarget
                        });
                    }
                    else
                    {
                        listScorePoll.Add(new ScorePollV2
                        {
                            Id = Guid.NewGuid().ToString(),
                            IdParty = item.IdParty,
                            IdArea = item.IdArea,
                            datePoll = new DateTime(2019, 1, 1),
                            Score = item.Score,
                            Source = "Poll",
                            TargetScoreDefault = 0,
                            TargetScore = 0
                        });
                    }
                }
            }
            ScorePollV3Collection.InsertMany(listScorePoll);
        }

        [HttpPost]
        public void MockPrototypeScoreTable4()
        {
            var getData = ScorePollV3Collection.Find(it => true).ToList();
            var groupByArea = getData.GroupBy(it => it.IdArea).ToList();
            var listScoreTable4 = new List<ScoreArea>();
            foreach (var item in groupByArea)
            {
                var groupByParty = item.GroupBy(it => it.IdParty).ToList();
                foreach (var data in groupByParty)
                {
                    if (data.Key != "000" && data.Key != "888")
                    {
                        var getCurrentScore = data.OrderByDescending(it => it.datePoll).ToList().FirstOrDefault();
                        listScoreTable4.Add(new ScoreArea
                        {
                            Id = Guid.NewGuid().ToString(),
                            IdArea = getCurrentScore.IdArea,
                            IdParty = getCurrentScore.IdParty,
                            Score = getCurrentScore.Score,
                            Source = getCurrentScore.Source
                        });
                    }
                }
            }
            Table4Collection.DeleteMany(it => true);
            Table4Collection.InsertMany(listScoreTable4);
        }

        [HttpPost]
        public void MatchDataWithTable2()
        {
            var readerCsv = new ReadCsv();
            var getTable2 = readerCsv.MockPrototypeDataTable2();
            var getData = Table4Collection.Find(it => true).ToList();
            var listTable4 = new List<ScoreArea>();
            foreach (var item in getData)
            {
                var getMatch = getTable2.FirstOrDefault(it => it.IdArea == item.IdArea
                && it.IdParty == item.IdParty);
                item.NameArea = getMatch.NameArea;
                item.NameInitial = getMatch.NameInitial;
                item.NameParty = getMatch.NameParty;
                item.NameRegister = getMatch.NameRegister;
                item.NoRegister = getMatch.NoRegister;
                item.Status = true;
                listTable4.Add(item);
            }
            Table4Collection.DeleteMany(it => true);
            Table4Collection.InsertMany(listTable4);
        }

        [HttpPost]
        public void SetTag()
        {
            var getData = Table4Collection.Find(it => true).ToList();
            var groupByArea = getData.GroupBy(it => it.IdArea).ToList();
            var listScoreArea = new List<ScoreArea>();
            foreach (var item in groupByArea)
            {
                var listMaxScore = item.Where(it => it.Score == item.Max(i => i.Score)).ToList();
                var getMaxScore = listMaxScore.FirstOrDefault();
                foreach (var data in item)
                {
                    if (data.Id == getMaxScore.Id)
                    {
                        data.Tags = new List<string>{
                            "ชนะ"
                        };
                        listScoreArea.Add(data);
                    }
                    else
                    {
                        data.Tags = new List<string>{
                            "แพ้"
                        };
                        listScoreArea.Add(data);
                    }
                }
            }
            Table4Collection.DeleteMany(it => true);
            Table4Collection.InsertMany(listScoreArea);
        }

        [HttpPost]
        public void MockDataPartyScore()
        {
            var getData = Table4Collection.Find(it => true).ToList();
            var totalScore = getData.Sum(it => it.Score);
            var groupByParty = getData.GroupBy(it => it.IdParty).ToList();
            var listScoreParty = new List<PartyList>();
            foreach (var item in groupByParty)
            {
                var totalScoreParty = item.Sum(it => it.Score);
                var percentScore = totalScoreParty * 100 / totalScore;
                var scoreHave = Math.Round(percentScore / 100.0 * 500);
                var scoreArea = item.Count(it => it.Tags.Any(i => i == "ชนะ"));
                var scorePartyList = scoreHave - scoreArea;
                listScoreParty.Add(new PartyList
                {
                    Id = Guid.NewGuid().ToString(),
                    IdParty = item.Key,
                    PartyName = item.FirstOrDefault(it => it.IdParty == item.Key).NameParty,
                    TotalScore = scoreHave,
                    AreaScore = scoreArea,
                    NameListScore = scorePartyList,
                    PercentScore = percentScore
                });
            }
            PartyScoreCollection.DeleteMany(it => true);
            PartyScoreCollection.InsertMany(listScoreParty.OrderByDescending(it => it.PercentScore));
        }

        [HttpGet]
        public PartyList CheckSumPartyScore()
        {
            var getData = PartyScoreCollection.Find(it => true).ToList();
            var sumHaveScore = getData.Sum(it => it.TotalScore);
            var sumAreaScore = getData.Sum(it => it.AreaScore);
            var sumPartyListScore = getData.Sum(it => it.NameListScore);
            var sumPercentScore = getData.Sum(it => it.PercentScore);
            var partyScore = new PartyList
            {
                TotalScore = sumHaveScore,
                AreaScore = sumAreaScore,
                NameListScore = sumPartyListScore,
                PercentScore = sumPercentScore
            };
            return partyScore;
        }

        [HttpPost]
        public void MockPrototypeTableScorePollV2()
        {
            var getData = ScorePollCsvCollection.Find(it => true).ToList();
            var rnd = new Random();
            var listScorePoll = new List<ScorePollV2>();
            foreach (var item in getData)
            {
                if (item.IdParty != "999")
                {
                    if (item.IdParty != "000" && item.IdParty != "888")
                    {
                        var randomDiff = rnd.Next(500, 3500);
                        var ScoreTarget = (rnd.Next(0, 1) == 0) ? item.Score + randomDiff : item.Score - randomDiff;
                        listScorePoll.Add(new ScorePollV2
                        {
                            Id = Guid.NewGuid().ToString(),
                            IdParty = item.IdParty,
                            NameParty = item.NameParty,
                            IdArea = item.IdArea,
                            NameArea = item.NameArea,
                            datePoll = new DateTime(2019, 1, 1),
                            Score = item.Score,
                            Source = "Poll",
                            TargetScoreDefault = ScoreTarget,
                            TargetScore = ScoreTarget
                        });
                    }
                    else
                    {
                        listScorePoll.Add(new ScorePollV2
                        {
                            Id = Guid.NewGuid().ToString(),
                            IdParty = item.IdParty,
                            NameParty = item.NameParty,
                            IdArea = item.IdArea,
                            NameArea = item.NameArea,
                            datePoll = new DateTime(2019, 1, 1),
                            Score = item.Score,
                            Source = "Poll",
                            TargetScoreDefault = 0,
                            TargetScore = 0
                        });
                    }
                }
            }
            ScorePollV3Collection.InsertMany(listScorePoll);
        }

        [HttpPost]
        public void AddPercentScorePoll()
        {
            var getData = ScorePollV3Collection.Find(it => true).ToList().GroupBy(it => it.IdArea);
            var listNewScorePoll = new List<ScorePollV2>();
            foreach (var item in getData)
            {
                var groupByArea = item.GroupBy(it => it.IdParty).ToList();
                var listCurrent = new List<ScorePollV2>();
                foreach (var dataParty in groupByArea)
                {
                    var getCuurent = dataParty.OrderByDescending(it => it.datePoll).ToList().FirstOrDefault();
                    listCurrent.Add(getCuurent);
                }
                var totalScoreArea = listCurrent.Sum(it => it.Score);
                foreach (var data in listCurrent)
                {
                    var percent = data.Score * 100.0 / totalScoreArea;
                    listNewScorePoll.Add(new ScorePollV2
                    {
                        Id = data.Id,
                        IdParty = data.IdParty,
                        NameParty = data.NameParty,
                        IdArea = data.IdArea,
                        NameArea = data.NameArea,
                        datePoll = data.datePoll,
                        Score = data.Score,
                        PercentScore = percent,
                        Source = data.Source,
                        TargetScoreDefault = data.TargetScoreDefault,
                        TargetScore = data.TargetScore
                    });
                }
            }
            ScorePollV3Collection.DeleteMany(it => true);
            ScorePollV3Collection.InsertMany(listNewScorePoll);
        }
    }
}
