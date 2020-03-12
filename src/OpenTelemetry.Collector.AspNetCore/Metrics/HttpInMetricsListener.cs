// <copyright file="HttpInMetricsListener.cs" company="OpenTelemetry Authors">
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
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace OpenTelemetry.Collector.AspNetCore.Implementation
{
    internal class HttpInMetricsListener : ListenerMetricsHandler
    {
        private static readonly string UnknownHostName = "UNKNOWN-HOST";
        private readonly PropertyFetcher startContextFetcher = new PropertyFetcher("HttpContext");
        private readonly PropertyFetcher stopContextFetcher = new PropertyFetcher("HttpContext");
        private readonly PropertyFetcher beforeActionActionDescriptorFetcher = new PropertyFetcher("actionDescriptor");
        private readonly PropertyFetcher beforeActionAttributeRouteInfoFetcher = new PropertyFetcher("AttributeRouteInfo");
        private readonly PropertyFetcher beforeActionTemplateFetcher = new PropertyFetcher("Template");
        private readonly bool hostingSupportsW3C = false;
        private readonly AspNetCoreCollectorOptions options;
        private readonly Gauge<double> responseTimeMeasure;
        private readonly Gauge<double> serverCountMeasure;

      // private readonly LabelSet responseTimeLabelSet;
     //   private readonly LabelSet serverCountLabelSet;

        public HttpInMetricsListener(string name, Meter meter, AspNetCoreCollectorOptions options)
            : base(name, meter)
        {
            this.hostingSupportsW3C = typeof(HttpRequest).Assembly.GetName().Version.Major >= 3;
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.responseTimeMeasure = meter.CreateDoubleGauge("server.core.totalTime");
            this.serverCountMeasure = meter.CreateDoubleGauge("server.core.totalRequests");

        // var responseTimeLabels = new List<KeyValuePair<string, string>>();
        //    responseTimeLabels.Add(new KeyValuePair<string, string>("server.core.totalTime", "value1"));
        //    this.serverCountLabels = meter.GetLabelSet(new [] { })
        }

        public override void OnStartActivity(Activity activity, object payload)
        {
        }

        public override void OnStopActivity(Activity activity, object payload)
        {
            // const string EventNameSuffix = ".OnStopActivity";

            var current = Activity.Current;

            if (current.Duration.TotalMilliseconds > 0)
            {
                // ITagContext tagContext = GetTagContext(arg);
                // StatsRecorder
                //    .NewMeasureMap()
                //    .Put(responseTimeMeasure, current.Duration.TotalMilliseconds)
                //    .Put(serverCountMeasure, 1)
                //    .Record(tagContext);

              // var responseTimeLabelSet = this.responseTimeMeasure.get
                this.responseTimeMeasure.Set(default(SpanContext), current.Duration.TotalMilliseconds, LabelSet.BlankLabelSet);
                this.serverCountMeasure.Set(default(SpanContext), current.Duration.TotalMilliseconds, LabelSet.BlankLabelSet);
            }
        }

        public override void OnCustom(string name, Activity activity, object payload)
        {
        }

        private static string GetUri(HttpRequest request)
        {
            var builder = new StringBuilder();

            builder.Append(request.Scheme).Append("://");

            if (request.Host.HasValue)
            {
                builder.Append(request.Host.Value);
            }
            else
            {
                // HTTP 1.0 request with NO host header would result in empty Host.
                // Use placeholder to avoid incorrect URL like "http:///"
                builder.Append(UnknownHostName);
            }

            if (request.PathBase.HasValue)
            {
                builder.Append(request.PathBase.Value);
            }

            if (request.Path.HasValue)
            {
                builder.Append(request.Path.Value);
            }

            if (request.QueryString.HasValue)
            {
                builder.Append(request.QueryString);
            }

            return builder.ToString();
        }
    }
}
