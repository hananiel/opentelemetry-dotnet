// <copyright file="MetricProcessorPipelineBuilder.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.Metrics.Export;

namespace OpenTelemetry.Metrics.Configuration
{
    /// <summary>
    /// Configures exporting pipeline with chains of processors and exporter.
    /// </summary>
    public class MetricProcessorPipelineBuilder
    {
        private Func<MetricExporter, MetricProcessor> lastProcessorFactory;
        private List<Func<MetricProcessor, MetricProcessor>> processorChain;

        internal MetricProcessorPipelineBuilder()
        {
        }

        internal MetricExporter Exporter { get; private set; }

        internal List<MetricProcessor> Processors { get; private set; }

        /// <summary>
        /// Adds chained processor to the pipeline. Processors are executed in the order they were added.
        /// </summary>
        /// <param name="processorFactory">Function that creates processor from the next one.</param>
        public MetricProcessorPipelineBuilder AddProcessor(Func<MetricProcessor, MetricProcessor> processorFactory)
        {
            if (processorFactory == null)
            {
                throw new ArgumentNullException(nameof(processorFactory));
            }

            if (this.processorChain == null)
            {
                this.processorChain = new List<Func<MetricProcessor, MetricProcessor>>();
            }

            this.processorChain.Add(processorFactory);

            return this;
        }

        /// <summary>
        /// Configures last processor that invokes exporter. When not set, <see cref="UngroupedBatcher"/> is used.
        /// </summary>
        /// <param name="processorFactory">Factory that creates exporting processor from the exporter.</param>
        public MetricProcessorPipelineBuilder SetExportingProcessor(Func<MetricExporter, MetricProcessor> processorFactory)
        {
            this.lastProcessorFactory = processorFactory ?? throw new ArgumentNullException(nameof(processorFactory));
            return this;
        }

        /// <summary>
        /// Configures exporter.
        /// </summary>
        /// <param name="exporter">Exporter instance.</param>
        public MetricProcessorPipelineBuilder SetExporter(MetricExporter exporter)
        {
            this.Exporter = exporter ?? throw new ArgumentNullException(nameof(exporter));
            return this;
        }

        internal MetricProcessor Build()
        {
            this.Processors = new List<MetricProcessor>();

            MetricProcessor exportingProcessor = null;

            // build or create default exporting processor
            if (this.lastProcessorFactory != null)
            {
                exportingProcessor = this.lastProcessorFactory.Invoke(this.Exporter);
                this.Processors.Add(exportingProcessor);
            }
            else if (this.Exporter != null)
            {
                exportingProcessor = new UngroupedBatcher(this.Exporter);
                this.Processors.Add(exportingProcessor);
            }

            if (this.processorChain == null)
            {
                // if there is no chain, return exporting processor.
                if (exportingProcessor == null)
                {
                    exportingProcessor = new NoOpMetricProcessor();
                    this.Processors.Add(exportingProcessor);
                }

                return exportingProcessor;
            }

            var next = exportingProcessor;

            // build chain from the end to the beginning
            for (int i = this.processorChain.Count - 1; i >= 0; i--)
            {
                next = this.processorChain[i].Invoke(next);
                this.Processors.Add(next);
            }

            // return the last processor in the chain - it will be called first
            return this.Processors[this.Processors.Count - 1];
        }
    }
}
