using BenchmarkDotNet.Running;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Thousand.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "summarise")
            {
                SummariseBaselines();
            }
            else
            {
                BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            }
        }

        record Baseline(string ReportFile, string Index, string Sha1, string Title);
        record BDNRecord(string Method, string Input, string Mean, string Error, string StdDev);
        record SummaryRecord(string Baseline, string Mean);

        static void SummariseBaselines()
        {
            var baselines = Directory.EnumerateDirectories(@"../../../baselines").Select(dir =>
            {
                var report = Path.Combine(dir, "Thousand.Benchmarks.Compilation-report.csv");
                var baseline = Path.GetDirectoryName(report).Split("-");
                return new Baseline(report, baseline[0], baseline[1], baseline[2]);
            }).OrderBy(b => b.Index).ToList(); ;
            
            var data = new Dictionary<Baseline, BDNRecord[]>();
            foreach (var baseline in baselines)
            {
                using var reader = new StreamReader(baseline.ReportFile);
                using var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture);

                data[baseline] = csvReader.GetRecords<BDNRecord>().ToArray();
            }

            using var file = File.Open("../../../baselines/summary.csv", FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(file);
            using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csvWriter.WriteField("Baseline");            
            foreach (var rec in data[baselines.First()])
            {
                csvWriter.WriteField($"{rec.Input}({rec.Method})");
            }
            csvWriter.NextRecord();

            foreach (var baseline in baselines)
            {
                csvWriter.WriteField(baseline.Title);
                foreach (var rec in data[baseline])
                {
                    csvWriter.WriteField(rec.Mean[..^3]);
                }
                csvWriter.NextRecord();
            }
        }
    }
}
