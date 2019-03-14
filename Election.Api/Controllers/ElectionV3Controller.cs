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
    public class ElectionV3Controller : Controller
    {
        const int AtATime = 550;
        const int Delay = 1000;

        IMongoCollection<ScoreArea> Table4Collection { get; set; }
        IMongoCollection<ScoreArea> DemoTable4Collection { get; set; }
        IMongoCollection<ScoreArea> FinalTable4Collection { get; set; }
        IMongoCollection<ScoreArea> Table2Collection { get; set; }
        IMongoCollection<ScorePollV2> FinalScorePollCollection { get; set; }
        IMongoCollection<PartyList> FinalPartyScoreCollection { get; set; }
        IMongoCollection<PartyList> App1PartyScoreCollection { get; set; }
        IMongoCollection<ScorePollCsv> ScorePollCsvCollection { get; set; }
        public static List<ScoreArea> listTable4 { get; set; }

        public ElectionV3Controller()
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
            Table4Collection = database.GetCollection<ScoreArea>("Table4");
            DemoTable4Collection = database.GetCollection<ScoreArea>("DemoTable4");
            FinalTable4Collection = database.GetCollection<ScoreArea>("FinalTable4");
            Table2Collection = database.GetCollection<ScoreArea>("Table2");
            FinalScorePollCollection = database.GetCollection<ScorePollV2>("FinalScorePoll");
            FinalPartyScoreCollection = database.GetCollection<PartyList>("FinalPartyScore");
            App1PartyScoreCollection = database.GetCollection<PartyList>("App1PartyScore");
            ScorePollCsvCollection = database.GetCollection<ScorePollCsv>("ScorePollCsv");
        }
        // Api Score Poll =========================================================================
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
        // Api Table4 ==============================================================================================================
        [HttpGet]
        public List<ScoreArea> GetAllScoreTable4()
        {
            var getData = Table4Collection.Find(it => true).ToList();
            return getData;
        }

        [HttpGet("{idArea}")]
        public List<ScoreArea> GetAreaWithIdArea(string idArea)
        {
            var getData = Table4Collection.Find(it => it.IdArea == idArea.ToUpper()).ToList();
            return getData;
        }

        [HttpGet("{idArea}")]
        public List<ScoreArea> GetAreaWithIdAreaTable2(string idArea)
        {
            var getData = Table2Collection.Find(it => it.IdArea == idArea.ToUpper()).ToList();
            return getData;
        }

        [HttpPost("{idArea}")]
        public void SetTags([FromBody]TextTag newTag, string idArea)
        {
            var getDataTable4 = Table4Collection.Find(it => it.IdArea == idArea.ToUpper()).ToList();
            var listTags = newTag.Text.Split('#').Distinct().ToList();
            foreach (var dataTable4 in getDataTable4)
            {
                if (dataTable4.Tags != null)
                {
                    dataTable4.Tags.RemoveAll(it => true);
                }
                dataTable4.Tags.AddRange(listTags);
                Table4Collection.ReplaceOne(it => it.IdArea == dataTable4.IdArea && it.Id == dataTable4.Id, dataTable4);
            }
        }

        [HttpGet("{idArea}")]
        public TextTag GetTagArea(string idArea)
        {
            var tagDataTable4 = Table4Collection.Find(it => it.IdArea == idArea.ToUpper()).FirstOrDefault();
            var tags = new TextTag();
            if (tagDataTable4 != null)
            {
                tags.Text = string.Join("#", tagDataTable4.Tags);
            }
            else
            {
                tags.Text = "";
            }
            return tags;
        }

        [HttpGet("{getTag}")]
        public List<ScoreArea> GetAreaWithTag(string getTag)
        {
            var getData = Table4Collection.Find(it => it.IdParty == "034" && it.Tags.Any(i => i == getTag)).ToList();
            return getData;
        }

        [HttpGet]
        public List<string> GetAllTag()
        {
            var getDataTag = Table4Collection.Find(it => it.IdParty == "034").ToList();
            var listTag = new List<string>();
            foreach (var data in getDataTag)
            {
                foreach (var tags in data.Tags)
                {
                    // if (tags != "ชนะ" && tags != "แพ้" && tags != "")
                    if (tags != "" && data.Tags.Any(i => i != tags))
                    {
                        listTag.Add(tags);
                    }
                }
            }
            var getAllDuplicateTag = listTag.GroupBy(it => it).ToList();
            var listTagWithOutDuplicate = new List<string>();
            foreach (var data in getAllDuplicateTag)
            {
                listTagWithOutDuplicate.Add(data.Key);
            }
            return listTagWithOutDuplicate;
        }

        [HttpPost("{newScore}")]
        public void EditScore([FromBody]ScoreArea scorePartyModel, double newScore)
        {
            var getParty = Table4Collection.Find(it => it.Id == scorePartyModel.Id).FirstOrDefault();
            scorePartyModel.Score = newScore;
            getParty.Score = scorePartyModel.Score;
            getParty.StatusEdit = true;
            // Table4Collection.DeleteOne(it => it.Id == getParty.Id);
            // Table4Collection.InsertOne(getParty);
            Table4Collection.ReplaceOne(it => it.IdArea == getParty.IdArea && it.Id == getParty.Id, getParty);
            //set status Area Edit
            var getDataByArea = Table4Collection.Find(it => it.IdArea == getParty.IdArea).ToList();
            foreach (var data in getDataByArea)
            {
                data.StatusAreaEdit = true;
                Table4Collection.ReplaceOne(it => it.IdArea == data.IdArea && it.Id == data.Id, data);
            }
        }

        [HttpGet]
        public List<ScoreArea> GetAllAreaTable4()
        {
            var getData = Table4Collection.Find(it => true).ToList()
            .OrderBy(it => it.IdRegion)
            .GroupBy(it => it.IdRegion).ToList();
            var listArea = new List<ScoreArea>();
            foreach (var dataRegion in getData)
            {
                var dataGroupByArea = dataRegion.OrderBy(it => it.IdArea).GroupBy(it => it.IdArea).ToList();
                foreach (var data in dataGroupByArea)
                {
                    var getArea = data.FirstOrDefault();
                    listArea.Add(getArea);
                }
            }
            return listArea;
        }

        [HttpGet("{idParty}")]
        public List<ScoreArea> GetAreaWinScoreParty(string idParty)
        {
            var getData = Table4Collection.Find(it => true).ToList().GroupBy(it => it.IdArea).ToList();
            var listWinParty = new List<ScoreArea>();
            foreach (var data in getData)
            {
                var maxScoreArea = data.Max(it => it.Score);
                foreach (var item in data)
                {
                    if (item.Score == maxScoreArea)
                    {
                        listWinParty.Add(item);
                    }
                }
            }
            var listPartyWin = listWinParty.Where(it => it.IdParty == idParty)
            .OrderBy(it => it.IdRegion).OrderBy(it => it.IdArea).ToList();
            return listPartyWin;
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
                if (getMyParty != null)
                {
                    listScore.Add(new MyParty
                    {
                        Id = Guid.NewGuid().ToString(),
                        IdArea = item.Key,
                        NameArea = getWinnerArea.NameArea,
                        PartyWin = getWinnerArea.NameParty,
                        scoreMax = getWinnerArea.Score,
                        scoreMyParty = getMyParty.Score,
                        StatusAreaEdit = getWinnerArea.StatusAreaEdit,
                        Region = getWinnerArea.Region,
                        IdRegion = getWinnerArea.IdRegion
                    });
                }
                else
                {
                    listScore.Add(new MyParty
                    {
                        Id = Guid.NewGuid().ToString(),
                        IdArea = item.Key,
                        NameArea = getWinnerArea.NameArea,
                        PartyWin = getWinnerArea.NameParty,
                        scoreMax = getWinnerArea.Score,
                        scoreMyParty = 0,
                        StatusAreaEdit = getWinnerArea.StatusAreaEdit,
                        Region = getWinnerArea.Region,
                        IdRegion = getWinnerArea.IdRegion
                    });
                }

            }

            var dataGroupByRegion = listScore.OrderBy(it => it.IdRegion).GroupBy(it => it.IdRegion).ToList();
            var sortData = new List<MyParty>();
            foreach (var dataRegion in dataGroupByRegion)
            {
                var dataGroupByArea = dataRegion.OrderBy(it => it.IdArea).GroupBy(it => it.IdArea).ToList();
                foreach (var dataArea in dataGroupByArea)
                {
                    var data = dataArea.FirstOrDefault(it => it.IdArea == dataArea.Key);
                    sortData.Add(data);
                }
            }
            return sortData;
        }

        [HttpGet("{idArea}")]
        public List<ScoreArea> GetScoreAreasWithArea(string idArea)
        {
            var getData = Table4Collection.Find(it => it.IdArea == idArea.ToUpper()).ToList().OrderByDescending(it => it.Score).ToList();
            return getData;
        }
        // Api Score Party =========================================================================
        [HttpGet]
        public List<PartyList> GetAllPartyScore()
        {
            return FinalPartyScoreCollection.Find(it => true).ToList().OrderByDescending(it => it.PercentScore).ToList();
        }
        //===================================================== Api Upload File and UpdateData ==========================================
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
        public void DeleteTable4()
        {
            var dataTable4 = Table4Collection.Find(it => true).ToList();
            foreach (var data in dataTable4.GroupBy(it => it.IdArea))
            {
                Table4Collection.DeleteMany(it => it.IdArea == data.Key);
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
                    }
                }
            }

            foreach (var data in dataTable4.GroupBy(it => it.IdArea))
            {
                Table4Collection.DeleteMany(it => it.IdArea == data.Key);
            }

            for (int i = 0; i < listT4.Count; i += 650)
            {
                var list = listT4.Skip(i).Take(650);
                Table4Collection.InsertMany(list);
                await Task.Delay(Delay);
            }
        }

        [HttpPost]
        // public void UpdatePartyScore()
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
            //Hack: move to upload process
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

        [HttpPost("{id}/{statusAllies}")]
        public void SetStatusAllies(string id, string statusAllies)
        {
            var dataPartyScore = FinalPartyScoreCollection.Find(it => it.Id == id).FirstOrDefault();
            dataPartyScore.StatusAllies = statusAllies;
            FinalPartyScoreCollection.ReplaceOne(it => it.Id == id, dataPartyScore);
        }

        [HttpGet]
        public List<string> GetAllStatusAllies()
        {
            var dataScoreParty = FinalPartyScoreCollection.Find(it => true).ToList();
            var listStatus = new List<string>();
            foreach (var data in dataScoreParty)
            {
                listStatus.Add(data.StatusAllies);
            }
            var sortStatusAlly = listStatus.GroupBy(it => it).ToList();
            var list2 = new List<string>();
            foreach (var data in sortStatusAlly)
            {
                list2.Add(data.Key);
            }
            return list2;
        }

        [HttpGet("{statusAllies}")]
        public List<PartyList> GetScorePartyByStatusAllies(string statusAllies)
        {
            var dataScoreParty = FinalPartyScoreCollection.Find(it => it.StatusAllies == statusAllies).ToList();
            return dataScoreParty;
        }
        // Api App1 Table 2 ========================================================================================
        [HttpGet]
        public List<ScoreArea> GetTable2()
        {
            var getDataTable2 = Table2Collection.Find(it => true).ToList()
            .OrderBy(it => it.IdRegion)
            .OrderBy(it => it.IdArea).ToList();
            return getDataTable2;
        }

        [HttpGet]
        public List<ScoreArea> GetAllAreaTable2()
        {
            var getData = Table2Collection.Find(it => true).ToList()
            .OrderBy(it => it.IdRegion)
            .GroupBy(it => it.IdRegion).ToList();
            var listArea = new List<ScoreArea>();
            foreach (var dataRegion in getData)
            {
                var dataGroupByArea = dataRegion.OrderBy(it => it.IdArea).GroupBy(it => it.IdArea).ToList();
                foreach (var data in dataGroupByArea)
                {
                    var getArea = data.FirstOrDefault();
                    listArea.Add(getArea);
                }
            }
            return listArea;
        }

        [HttpGet("{idArea}")]
        public List<ScoreArea> GetAreaTable2(string idArea)
        {
            var getArea = Table2Collection.Find(it => it.IdArea == idArea.ToUpper()).ToList()
            .OrderByDescending(it => it.Score).ToList();
            return getArea;
        }
        [HttpPost]
        // public void UpdateTable2()
        public async Task UpdateTable2()
        {
            var dataTable2 = Table2Collection.Find(it => true).ToList();
            if (dataTable2.Any())
            {
                foreach (var data in dataTable2.GroupBy(it => it.IdArea))
                {
                    Table2Collection.DeleteMany(it => it.IdArea == data.Key);
                }
            }
            // Problem can't insert
            try
            {
                var getData = Table4Collection.Find(it => true).ToList();
                for (int i = 0; i < getData.Count; i += AtATime)
                {
                    var list = getData.Skip(i).Take(AtATime);
                    Table2Collection.InsertMany(list);
                    await Task.Delay(Delay);
                }
            }
            catch (System.Exception e)
            {
                throw e;
            }
        }

        [HttpGet("{getTag}")]
        public List<ScoreArea> GetAreaWithTagTable2(string getTag)
        {
            var getData = Table2Collection.Find(it => it.Tags.Any(i => i == getTag)).ToList();
            return getData;
        }

        [HttpGet]
        public List<string> GetAllTagTable2()
        {
            var getDataTag = Table2Collection.Find(it => true).ToList();
            var listTag = new List<string>();
            foreach (var data in getDataTag)
            {
                foreach (var tags in data.Tags)
                {
                    if (tags != "" && data.Tags.Any(i => i != tags))
                    {
                        listTag.Add(tags);
                    }
                }
            }
            var getAllDuplicateTag = listTag.GroupBy(it => it).ToList();
            var listTagWithOutDuplicate = new List<string>();
            foreach (var data in getAllDuplicateTag)
            {
                listTagWithOutDuplicate.Add(data.Key);
            }

            return listTagWithOutDuplicate;
        }
        // Api App1 Score Party ====================================================================================
        [HttpGet]
        public List<PartyList> GetApp1AllScoreParty()
        {
            var getDataApp1ScoreParty = App1PartyScoreCollection.Find(it => true).ToList().OrderByDescending(it => it.PercentScore).ToList();
            return getDataApp1ScoreParty;
        }

        [HttpPost]
        // public void UpdateScorePartyApp1()
        public async Task UpdateScorePartyApp1()
        {
            var getDataScorePartyFormApp2 = FinalPartyScoreCollection.Find(it => true).ToList();
            var dataApp1PartyScore = App1PartyScoreCollection.Find(it => true).ToList();
            if (dataApp1PartyScore.Any())
            {
                foreach (var data in dataApp1PartyScore)
                {
                    App1PartyScoreCollection.DeleteOne(it => it.Id == data.Id);
                }
            }
            for (int i = 0; i < getDataScorePartyFormApp2.Count; i += AtATime)
            {
                var list = getDataScorePartyFormApp2.Skip(i).Take(AtATime);
                App1PartyScoreCollection.InsertMany(list);
                await Task.Delay(Delay);
            }
        }

    }
}

// while (!csvReader.EndOfStream)
// {
//     var getFormCsv = csvReader.ReadLine();
//     var getLine = getFormCsv.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
//     foreach (var item in getLine.Skip(1))
//     {
//         var getData = item.Split(',').ToList();
//         if (getData[0] != "รหัสพรรค" && getData[1] != "ชื่อเขต" &&
//         getData[2] != "รหัสเขต " && getData[3] != "ชื่อพรรค" && getData[4] != "เปอร์เซ็น/คะแนน"
//         && getData[5] != "ภูมิภาค" && getData[6] != "รหัสภูมิภาค")
//         //&& getData[4] != ""
//         {
//             float.TryParse(getData[4], out float score);
//             listScoreCsv.Add(new ScorePollCsv
//             {
//                 Id = Guid.NewGuid().ToString(),
//                 IdParty = getData[0],
//                 NameParty = getData[3],
//                 IdArea = getData[2],
//                 NameArea = getData[1],
//                 Score = score,
//                 Region = getData[5],
//                 IdRegion = getData[6]
//             });
//         }
//     }
// }

//update Score Table 4
//var getDataFromScorePoll = FinalScorePollCollection.Find(it => true).ToList();
// var getTable4 = Table4Collection.Find(it => true).ToList();
// listTable4 = new List<ScoreArea>();
//not fill data csv
// var groupByAreaScorePoll = listScorePoll.GroupBy(it => it.IdArea).ToList();
// foreach (var data in listScorePoll)
// {
//     if (data.IdParty != "999")
//     {
//         var getTable4Update = getTable4.FirstOrDefault(it => it.IdArea == data.IdArea && it.IdParty == data.IdParty);
//         getTable4Update.Score = data.Score;
//         getTable4Update.Source = data.Source;
//         getTable4Update.StatusEdit = false;
//         getTable4Update.StatusAreaEdit = false;
//         getTable4Update.Region = data.Region;
//         getTable4Update.IdRegion = data.IdRegion;
//         // create tag
//         if (getTable4Update.Tags == null)
//         {
//             getTable4Update.Tags = new List<string>();
//         }
//         listTable4.Add(getTable4Update);
//     }
// }
//var groupByAreaTable4 = listTable4.GroupBy(it => it.IdArea);

// var dataTable4GroupByArea = getTable4.GroupBy(it => it.IdArea).ToList();
// foreach (var data in dataTable4GroupByArea)
// {
//     Table4Collection.DeleteMany(it => it.IdArea == data.Key);
// }


