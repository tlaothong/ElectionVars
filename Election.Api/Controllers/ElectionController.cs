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

        // IMongoCollection<LocationModel> LocationCollection2 { get; set; }
        IMongoCollection<LocationCodeModel> LocationCodeCollection { get; set; }

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
            // LocationCollection2 = database.GetCollection<LocationModel>("LocationTest2");
            LocationCodeCollection = database.GetCollection<LocationCodeModel>("LocationCodeTest");
        }

        [HttpGet]
        public List<ElectionModel> GetAll()
        {
            var listElection = ElectionCollection.Find(it => true).ToList();
            return listElection;
        }

        [HttpGet]
        public List<LocationModel> GetAllLocation()
        {
            var listLocation = LocationCollection.Find(it => true).ToList();
            return listLocation;
        }

        // [HttpGet]
        // public List<IGrouping<string, LocationModel>> GetLocationAll()
        // {
        //     var listLocation = LocationCollection.Find(it => true).ToList();
        //     var list = listLocation.GroupBy(it => it.Province).ToList();
        //     var list2 = new List<IGrouping<string, LocationModel>>();
        //     var listTest = new List<LocationModel>();
        //     // foreach (var data in list)
        //     // {

        //     //     // foreach (var data2 in data)
        //     //     // {
        //     //     //     listTest.Add(data2);
        //     //     // }
        //     // }
        //     return list;
        // }

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

        [HttpGet]
        public List<LocationCodeModel> GetAllLocationCode()
        {
            var listLocationCode = LocationCodeCollection.Find(it => true).ToList();
            return listLocationCode;
        }

        [HttpGet("{filter}")]
        public List<ElectionModel> GetFilter(string filter)
        {
            var getFilter = ElectionCollection.Find(it => it.Tag == filter).ToList();
            return getFilter;
        }

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
        public void fillDataLocationCode()
        {
            LocationCodeCollection.DeleteMany(it => true);
            var csvReader = new ReadCsv();
            var dataLocationCode = csvReader.GetDataLocatioCode().ToList();
            var listLocationCode = new List<LocationCodeModel>();
            foreach (var data in dataLocationCode)
            {
                data.Id = Guid.NewGuid().ToString();
                listLocationCode.Add(data);
            }
            LocationCodeCollection.InsertMany(listLocationCode);
        }
    }
}
