// <copyright file="AspNetCoreHostingCollector.cs" company="OpenTelemetry Authors">
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
using System.Threading;
using System.Threading.Tasks;
using OpenTelemetry.Collector.AspNetCore.Implementation;
using OpenTelemetry.Metrics;

namespace OpenTelemetry.Collector.AspNetCore
{
    /// <summary>
    /// Requests collector.
    /// </summary>
    public class AspNetCoreHostingCollector : IDisposable
    {
        private readonly DiagnosticSourceSubscriber diagnosticSourceSubscriber;

        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetCoreHostingCollector"/> class.
        /// </summary>
        /// <param name="meter">Meter to record metrics with.</param>
        public AspNetCoreHostingCollector(Meter meter)
            : this(meter, new AspNetCoreCollectorOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetCoreHostingCollector"/> class.
        /// </summary>
        /// <param name="meter">Meter to record traced with.</param>
        /// <param name="options">Configuration options for dependencies collector.</param>
        public AspNetCoreHostingCollector(Meter meter, AspNetCoreCollectorOptions options)
        {
            this.diagnosticSourceSubscriber = new DiagnosticSourceSubscriber(new HttpInMetricsListener("Microsoft.AspNetCore", meter, options), null);
            this.diagnosticSourceSubscriber.Subscribe();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.diagnosticSourceSubscriber?.Dispose();
        }
    }
}
