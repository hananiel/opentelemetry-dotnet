﻿// <copyright file="MeterBuilderExtensions.cs" company="OpenTelemetry Authors">
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
using OpenTelemetry.Collector.AspNetCore;

namespace OpenTelemetry.Metrics.Configuration
{
    /// <summary>
    /// Extension methods to simplify registering of data collection.
    /// </summary>
    public static class MeterBuilderExtensions
    {
        /// <summary>
        /// Enables the incoming requests automatic data collection.
        /// </summary>
        /// <param name="builder">Trace builder to use.</param>
        /// <returns>The instance of <see cref="MeterBuilder"/> to chain the calls.</returns>
        public static MeterBuilder AddRequestCollector(this MeterBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddCollector(m => new AspNetCoreHostingCollector(m));
        }

        /// <summary>
        /// Enables the incoming requests automatic data collection.
        /// </summary>
        /// <param name="builder">Trace builder to use.</param>
        /// <param name="configure">Configuration options.</param>
        /// <returns>The instance of <see cref="MeterBuilder"/> to chain the calls.</returns>
        public static MeterBuilder AddRequestCollector(this MeterBuilder builder, Action<AspNetCoreCollectorOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var options = new AspNetCoreCollectorOptions();
            configure(options);

            return builder.AddCollector(m => new AspNetCoreHostingCollector(m, options));
        }
    }
}
