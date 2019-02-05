
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Election.Api.Models;

public class ReadCsv
{
    public List<ElectionModel> ListElection { get; set; }
    public List<LocationModel> ListLocation { get; set; }
    public List<AreaElection> ListArea { get; set; }
    public List<PartyScore> ListPartyScore { get; set; }
    public List<ScorePollCsv> listFullScorePoll { get; set; }

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

    public List<AreaElection> GetDataAreaElection()
    {
        var FilePath = @"DataAreaElection.csv";
        ListArea = new List<AreaElection>();
        using (var reader = new StreamReader(FilePath))
        {
            while (!reader.EndOfStream)
            {
                var getReadCsv = reader.ReadLine();
                var dataFromCsv = getReadCsv.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var data in dataFromCsv)
                {
                    var getData = data.Split(',').ToList();
                    if (getData[0] != "เขต" && getData[1] != "พรรค" && getData[2] != "คะแนน" &&
                    getData[3] != "Tag" && getData[4] != "พรรคที่ชนะ")
                    {
                        Int32.TryParse(getData[2], out Int32 ScoreParty);
                        ListArea.Add(new AreaElection
                        {
                            NameArea = getData[0],
                            PartyName = getData[1],
                            Score = ScoreParty,
                            Tag = getData[3],
                            PartyWinner = getData[4]
                        });
                    }
                }
            }

        }
        return ListArea;
    }

    public List<PartyScore> GetDataPartyScore()
    {
        var FilePath = @"ParytyScore.csv";
        ListPartyScore = new List<PartyScore>();
        using (var reader = new StreamReader(FilePath))
        {
            while (!reader.EndOfStream)
            {
                var getReadCsv = reader.ReadLine();
                var dataFromCsv = getReadCsv.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var data in dataFromCsv)
                {
                    var dataPary = data.Split(',').ToList();
                    if (dataPary[0] != "พรรค" && dataPary[1] != "สส.พึงมี" && dataPary[2] != "สส.แบ่งเขต"
                    && dataPary[3] != "สส.บัญชีรายชื่อ")
                    {
                        Int32.TryParse(dataPary[1], out Int32 scoreTotal);
                        Int32.TryParse(dataPary[2], out Int32 scoreArea);
                        Int32.TryParse(dataPary[3], out Int32 scoreNameList);
                        ListPartyScore.Add(new PartyScore
                        {
                            PartyName = dataPary[0],
                            TotalScore = scoreTotal,
                            AreaScore = scoreArea,
                            NameListScore = scoreNameList
                        });
                    }
                }

            }
        }
        return ListPartyScore;
    }

    //Remove
    public List<File1> GetFile1()
    {
        var FilePath = @"File1.csv";
        var listFile1 = new List<File1>();
        using (var reader = new StreamReader(FilePath))
        {
            while (!reader.EndOfStream)
            {
                var getFromCsv = reader.ReadLine();
                var getLine = getFromCsv.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var line in getLine)
                {
                    var getData = line.Split(',').ToList();
                    listFile1.Add(new File1
                    {
                        NameParty = getData[2],
                        IDProvince = getData[1],
                        NoRegister = getData[3],
                        NameRegister = getData[4],
                    });
                }
            }
        }
        return listFile1;
    }

    public List<AreaData> GetFile2()
    {
        var FilePath = @"File2.csv";
        var listFile2 = new List<AreaData>();
        using (var reader = new StreamReader(FilePath))
        {
            while (!reader.EndOfStream)
            {
                var getFromCsv = reader.ReadLine();
                var getLine = getFromCsv.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var line in getLine)
                {
                    var getData = line.Split(',').ToList();
                    listFile2.Add(new AreaData
                    {
                        Id = Guid.NewGuid().ToString(),
                        NameParty = getData[0],
                        NameArea = getData[1],
                        LocationCode = getData[2],
                        IDProvince = getData[3],
                        District = getData[4],
                        SubDistrict = getData[5],
                    });
                }
            }
        }
        return listFile2;
    }
    // New Update 2/3/2019
    public List<ScorePollCsv> MockModelScorePollCsv()
    {
        var FilePath = @"ScorePoll.csv";
        var rnd = new Random();
        var listScorePollCsv = new List<ScorePollCsv>();
        using (var reader = new StreamReader(FilePath))
        {
            while (!reader.EndOfStream)
            {
                var getFromCsv = reader.ReadLine();
                var getLine = getFromCsv.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
                foreach (var item in getLine)
                {
                    var getDataFromLine = item.Split(',').ToList();
                    if (getDataFromLine[0] != "รหัสพรรค" && getDataFromLine[1] != "ชื่อเขต" && getDataFromLine[2] != "รหัสเขต" &&
                    getDataFromLine[3] != "ชื่อพรรค" && getDataFromLine[4] != "เปอร์เซ็น/คะแนน" && getDataFromLine[5] != "ภูมิภาค")
                    {
                        Int32.TryParse(getDataFromLine[4], out Int32 ScoreFromPoll);
                        listScorePollCsv.Add(new ScorePollCsv
                        {
                            Id = Guid.NewGuid().ToString(),
                            IdParty = getDataFromLine[0],
                            NameParty = getDataFromLine[3],
                            IdArea = getDataFromLine[2],
                            NameArea = getDataFromLine[1],
                            Region = getDataFromLine[5],
                            Score = ScoreFromPoll
                        });
                    }
                }
            }
        }

        listFullScorePoll = new List<ScorePollCsv>();
        foreach (var item in listScorePollCsv)
        {
            if (item.Score == 0 && item.NameParty == "บัตรดี")
            {
                var goodScoreRandom = rnd.Next(80000, 20001);
                listFullScorePoll.Add(new ScorePollCsv
                {
                    Id = item.Id,
                    IdParty = item.IdParty,
                    NameParty = item.NameParty,
                    IdArea = item.IdArea,
                    NameArea = item.NameArea,
                    Region = item.Region,
                    Score = goodScoreRandom
                });
            }
            else
            {
                listFullScorePoll.Add(item);
            }
        }
        // var sortData = listFullScorePoll.OrderBy(it => it.IdArea).ToList();
        return listFullScorePoll;
    }


}