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
using System.Text;
using MongoDB.Bson;

namespace Election.Api.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("api/[controller]/[action]")]
    public class TestApiController : Controller
    {
        const int AtATime = 550;
        const int Delay = 1000;

        IMongoCollection<ScoreArea> Table4Collection { get; set; }
        IMongoCollection<ScoreArea> ListT4Collection { get; set; }
        IMongoCollection<ScoreArea> DemoTable4Collection { get; set; }
        IMongoCollection<ScorePollV2> FinalScorePollCollection { get; set; }
        IMongoCollection<PartyList> FinalPartyScoreCollection { get; set; }
        //public static List<ScoreArea> listT4 { get; set; }

        public TestApiController()
        {
            //var settings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://guntza22:guntza220938@ds026558.mlab.com:26558/electionmana"));
            var settings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://thes:zk70NWOArstd28WKZzMzecE0qF9fYD8TD89SMkLt9jbRuaCSFyNDBkP1lS2SbxVbDXvtzTuuKHphEZS5fBDifg==@thes.documents.azure.com:10255/Election?ssl=true&replicaSet=globaldb"));
            var mongoClient = new MongoClient(settings);
            // mlab
            //var database = mongoClient.GetDatabase("electionmana");
            // Azure
            var database = mongoClient.GetDatabase("Election");
            settings.SslSettings = new SslSettings()
            {
                EnabledSslProtocols = SslProtocols.Tls12
            };
            Table4Collection = database.GetCollection<ScoreArea>("TestTable4");
            DemoTable4Collection = database.GetCollection<ScoreArea>("DemoTable4");
            FinalScorePollCollection = database.GetCollection<ScorePollV2>("TestFinalScorePoll");
            FinalPartyScoreCollection = database.GetCollection<PartyList>("TestFinalPartyScore");
            ListT4Collection = database.GetCollection<ScoreArea>("listT4");
        }

        [HttpPost]
        public async Task MockDemoTable4()
        {
            var csvReader = new ReadCsv();
            var listTable2 = csvReader.MockPrototypeDataTable2();
            for (int i = 0; i < listTable2.Count; i += AtATime)
            {
                var list = listTable2.Skip(i).Take(AtATime);
                DemoTable4Collection.InsertMany(list);
                await Task.Delay(Delay);
            }
        }


        [HttpPost]
        public async Task MockTable4()
        {
            var dataDemoTable4 = DemoTable4Collection.Find(it => true).ToList();
            for (int i = 0; i < dataDemoTable4.Count; i += AtATime)
            {
                var list = dataDemoTable4.Skip(i).Take(AtATime);
                Table4Collection.InsertMany(list);
                await Task.Delay(Delay);
            }
        }

        [HttpPost]
        public async Task UploadFile()
        {
            // Read
            var listScoreCsv = new List<ScorePollCsv>();
            using (var csvReader = new StreamReader(Request.Form.Files.FirstOrDefault().OpenReadStream()))
            {
                listScoreCsv = csvReader.ReadToEnd()
                    .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                    .Skip(1)
                    .Select(it =>
                    {
                        var getData = it.Split(',').ToList();
                        float.TryParse(getData[4], out float score);
                        return new ScorePollCsv
                        {
                            Id = Guid.NewGuid().ToString(),
                            IdParty = getData[0],
                            NameParty = getData[3],
                            IdArea = getData[2],
                            NameArea = getData[1],
                            Score = score,
                            Region = getData[5],
                            IdRegion = getData[6]
                        };
                    }).ToList();
            }

            // Fill in ScorePoll
            var listScorePoll = new List<ScorePollV2>();
            var groupByArea = listScoreCsv.GroupBy(it => it.IdArea).ToList();
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
                            Source = "Poll",
                            Region = datas.Region,
                            IdRegion = datas.IdRegion
                        });
                    }
                    else
                    {
                        listScorePoll.Add(new ScorePollV2
                        {
                            Id = datas.Id,
                            IdParty = datas.IdParty,
                            NameParty = datas.NameParty,
                            IdArea = datas.IdArea,
                            NameArea = datas.NameArea,
                            datePoll = DateTime.Now,
                            Score = datas.Score,
                            PercentScore = datas.Score,
                            Source = "Poll",
                            Region = datas.Region,
                            IdRegion = datas.IdRegion
                        });
                    }
                }
            }

            for (int i = 0; i < listScorePoll.Count; i += AtATime)
            {
                var list = listScorePoll.Skip(i).Take(AtATime);
                FinalScorePollCollection.InsertMany(list);
                await Task.Delay(Delay);
            }
        }

        [HttpPost]
        public async Task UpdateTable4()
        {
            var dataScorePoll = FinalScorePollCollection.Find(it => true).ToList();
            var dataTable4 = Table4Collection.Find(it => true).ToList();
            var dataScorePollGroupByArea = dataScorePoll.GroupBy(it => it.IdArea).ToList();
            var listT4 = new List<ScoreArea>();
            foreach (var dataPoll in dataScorePollGroupByArea)
            {
                var dataScorePollGroupByParty = dataPoll.GroupBy(it => it.IdParty).ToList();
                foreach (var dataParty in dataScorePollGroupByParty)
                {
                    var getCurentData = dataParty.OrderByDescending(it => it.datePoll).FirstOrDefault();
                    var getMatchDataTable4 = dataTable4.FirstOrDefault(it => it.IdArea == getCurentData.IdArea
                     && it.IdParty == getCurentData.IdParty);
                    if (getMatchDataTable4 != null)
                    {
                        getMatchDataTable4.Score = getCurentData.Score;
                        getMatchDataTable4.Source = getCurentData.Source;
                        getMatchDataTable4.StatusEdit = false;
                        getMatchDataTable4.StatusAreaEdit = false;
                        getMatchDataTable4.Region = getCurentData.Region;
                        getMatchDataTable4.IdRegion = getCurentData.IdRegion;
                        // create tag
                        if (getMatchDataTable4.Tags == null)
                        {
                            getMatchDataTable4.Tags = new List<string>();
                        }
                        listT4.Add(getMatchDataTable4);
                        // Table4Collection.ReplaceOne(it => it.IdArea == getMatchDataTable4.IdArea
                        // && it.IdParty == getMatchDataTable4.IdParty, getMatchDataTable4);
                    }
                }
            }

            for (int i = 0; i < listT4.Count; i += 550)
            {
                var list = listT4.Skip(i).Take(550);
                ListT4Collection.InsertMany(list);
                await Task.Delay(1000);
            }

            foreach (var data in dataTable4.GroupBy(it => it.IdArea))
            {
                Table4Collection.DeleteMany(it => it.IdArea == data.Key);
            }
        }

        [HttpPost]
        public void DeleteTable4()
        {
            var dataTable4 = Table4Collection.Find(it => true).ToList();
            foreach (var data in dataTable4.GroupBy(it => it.IdArea))
            {
                Table4Collection.DeleteMany(it => it.IdArea == data.Key);
            }
        }


        [HttpPost]
        public async Task FillDataIntoTable4_1()
        {
            var dataListT4 = ListT4Collection.Find(it => true).ToList();
            var listTable4P1 = dataListT4.Skip(0).Take(dataListT4.Count / 2).ToList();
            for (int i = 0; i < listTable4P1.Count; i += 550)
            {
                var list = listTable4P1.Skip(i).Take(550);
                Table4Collection.InsertMany(list);
                await Task.Delay(1000);
            }

            foreach (var data in listTable4P1)
            {
                ListT4Collection.DeleteOne(it => it.IdArea == data.IdArea &&
                it.IdParty == data.IdParty);
            }
        }

        [HttpPost]
        public async Task FillDataIntoTable4_2()
        {
            var dataListT4 = ListT4Collection.Find(it => true).ToList();
            //var listTable4P2 = dataListT4.Skip(dataListT4.Count / 2).Take(6000).ToList();
            for (int i = 0; i < dataListT4.Count; i += 550)
            {
                var list = dataListT4.Skip(i).Take(550);
                Table4Collection.InsertMany(list);
                await Task.Delay(1000);
            }

            foreach (var data in dataListT4)
            {
                ListT4Collection.DeleteOne(it => it.IdArea == data.IdArea &&
                it.IdParty == data.IdParty);
            }
        }

        [HttpGet]
        public int GetCountOfTable4()
        {
            var count = Table4Collection.Find(it => true).ToList().Count;
            return count;
        }

        [HttpPost]
        public async Task UpdatePartyScore()
        {
            var dataScoreArea = Table4Collection.Find(it => true).ToList();
            var totalScore = dataScoreArea.Sum(it => it.Score);
            var totalSS = 500.0;
            var ratio = Convert.ToInt32(totalScore / totalSS);
            var listPartyFinal = new List<PartyList>();
            var dataGroupByArea = dataScoreArea.GroupBy(it => it.IdArea).ToList();
            //get max score
            var listPartyWin = new List<ScoreArea>();
            foreach (var item in dataGroupByArea)
            {
                var maxScore = item.Max(it => it.Score);
                var partyWin = item.FirstOrDefault(it => it.Score == maxScore);
                listPartyWin.Add(partyWin);
            }
            var dataGroupByParty = dataScoreArea.GroupBy(it => it.IdParty).ToList();
            var listParty = new List<PartyList>();
            foreach (var data in dataGroupByParty)
            {
                var totalScoreParty = data.Sum(it => it.Score);
                var scoreWithArea = listPartyWin.Count(it => it.IdParty == data.Key);
                listParty.Add(new PartyList
                {
                    Id = Guid.NewGuid().ToString(),
                    IdParty = data.Key,
                    PartyName = data.FirstOrDefault().NameParty,
                    NameInitial = data.FirstOrDefault().NameInitial,
                    TotalScore = totalScoreParty,
                    HaveScoreDigit = totalScoreParty / ratio,
                    HaveScore = Math.Round(totalScoreParty / ratio),
                    AreaScore = scoreWithArea,
                    NameListScore = Math.Round(totalScoreParty / ratio) - scoreWithArea,
                    PercentScore = Math.Round(totalScoreParty / ratio) * 100 / totalSS
                });
            }
            while (listParty.Sum(it => it.HaveScore) < totalSS || listParty.Any(it => it.HaveScore < it.AreaScore))
            {
                if (listParty.Sum(it => it.HaveScore) < totalSS)
                {
                    var diff = totalSS - listParty.Sum(it => it.HaveScore);
                    listParty = listParty.OrderByDescending(it => it.HaveScoreDigit - Math.Floor(it.HaveScoreDigit)).ToList();
                    for (int i = 0; i < diff; i++)
                    {
                        listParty[i].HaveScore++;
                        listParty[i].NameListScore++;
                        listParty[i].PercentScore = listParty[i].HaveScore * 100.0 / 500;
                    }
                }

                if (listParty.Any(it => it.HaveScore < it.AreaScore))
                {
                    var parties = listParty.Where(it => it.HaveScore < it.AreaScore).ToList();

                    foreach (var party in parties)
                    {
                        listParty.Remove(party);
                        party.HaveScoreDigit = party.AreaScore;
                        party.HaveScore = party.AreaScore;
                        party.NameListScore = 0;
                        party.PercentScore = party.HaveScore * 100.0 / 500;
                    }

                    listPartyFinal.AddRange(parties);

                    totalScore = listParty.Sum(it => it.TotalScore);
                    totalSS = 500 - listPartyFinal.Sum(it => it.HaveScore);
                    ratio = Convert.ToInt32(totalScore / totalSS);

                    foreach (var party in listParty)
                    {
                        party.HaveScoreDigit = party.TotalScore / ratio;
                        party.HaveScore = Math.Round(party.HaveScoreDigit);
                        party.NameListScore = party.HaveScore - party.AreaScore;
                        party.PercentScore = party.HaveScore * 100.0 / 500;
                    }
                }
            }
            listPartyFinal.AddRange(listParty);
            var finalPartyScores = FinalPartyScoreCollection.Find(it => true).ToList();

            if (finalPartyScores.Any())
            {
                var sortData = listPartyFinal.OrderByDescending(it => it.PercentScore).ToList();
                foreach (var data in sortData)
                {
                    var statusAllies = finalPartyScores.FirstOrDefault(it => it.IdParty == data.IdParty).StatusAllies;
                    data.StatusAllies = statusAllies;
                }
                foreach (var finalPartyScore in finalPartyScores)
                {
                    await FinalPartyScoreCollection.DeleteOneAsync(it => it.Id == finalPartyScore.Id);
                }
                for (int i = 0; i < sortData.Count; i += AtATime)
                {
                    var list = sortData.Skip(i).Take(AtATime);
                    FinalPartyScoreCollection.InsertMany(list);
                    await Task.Delay(Delay);
                }
            }
            else
            {
                var sortData = listPartyFinal.OrderByDescending(it => it.PercentScore).ToList();
                foreach (var data in sortData)
                {
                    data.StatusAllies = "";
                }
                for (int i = 0; i < sortData.Count; i += AtATime)
                {
                    var list = sortData.Skip(i).Take(AtATime);
                    FinalPartyScoreCollection.InsertMany(list);
                    await Task.Delay(Delay);
                }
            }
        }
    
    
    }
}
