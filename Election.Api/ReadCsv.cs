
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Election.Api.Models;

public class ReadCsv
{
    public List<ElectionModel> ListElection { get; set; }
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
        return ListElection;
    }
}