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
    public class ElectionV2Controller : Controller
    {
        IMongoCollection<ScorePoll> ScorePollCollection { get; set; }
        IMongoCollection<ScoreArea> ScoreAreaCollection { get; set; }
        IMongoCollection<PartyList> PartyScoreCollection { get; set; }
        IMongoCollection<ScorePollV2> ScorePollV3Collection { get; set; }

        public ElectionV2Controller()
        {
            var settings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://guntza22:guntza220938@ds026558.mlab.com:26558/electionmana"));
            settings.SslSettings = new SslSettings()
            {
                EnabledSslProtocols = SslProtocols.Tls12
            };
            var mongoClient = new MongoClient(settings);
            var database = mongoClient.GetDatabase("electionmana");
            ScorePollCollection = database.GetCollection<ScorePoll>("ScorePoll");
            ScoreAreaCollection = database.GetCollection<ScoreArea>("ScoreArea");
            PartyScoreCollection = database.GetCollection<PartyList>("PartyListScore");
            ScorePollV3Collection = database.GetCollection<ScorePollV2>("ScorePollV3");
        }

        [HttpPost]
        public void MockDataScorePoll()
        {
            var readerCsv = new ReadCsv();
            var rnd = new Random();
            var listScorePoll = new List<ScorePollV2>();
            var dataPollCsv = readerCsv.MockDataScorePoll();
            var groupByArea = dataPollCsv.OrderBy(it => it.IdArea).GroupBy(it => it.IdArea).ToList();
            var goodScoreDefault = 0.0;
            foreach (var item in groupByArea)
            {
                foreach (var data in item)
                {
                    if (data.NameParty == "บัตรดี")
                    {
                        goodScoreDefault = data.Score;
                    }
                    else
                    {
                        var scorePolls = Math.Round(data.Score * goodScoreDefault / 100.0);
                        var scoreTarget = (rnd.Next(0, 1) == 0) ? scorePolls + rnd.Next(1000, 2000) : scorePolls - rnd.Next(1000, 2000);
                        listScorePoll.Add(new ScorePollV2
                        {
                            Id = Guid.NewGuid().ToString(),
                            IdParty = data.IdParty,
                            IdArea = data.IdArea,
                            datePoll = new DateTime(2019, 1, 22),
                            Score = scorePolls,
                            Source = "poll",
                            TargetScoreDefault = scoreTarget,
                            TargetScore = scoreTarget
                        });
                    }
                }
            }
            ScorePollV3Collection.DeleteMany(it => true);
            ScorePollV3Collection.InsertMany(listScorePoll);
        }

        [HttpPost]
        public void MockDataTable4()
        {
            var readerCsv = new ReadCsv();
            var dataPrototypeTable2 = readerCsv.MockPrototypeDataTable2().OrderBy(it => it.IdArea).ToList();
            ScoreAreaCollection.InsertMany(dataPrototypeTable2);
        }

        [HttpPost]
        public void MockScoreParty()
        {
            var getData = ScoreAreaCollection.Find(it => true).ToList();
            var groupByParty = getData.GroupBy(it => it.IdParty).ToList();
            var totalScore = getData.Sum(it => it.Score);
            var listScoreParty = new List<PartyList>();
            foreach (var item in groupByParty)
            {
                var totalScoreParty = item.Sum(it => it.Score);
                var percentScore = totalScoreParty * 100.0 / totalScore;
                // var totalScoreHave = Math.Round(percentScore / 100 * 500);
                var totalScoreHave = Convert.ToInt32(Math.Round(percentScore / 100 * 500));
                var nameP = item.FirstOrDefault(it => it.IdParty == item.Key).NameParty;
                // Any 

                var totalScoreArea = item.Count(it => it.Tags.Any(i => i == "ชนะ"));
                var scorePartyList = (totalScoreHave - totalScoreArea >= 0) ? totalScoreHave - totalScoreArea : 0;
                listScoreParty.Add(new PartyList
                {
                    Id = Guid.NewGuid().ToString(),
                    IdParty = item.Key,
                    PartyName = nameP,
                    TotalScore = totalScoreHave,
                    AreaScore = totalScoreArea,
                    NameListScore = scorePartyList,
                    PercentScore = percentScore
                });
            }
            PartyScoreCollection.DeleteMany(it => true);
            var sortData = listScoreParty.OrderByDescending(it => it.PercentScore);
            PartyScoreCollection.InsertMany(sortData);
        }

        [HttpGet]
        public PartyList GetSum()
        {
            var getData = PartyScoreCollection.Find(it => true).ToList();
            var gt = new PartyList();
            gt.TotalScore = getData.Sum(it => it.TotalScore);
            gt.AreaScore = getData.Sum(it => it.AreaScore);
            gt.PercentScore = getData.Sum(it => it.PercentScore);
            return gt;
        }

        //ScoreArea Table 4
        [HttpPost]
        public void CalculateScoreFromScorePoll()
        {
            var getDataScorePoll = ScorePollV3Collection.Find(it => true).ToList();
            var getDataScoreArea = ScoreAreaCollection.Find(it => true).ToList();
            var groupByArea = getDataScorePoll.GroupBy(it => it.IdArea).ToList();
            var listScoreArea = new List<ScoreArea>();
            foreach (var item in groupByArea)
            {
                var groupByPart = item.GroupBy(it => it.IdParty).ToList();
                foreach (var data in groupByPart)
                {
                    var getCurrentData = data.OrderByDescending(it => it.datePoll).FirstOrDefault();
                    if (getCurrentData.IdParty != "000" && getCurrentData.IdParty != "888")
                    {
                        var getDataParty = getDataScoreArea.FirstOrDefault(it => it.IdArea == getCurrentData.IdArea
                        && it.IdParty == getCurrentData.IdParty);
                        getDataParty.Score = getCurrentData.Score;
                        getDataParty.Source = getCurrentData.Source;
                        listScoreArea.Add(getDataParty);
                    }
                }
            }
            ScoreAreaCollection.DeleteMany(it => true);
            var sortData = listScoreArea.OrderBy(it => it.IdArea).ToList();
            ScoreAreaCollection.InsertMany(sortData);
        }

        [HttpPost]
        public void SetTags()
        {
            var getDataScoreArea = ScoreAreaCollection.Find(it => true).ToList();
            var groupByArea = getDataScoreArea.GroupBy(it => it.IdArea).ToList();
            var listScoreArea = new List<ScoreArea>();
            foreach (var item in groupByArea)
            {
                var maxScore = item.Max(it => it.Score);
                foreach (var data in item)
                {
                    var tagDefault = (data.Score == maxScore) ? "ชนะ" : "แพ้";
                    data.Tags = new List<string>();
                    data.Tags.Add(tagDefault);
                    // ScoreAreaCollection.ReplaceOne(it => it.Id == data.Id, data);
                    listScoreArea.Add(data);
                }
            }
            ScoreAreaCollection.DeleteMany(it => true);
            var sortData = listScoreArea.OrderBy(it => it.IdArea).ToList();
            ScoreAreaCollection.InsertMany(sortData);
        }

        // APi ScorePoll
        [HttpGet]
        public List<ScorePollV2> GetAllScorePoll()
        {
            var getData = ScorePollV3Collection.Find(it => true).ToList();
            var sortData = getData.OrderByDescending(it => it.datePoll).ToList();
            return sortData;
        }

        [HttpGet("{idArea}")]
        public List<ScorePollV2> GetListScoreArea(string idArea)
        {
            idArea.ToUpper();
            var getData = ScorePollV3Collection.Find(it => it.IdArea == idArea.ToUpper()).ToList();
            var sortData = getData.OrderByDescending(it => it.datePoll).ToList();
            return sortData;
        }

        //Api ScoreArea Table4
        [HttpGet]
        public List<ScoreArea> GetAllScoreTable4()
        {
            var getData = ScoreAreaCollection.Find(it => true).ToList();
            var sortData = getData.OrderBy(it => it.IdArea).ToList();
            return sortData;
        }

        [HttpGet("{IdArea}")]
        public List<ScoreArea> GetScoreArea(string IdArea)
        {

            var getData = ScoreAreaCollection.Find(it => it.IdArea == IdArea.ToUpper()).ToList();
            var sortData = getData.OrderBy(it => it.IdArea).ToList();
            return sortData;
        }

        [HttpGet]
        public List<ScoreArea> GetMaxScoreArea()
        {
            var getData = ScoreAreaCollection.Find(it => true).ToList();
            var groupByArea = getData.GroupBy(it => it.IdArea).ToList();
            var listMaxScore = new List<ScoreArea>();
            foreach (var item in groupByArea)
            {
                var getWinArea = item.FirstOrDefault(it => it.Tags.Any(i => i == "ชนะ"));
                listMaxScore.Add(getWinArea);
            }
            var sortData = listMaxScore.OrderBy(it => it.IdArea).ToList();
            return sortData;
        }

        [HttpGet("{idParty}")]
        public List<ScoreArea> GetScoreAreaByParty(string idParty)
        {
            var getData = ScoreAreaCollection.Find(it => it.IdParty == idParty).ToList();
            var getScoreAreaByParty = getData.Where(it => it.Tags.Any(i => i == "ชนะ")).ToList().OrderBy(it => it.IdArea).ToList();
            return getScoreAreaByParty;
        }

        //Total Score Table Party
        [HttpGet]
        public List<PartyList> GetAllPartyScore()
        {
            var getData = PartyScoreCollection.Find(it => true).ToList();
            var sortData = getData.OrderByDescending(it => it.PercentScore).ToList();
            return sortData;
        }

        [HttpGet("{IdParty}")]
        public PartyList GetPartyScore(string IdParty)
        {
            var getData = PartyScoreCollection.Find(it => it.IdParty == IdParty).FirstOrDefault();
            return getData;
        }
    }
}
