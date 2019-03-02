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

namespace Election.Api.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("api/[controller]/[action]")]
    public class ElectionV3Controller : Controller
    {
        const int AtATime = 100;
        const int Delay = 1000;

        IMongoCollection<ScoreArea> Table4Collection { get; set; }
        IMongoCollection<ScoreArea> FinalTable4Collection { get; set; }
        IMongoCollection<ScoreArea> Table2Collection { get; set; }
        IMongoCollection<ScorePollV2> FinalScorePollCollection { get; set; }
        IMongoCollection<PartyList> FinalPartyScoreCollection { get; set; }
        IMongoCollection<PartyList> App1PartyScoreCollection { get; set; }

        public ElectionV3Controller()
        {
            //var settings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://guntza22:guntza220938@ds026558.mlab.com:26558/electionmana"));
            var settings = MongoClientSettings.FromUrl(new MongoUrl("mongodb://thes:zk70NWOArstd28WKZzMzecE0qF9fYD8TD89SMkLt9jbRuaCSFyNDBkP1lS2SbxVbDXvtzTuuKHphEZS5fBDifg==@thes.documents.azure.com:10255/Election?ssl=true&replicaSet=globaldb"));
            settings.SslSettings = new SslSettings()
            {
                EnabledSslProtocols = SslProtocols.Tls12
            };
            var mongoClient = new MongoClient(settings);
            // mlab
            //var database = mongoClient.GetDatabase("electionmana");
            // Azure
            var database = mongoClient.GetDatabase("Election");
            Table4Collection = database.GetCollection<ScoreArea>("Table4");
            FinalTable4Collection = database.GetCollection<ScoreArea>("FinalTable4");
            Table2Collection = database.GetCollection<ScoreArea>("Table2");
            FinalScorePollCollection = database.GetCollection<ScorePollV2>("FinalScorePoll");
            FinalPartyScoreCollection = database.GetCollection<PartyList>("FinalPartyScore");
            App1PartyScoreCollection = database.GetCollection<PartyList>("App1PartyScore");
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
            var getDataTable4 = Table4Collection.Find(it => it.IdParty == "034" && it.IdArea == idArea.ToUpper()).FirstOrDefault();
            var listTags = newTag.Text.Split('#').Distinct().ToList();
            // getDataTable4.Tags.RemoveAll(it => it != "ชนะ" || it != "แพ้");
            getDataTable4.Tags.RemoveAll(it => true);
            getDataTable4.Tags.AddRange(listTags);
            Table4Collection.ReplaceOne(it => it.IdArea == getDataTable4.IdArea && it.Id == getDataTable4.Id, getDataTable4);
        }

        [HttpGet("{idArea}")]
        public TextTag GetTagArea(string idArea)
        {
            var tagDataTable4 = Table4Collection.Find(it => it.IdArea == idArea.ToUpper() && it.IdParty == "034").FirstOrDefault();
            var tags = new TextTag();
            // tagDataTable4.Tags.RemoveAll(it => it == "ชนะ" || it == "แพ้");

            tags.Text = string.Join("#", tagDataTable4.Tags);
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
            var getDataUpdate = Table4Collection.Find(it => true).ToList();
            var groupByArea = getDataUpdate.GroupBy(it => it.IdArea).ToList();
            foreach (var item in groupByArea)
            {
                if (item.Key == getParty.IdArea)
                {
                    foreach (var datas in item)
                    {
                        datas.StatusAreaEdit = true;
                        // Table4Collection.DeleteOne(it => it.Id == datas.Id);
                        // Table4Collection.InsertOne(datas);
                        Table4Collection.ReplaceOne(it => it.IdArea == datas.IdArea && it.Id == datas.Id, datas);
                    }
                }
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

            // var sortData = listArea.OrderBy(it => it.IdRegion).OrderBy(it => it.IdArea).ToList();
            return listArea;
        }

        [HttpGet("{idParty}")]
        public List<ScoreArea> GetAreaWinScoreParty(string idParty)
        {
            // var getData = Table4Collection.Find(it => it.IdParty == idParty).ToList()
            // .Where(it => it.Tags.Any(i => i == "ชนะ")).ToList();
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
                listScore.Add(new MyParty
                {
                    Id = Guid.NewGuid().ToString(),
                    IdArea = item.Key,
                    NameArea = getMyParty.NameArea,
                    PartyWin = getWinnerArea.NameParty,
                    scoreMax = getWinnerArea.Score,
                    scoreMyParty = getMyParty.Score,
                    StatusAreaEdit = getMyParty.StatusAreaEdit,
                    Region = getMyParty.Region,
                    IdRegion = getMyParty.IdRegion
                });
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
        public async Task MockTable2()
        {
            var csvReader = new ReadCsv();
            var listTable2 = csvReader.MockPrototypeDataTable2();
            var dataTable4 = Table4Collection.Find(it => true).ToList();
            //foreach (var data in dataTable4)
            //{
            //    Table4Collection.DeleteOne(it => it.Id == data.Id);
            //}
            for (int i = 0; i < listTable2.Count; i += AtATime)
            {
                var list = listTable2.Skip(i).Take(AtATime);
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
                while (!csvReader.EndOfStream)
                {
                    var getFormCsv = csvReader.ReadLine();
                    var getLine = getFormCsv.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    foreach (var item in getLine)
                    {
                        var getData = item.Split(',').ToList();
                        if (getData[0] != "รหัสพรรค" && getData[1] != "ชื่อเขต" &&
                        getData[2] != "รหัสเขต " && getData[3] != "ชื่อพรรค" && getData[4] != "เปอร์เซ็น/คะแนน"
                        && getData[5] != "ภูมิภาค" && getData[6] != "รหัสภูมิภาค" && getData[4] != "")
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
                                Region = getData[5],
                                IdRegion = getData[6]
                            });
                        }
                    }
                }
            }
            // Fill in ScorePoll
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
            //FinalScorePollCollection.InsertMany(listScorePoll);


            for (int i = 0; i < listScorePoll.Count; i += AtATime)
            {
                var list = listScorePoll.Skip(i).Take(AtATime);
                FinalScorePollCollection.InsertMany(list);
                await Task.Delay(Delay);
            }
            //update Score Table 4
            var getDataFromScorePoll = FinalScorePollCollection.Find(it => true).ToList();
            var getTable4 = Table4Collection.Find(it => true).ToList();
            var listTable4 = new List<ScoreArea>();
            var groupByAreaScorePoll = getDataFromScorePoll.GroupBy(it => it.IdArea).ToList();
            foreach (var item in groupByAreaScorePoll)
            {
                var groupByParty = item.GroupBy(it => it.IdParty).ToList();
                foreach (var datas in groupByParty)
                {
                    if (datas.Key != "999")
                    {
                        var getCurrentData = datas.OrderByDescending(it => it.datePoll).FirstOrDefault();
                        var getTable4Update = getTable4.FirstOrDefault(it => it.IdArea == getCurrentData.IdArea && it.IdParty == getCurrentData.IdParty);
                        getTable4Update.Score = getCurrentData.Score;
                        getTable4Update.Source = getCurrentData.Source;
                        getTable4Update.StatusEdit = false;
                        getTable4Update.StatusAreaEdit = false;
                        getTable4Update.Region = getCurrentData.Region;
                        getTable4Update.IdRegion = getCurrentData.IdRegion;
                        // create tag
                        getTable4Update.Tags = new List<string>();
                        listTable4.Add(getTable4Update);
                    }
                }
            }
            //Table4Collection.DeleteMany(it => true);
            //Table4Collection.InsertMany(listTable4);

            var idAreas = getTable4.GroupBy(it => it.IdArea).Select(x => x.First()).Select(it => it.IdArea).ToArray();
            foreach (var idArea in idAreas)
            {
                Table4Collection.DeleteMany(it => it.IdArea == idArea);
            }

            for (int i = 0; i < listTable4.Count; i += AtATime)
            {
                var list = listTable4.Skip(i).Take(AtATime);
                Table4Collection.InsertMany(list);
                await Task.Delay(Delay);
            }
        }

        [HttpPost]
        // public void UpdatePartyScore()
        public async Task UpdatePartyScore()
        {
            var dataScoreArea = Table4Collection.Find(it => true).ToList();
            var dataScoreParty = FinalPartyScoreCollection.Find(it => true).ToList();
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
                var statusAlly = dataScoreParty.FirstOrDefault(it => it.IdParty == data.Key)?.StatusAllies;
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
                    PercentScore = Math.Round(totalScoreParty / ratio) * 100 / totalSS,
                    StatusAllies = statusAlly
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
            //FinalPartyScoreCollection.DeleteMany(it => true);
            //FinalPartyScoreCollection.InsertMany(listPartyFinal);

            var finalPartyScores = FinalPartyScoreCollection.Find(it => true).ToList();
            foreach (var finalPartyScore in finalPartyScores)
            {
                await FinalPartyScoreCollection.DeleteOneAsync(it => it.Id == finalPartyScore.Id && it.IdParty == finalPartyScore.IdParty);
            }
            var sortData = listPartyFinal.OrderByDescending(it => it.PercentScore).ToList();
            for (int i = 0; i < sortData.Count; i += AtATime)
            {
                var list = sortData.Skip(i).Take(AtATime);
                await FinalPartyScoreCollection.InsertManyAsync(list);
                await Task.Delay(Delay);
            }
            //FinalPartyScoreCollection.InsertMany(sortData);
        }

        [HttpPost("{id}/{statusAllies}")]
        public void SetStatusAllies(string id, string statusAllies)
        {
            var dataPartyScore = FinalPartyScoreCollection.Find(it => it.Id == id).FirstOrDefault();
            dataPartyScore.StatusAllies = statusAllies;
            // FinalPartyScoreCollection.DeleteOne(it => it.Id == id);
            // FinalPartyScoreCollection.InsertOne(dataPartyScore);
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
                    var getArea = data.FirstOrDefault(it => it.IdParty == "034");
                    listArea.Add(getArea);
                }
            }
            // var sortData = listArea.OrderBy(it => it.IdRegion).OrderBy(it => it.IdArea).ToList();
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
            var getData = Table4Collection.Find(it => true).ToList();
            var dataTable2 = Table2Collection.Find(it => true).ToList()
            .GroupBy(it => it.IdArea).ToList();
            if (dataTable2 != null)
            {
                foreach (var data in dataTable2)
                {
                    Table2Collection.DeleteMany(it => it.IdArea == data.Key);
                }
            }
            // Problem can't insert
            for (int i = 0; i < getData.Count; i += AtATime)
            {
                var list = getData.Skip(i).Take(AtATime);
                Table2Collection.InsertMany(list);
                await Task.Delay(Delay);
            }
            // Table2Collection.DeleteMany(it => true);
            // Table2Collection.InsertMany(getData);

        }

        [HttpGet("{getTag}")]
        public List<ScoreArea> GetAreaWithTagTable2(string getTag)
        {
            var getData = Table2Collection.Find(it => it.IdParty == "034" && it.Tags.Any(i => i == getTag)).ToList();
            return getData;
            // var getData = Table2Collection.Find(it => it.IdParty == "034" && it.Tags.Any(i => i == getTag)).ToList()
            // .OrderBy(it => it.IdRegion)
            // .OrderBy(it => it.IdArea).ToList();
            // return getData;
        }

        [HttpGet]
        public List<string> GetAllTagTable2()
        {
            var getDataTag = Table2Collection.Find(it => it.IdParty == "034").ToList();
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
            foreach (var data in dataApp1PartyScore)
            {
                App1PartyScoreCollection.DeleteOne(it => it.Id == data.Id);
            }
            for (int i = 0; i < getDataScorePartyFormApp2.Count; i += AtATime)
            {
                var list = getDataScorePartyFormApp2.Skip(i).Take(AtATime);
                App1PartyScoreCollection.InsertMany(list);
                await Task.Delay(Delay);
            }
            //App1PartyScoreCollection.DeleteMany(it => true);
            //App1PartyScoreCollection.InsertMany(getDataScorePartyFormApp2);
        }
        //Mock Final Table4
        [HttpPost]
        // public void MockFinalTable4()
        public async Task MockFinalTable4()
        {
            var csvReader = new ReadCsv();
            var listDataTable4FromCsv = csvReader.MockPrototypeDataTable2().OrderBy(it => it.IdArea).ToList();
            var dataFinalTAble4 = FinalTable4Collection.Find(it => true).ToList();
            // foreach (var data in dataFinalTAble4)
            // {
            //     FinalTable4Collection.DeleteOne(it => it.Id == data.Id);
            // }
            // const int AtATime = 100;
            // const int Delay = 700;
            // for (int i = 0; i < listDataTable4FromCsv.Count; i += AtATime)
            // {
            //     var list = listDataTable4FromCsv.Skip(i).Take(AtATime);
            //     FinalTable4Collection.InsertMany(list);
            //     await Task.Delay(Delay);
            // }
            FinalTable4Collection.DeleteMany(it => true);
            FinalTable4Collection.InsertMany(listDataTable4FromCsv);
        }
        [HttpGet]
        public List<ScoreArea> GetFinalTable4()
        {
            var dataFinalTAble4 = FinalTable4Collection.Find(it => true).ToList();
            return dataFinalTAble4;
        }
    }
}


//  var dataTable4 = Table4Collection.Find(it => true).ToList();
// foreach (var data in dataTable4)
// {
//     Table4Collection.DeleteOne(it => it.Id == data.Id);
// }
// const int AtATime = 100;
// const int Delay = 700;
// for (int i = 0; i < listTable2.Count; i += AtATime)
// {
//     var list = listTable2.Skip(i).Take(AtATime);
//     Table4Collection.InsertMany(list);
//     await Task.Delay(Delay);
// }

// set tag
// var getDataUpdate2 = Table4Collection.Find(it => true).ToList();
// var groupByArea2 = getDataUpdate2.GroupBy(it => it.IdArea).ToList();
// var listUpdate = new List<ScoreArea>();
// foreach (var item in groupByArea2)
// {
//     var maxScore = item.Max(it => it.Score);
//     foreach (var data in item)
//     {
//         if (data.Score == maxScore)
//         {
//             if (data.Tags.Any(i => i != "ชนะ"))
//             {
//                 data.Tags.Remove("แพ้");
//                 data.Tags.Add("ชนะ");
//                 listUpdate.Add(data);
//             }
//             else
//             {
//                 listUpdate.Add(data);
//             }
//         }
//         else
//         {
//             if (data.Tags.Any(i => i == "ชนะ"))
//             {
//                 data.Tags.Remove("ชนะ");
//                 data.Tags.Add("แพ้");
//                 listUpdate.Add(data);
//             }
//             else
//             {
//                 listUpdate.Add(data);
//             }
//         }
//     }
// }
// Table4Collection.DeleteMany(it => true);
// Table4Collection.InsertMany(listUpdate);

// var listParty = dataScoreArea.GroupBy(it => it.IdParty)
// .Select(it => new PartyList
// {
//     Id = Guid.NewGuid().ToString(),
//     IdParty = it.Key,
//     PartyName = it.FirstOrDefault().NameParty,
//     NameInitial = it.FirstOrDefault().NameInitial,
//     TotalScore = it.Sum(i => i.Score),
//     HaveScoreDigit = it.Sum(i => i.Score) / ratio,
//     HaveScore = Math.Round(it.Sum(i => i.Score) / ratio),
//     AreaScore = it.Count(i => i.Tags.Any(x => x == "ชนะ")),
//     NameListScore = Math.Round(it.Sum(i => i.Score) / ratio) - it.Count(i => i.Tags.Any(x => x == "ชนะ")),
//     PercentScore = Math.Round(it.Sum(i => i.Score) / ratio) * 100 / totalSS
// }).ToList();

