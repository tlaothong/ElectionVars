
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Election.Api.Models;

public class ReadCsv
{
    public List<ElectionModel> ListElection { get; set; }
    public List<LocationModel> ListLocation { get; set; }
    public List<LocationCodeModel> ListLocationCode { get; set; }
    public IEnumerable<ElectionModel> GetElectionData()
    {
        var FilePath = @"ExamData.csv";
        ListElection = new List<ElectionModel>();
        using (var reader = new StreamReader(FilePath))
        {
            while (!reader.EndOfStream)
            {
                var getReadCsv = reader.ReadLine();
                var dataFromCsv = getReadCsv.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var data in dataFromCsv)
                {
                    var dataElection = data.Split(',').ToList();
                    Int32.TryParse(dataElection[1], out Int32 numberArea);
                    Int32.TryParse(dataElection[6], out Int32 scorePolitical);
                    Int32.TryParse(dataElection[8], out Int32 targetScorePolitical);
                    if (numberArea != 0 && scorePolitical != 0 && targetScorePolitical != 0)
                    {
                        ListElection.Add(new ElectionModel
                        {
                            NameArea = dataElection[0],
                            NumberArea = numberArea,
                            Province = dataElection[2],
                            District = dataElection[3],
                            Party = dataElection[4],
                            NameRegister = dataElection[5],
                            Score = scorePolitical,
                            Tag = dataElection[7],
                            TargetScore = targetScorePolitical
                        });
                    }

                }
            }
        }
        return ListElection;
    }

    public IEnumerable<LocationModel> GetDataLocation()
    {
        var FilePath = @"LocationCode.csv";
        ListLocation = new List<LocationModel>();
        using (var reader = new StreamReader(FilePath))
        {
            while (!reader.EndOfStream)
            {
                var getReadCsv = reader.ReadLine();
                var dataFromCsv = getReadCsv.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var data in dataFromCsv)
                {
                    var dataLocation = data.Split(',').ToList();
                    if (dataLocation[0] != "IDProvince")
                    {
                        ListLocation.Add(new LocationModel
                        {
                            IDProvince = dataLocation[0],
                            LocationCode = dataLocation[1],
                            Province = dataLocation[2],
                            District = dataLocation[3],
                            SubDistrict = dataLocation[4],
                            ZipCode = dataLocation[5],
                            Note = dataLocation[6]
                        });
                    }
                }

            }
        }
        return ListLocation;
    }

    public IEnumerable<LocationCodeModel> GetDataLocatioCode()
    {
        var FilePath = @"LocationPostalCode.csv";
        ListLocationCode = new List<LocationCodeModel>();
        using (var reader = new StreamReader(FilePath))
        {
            while (!reader.EndOfStream)
            {
                var getReadCsv = reader.ReadLine();
                var dataFromCsv = getReadCsv.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var data in dataFromCsv)
                {
                    var dataLocatioCode = data.Split(',').ToList();
                    var IsRegister = true;
                    if (dataLocatioCode[5] == "")
                    {
                        IsRegister = false;
                    }
                    if (dataLocatioCode[0] != "เขตแต่ละจังหวัด(กกต)")
                    {
                        ListLocationCode.Add(new LocationCodeModel
                        {
                            NameArea = dataLocatioCode[0],
                            IdArea = dataLocatioCode[1],
                            PartyListName = dataLocatioCode[2],
                            NumberRegister = dataLocatioCode[3],
                            NameRegister = dataLocatioCode[4],
                            HasRegister = IsRegister
                        });
                    }
                }
            }
        }
        return ListLocationCode;
    }
}