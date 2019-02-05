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
        }

        [HttpPost]
        public void MockDataScorePoll()
        {
            var readerCsv = new ReadCsv();
            var listScorePoll = new List<ScorePoll>();
            var dataPollCsv = readerCsv.MockModelScorePollCsv().OrderBy(it => it.IdArea).ToList();
            var groupByArea = dataPollCsv.GroupBy(it => it.IdArea).ToList();
            var goodScoreDefault = 0;
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
                        var ScoreParty = Math.Round(data.Score * goodScoreDefault / 100.0);
                        DateTime dt = new DateTime(2019, 1, 22);
                        listScorePoll.Add(new ScorePoll
                        {
                            Id = Guid.NewGuid().ToString(),
                            IdParty = data.IdParty,
                            IdArea = data.IdArea,
                            datePoll = dt,
                            Score = ScoreParty,
                            Source = "poll"
                        });
                    }
                }
            }
            ScorePollCollection.InsertMany(listScorePoll);
        }
    }
}
