﻿// <copyright file="MeterBuilder.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.Resources;

namespace OpenTelemetry.Metrics.Configuration
{
    /// <summary>
    /// Build Tracers.
    /// </summary>
    public class MeterBuilder
    {
        internal MeterBuilder()
        {
        }

        internal Resource Resource { get; private set; } = Resource.Empty;

        internal List<MetricProcessorPipelineBuilder> ProcessingPipelines { get; private set; }

        internal List<CollectorFactory> CollectorFactories { get; private set; }

        /// <summary>
        /// Sets the <see cref="Resource"/> describing the app associated with all traces. Overwrites currently set resource. 
        /// </summary>
        /// <param name="resource">Resource to be associate with all traces.</param>
        /// <returns>Trace builder for chaining.</returns>
        public MeterBuilder SetResource(Resource resource)
        {
            this.Resource = resource ?? Resource.Empty;
            return this;
        }

        /// <summary>
        /// Adds processing and exporting pipeline. Pipelines are executed sequentially in the order they are added.
        /// </summary>
        /// <param name="configure">Function that configures pipeline.</param>
        public MeterBuilder AddProcessorPipeline(Action<MetricProcessorPipelineBuilder> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (this.ProcessingPipelines == null)
            {
                this.ProcessingPipelines = new List<MetricProcessorPipelineBuilder>();
            }

            var pipelineBuilder = new MetricProcessorPipelineBuilder();
            configure(pipelineBuilder);
            this.ProcessingPipelines.Add(pipelineBuilder);
            return this;
        }

        /// <summary>
        /// Adds auto-collectors for spans.
        /// </summary>
        /// <typeparam name="TCollector">Type of collector class.</typeparam>
        /// <param name="collectorFactory">Function that builds collector from <see cref="Meter"/>.</param>
        public MeterBuilder AddCollector<TCollector>(
            Func<Meter, TCollector> collectorFactory)
            where TCollector : class
        {
            if (collectorFactory == null)
            {
                throw new ArgumentNullException(nameof(collectorFactory));
            }

            if (this.CollectorFactories == null)
            {
                this.CollectorFactories = new List<CollectorFactory>();
            }

            this.CollectorFactories.Add(
                new CollectorFactory(
                    typeof(TCollector).Name, 
                    "semver:" + typeof(TCollector).Assembly.GetName().Version,
                    collectorFactory));

            return this;
        }

        ///// <summary>
        ///// Configures tracing options.
        ///// </summary>
        ///// <param name="options">Instance of <see cref="TracerConfiguration"/>.</param>
        // public TracerBuilder SetTracerOptions(TracerConfiguration options)
        // {
        //    this.TracerConfigurationOptions = options ?? throw new ArgumentNullException(nameof(options));
        //    return this;
        // }

        internal readonly struct CollectorFactory
        {
            public readonly string Name;
            public readonly string Version;
            public readonly Func<Meter, object> Factory;

            internal CollectorFactory(string name, string version, Func<Meter, object> factory)
            {
                this.Name = name;
                this.Version = version;
                this.Factory = factory;
            }
        }
    }
}
