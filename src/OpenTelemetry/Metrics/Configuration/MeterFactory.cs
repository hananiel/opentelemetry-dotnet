// <copyright file="MeterFactory.cs" company="OpenTelemetry Authors">
// Copyright 2018, OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry.Metrics.Export;

namespace OpenTelemetry.Metrics.Configuration
{
    public class MeterFactory : MeterFactoryBase
    {
        private readonly object lck = new object();
        private readonly Dictionary<MeterRegistryKey, Meter> meterRegistry = new Dictionary<MeterRegistryKey, Meter>();
        private readonly List<object> collectors = new List<object>();

        private readonly MetricProcessor metricProcessor;

        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private readonly TimeSpan collectionInterval = new TimeSpan(2000);
        private readonly Task worker;

        private Meter defaultMeter;

        private MeterFactory(MetricProcessor metricProcessor)
        {
            if (metricProcessor == null)
            {
                this.metricProcessor = new NoOpMetricProcessor();
            }
            else
            {
                this.metricProcessor = metricProcessor;
            }

            this.defaultMeter = new MeterSdk(string.Empty,
                       this.metricProcessor);
        }

        private MeterFactory(MeterBuilder builder)
        {
            var processorFactory = builder.ProcessingPipelines[0];
            this.metricProcessor = processorFactory.Build();

            this.defaultMeter = new MeterSdk(string.Empty, this.metricProcessor);

            this.worker = Task.Factory.StartNew(
                s => this.Worker((CancellationToken)s), this.cts.Token).ContinueWith((task) => Console.WriteLine("error"), TaskContinuationOptions.OnlyOnFaulted);
        }

        public static MeterFactory Create(MetricProcessor metricProcessor)
        {
            return new MeterFactory(metricProcessor);
        }

        /// <summary>
        /// Creates meter factory.
        /// </summary>
        /// <param name="configure">Function that configures tracerSdk factory.</param>
        public static MeterFactory Create(Action<MeterBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var builder = new MeterBuilder();
            configure(builder);
            var factory = new MeterFactory(builder);

            if (builder.CollectorFactories != null)
            {
                foreach (var collector in builder.CollectorFactories)
                {
                    var meter = factory.GetMeter(collector.Name, collector.Version);
                    factory.collectors.Add(collector.Factory(meter));
                }
            }

            return factory;
        }

        public override Meter GetMeter(string name, string version = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                return this.defaultMeter;
            }

            lock (this.lck)
            {
                var key = new MeterRegistryKey(name, version);
                if (!this.meterRegistry.TryGetValue(key, out var meter))
                {
                    meter = this.defaultMeter = new MeterSdk(name,
                        this.metricProcessor);

                    this.meterRegistry.Add(key, meter);
                }

                return meter;
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> CreateLibraryResourceLabels(string name, string version)
        {
            var labels = new Dictionary<string, string> { { "name", name } };
            if (!string.IsNullOrEmpty(version))
            {
                labels.Add("version", version);
            }

            return labels;
        }

        private async Task Worker(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(this.collectionInterval, cancellationToken).ConfigureAwait(false);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var sw = Stopwatch.StartNew();

                    foreach (var meter in this.meterRegistry.Values)
                    {
                        (meter as MeterSdk).Collect();
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var remainingWait = this.collectionInterval - sw.Elapsed;
                    if (remainingWait > TimeSpan.Zero)
                    {
                        await Task.Delay(remainingWait, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                var s = ex.Message;
            }
        }

        private readonly struct MeterRegistryKey
        {
            private readonly string name;
            private readonly string version;

            internal MeterRegistryKey(string name, string version)
            {
                this.name = name;
                this.version = version;
            }
        }
    }
}
