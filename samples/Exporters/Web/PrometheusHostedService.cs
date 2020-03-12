using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Exporter.Prometheus;

namespace OpenTelemetry.Exporter.Web
{
    public class PrometheusHostedService : IHostedService, IDisposable
    {
        private readonly PrometheusExporterMetricsHttpServer server;
        public PrometheusHostedService(PrometheusExporterMetricsHttpServer server)
        {
            this.server = server;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.Run(()=> this.server.Start(cancellationToken));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => this.server.Stop());
        }
    }
}
