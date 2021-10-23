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

        record Baseline(string ReportDirectory, string Index, string Sha1, string Title);
        record BDNRecord(string Method, string Input, string Mean, string Error, string StdDev);
        record SummaryRecord(string Baseline, string Mean);

        static void SummariseBaselines()
        {
            var baselines = Directory.EnumerateDirectories(@"../../../baselines").Select(dir =>
            {
                var baseline = Path.GetFileName(dir).Split("-");
                return new Baseline(dir, baseline[0], baseline[1], baseline[2]);
            }).OrderBy(b => b.Index).ToList(); ;
            
            var compilationData = new Dictionary<Baseline, BDNRecord[]>();
            var stagesData = new Dictionary<Baseline, BDNRecord[]>();
            foreach (var baseline in baselines)
            {
                var compilationReport = Path.Combine(baseline.ReportDirectory, "Thousand.Benchmarks.Compilation-report.csv");
                {
                    using var reader = new StreamReader(compilationReport);
                    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                    compilationData[baseline] = csv.GetRecords<BDNRecord>().ToArray();
                }

                var stagesReport = Path.Combine(baseline.ReportDirectory, "Thousand.Benchmarks.Stages-report.csv");
                {
                    using var reader = new StreamReader(stagesReport);
                    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                    stagesData[baseline] = csv.GetRecords<BDNRecord>().ToArray();
                }
            }

            SummariseCompilation(baselines, compilationData);
            SummariseStages(baselines, stagesData);
        }

        static void SummariseCompilation(List<Baseline> baselines, Dictionary<Baseline, BDNRecord[]> data)
        {
            using var file = File.Open("../../../baselines/Compilation.csv", FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(file);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteField("Baseline");
            foreach (var rec in data[baselines.First()])
            {
                csv.WriteField($"{rec.Input}({rec.Method})");
            }
            csv.NextRecord();

            foreach (var baseline in baselines)
            {
                csv.WriteField(baseline.Title);
                foreach (var rec in data[baseline])
                {
                    csv.WriteField(rec.Mean[..^3]);
                }
                csv.NextRecord();
            }
        }

        static void SummariseStages(List<Baseline> baselines, Dictionary<Baseline, BDNRecord[]> data)
        {
            using var file = File.Open("../../../baselines/Stages.csv", FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(file);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            csv.WriteField("Baseline");
            var inputs = data.Values.SelectMany(r => r).Select(r => r.Input).Distinct();
            var methods = data.Values.SelectMany(r => r).Select(r => r.Method).Distinct();
            var pairs = inputs.SelectMany(input => methods.Select(method => (input, method))).Distinct().OrderBy(p => p.input).ThenBy(p => Enum.Parse<Stages>(p.method));
            foreach (var pair in pairs)
            {
                csv.WriteField($"{pair.input}({pair.method})");
            }
            csv.NextRecord();

            foreach (var baseline in baselines)
            {
                csv.WriteField(baseline.Title);
                foreach (var pair in pairs)
                {
                    var rec = data[baseline].SingleOrDefault(r => r.Input == pair.input && r.Method == pair.method);
                    if (rec == null)
                    {
                        csv.WriteField("");
                    }
                    else
                    {
                        csv.WriteField(rec.Mean[..^3]);
                    }
                }
                csv.NextRecord();
            }
        }

        private enum Stages
        {
            Preprocess,
            Typecheck,
            Parse,                       
            Evaluate,
            Compose,
            Render,
        }
    }
}
