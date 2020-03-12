using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter.Prometheus;
using OpenTelemetry.Metrics;
using OpenTelemetry.Metrics.Implementation;

namespace webapi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Task.Run(() => { RunPrometheusExporter();});
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        public static object RunPrometheusExporter()
        {
            var promOptions = new PrometheusExporterOptions() { Url = "http://localhost:9184/metrics/" };

            Metric<long> metric = new Metric<long>("sample");
            var promExporter = new PrometheusExporter<long>(promOptions, metric);
            try
            {
                promExporter.Start();
                List<KeyValuePair<string, string>> label1 = new List<KeyValuePair<string, string>>();
                label1.Add(new KeyValuePair<string, string>("dim1", "value1"));
                var labelSet1 = new LabelSet(label1);
                metric.GetOrCreateMetricTimeSeries(labelSet1).Add(100);
                Task.Delay(30000).Wait();
                metric.GetOrCreateMetricTimeSeries(labelSet1).Add(200);
                Console.WriteLine("Look at metrics in Prometheus console!");
                Console.ReadLine();
            }
            finally
            {
                promExporter.Stop();
            }

            return null;
        }
    }
}
