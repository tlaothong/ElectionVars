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
        }

        [HttpGet]
        public List<ScorePoll> GetAllScorePoll()
        {
            return ScorePollV2Collection.Find(it => true).ToList();
        }

        [HttpGet]
        public List<ScoreArea> GetAllScoreTable4()
        {
            return Table4Collection.Find(it => true).ToList();
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
                var getWinnerArea = item.FirstOrDefault(it => it.Tags.Any(i => i == "ชนะ"));
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

        [HttpPost("{id}")]
        public void Edititem([FromBody]ScoreArea model,string id)
        {
            var data = Table4Collection.Find(x => x.Id == id).FirstOrDefault();
            data.Score = model.Score;
            Table4Collection.ReplaceOne(x => x.Id == data.Id, data);
        }

    }
}


