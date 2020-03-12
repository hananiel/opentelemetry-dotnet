﻿// <copyright file="DiagnosticSourceSubscriber.cs" company="OpenTelemetry Authors">
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

namespace OpenTelemetry.Collector
{
    public class DiagnosticSourceSubscriber : IDisposable, IObserver<DiagnosticListener>
    {
        private readonly Func<string, IListenerHandler> handlerFactory;
        private readonly Func<string, IListenerHandler> metricsHandlerFactory;
        private readonly Func<DiagnosticListener, bool> diagnosticSourceFilter;
        private readonly Func<string, object, object, bool> isEnabledFilter;
        private long disposed;
        private IDisposable allSourcesSubscription;
        private List<IDisposable> listenerSubscriptions;

        public DiagnosticSourceSubscriber(
            ListenerHandler handler,
            Func<string, object, object, bool> isEnabledFilter) : this(_ => handler, value => handler.SourceName == value.Name, isEnabledFilter)
        {
        }

        public DiagnosticSourceSubscriber(
           ListenerMetricsHandler handler,
           Func<string, object, object, bool> isEnabledFilter) : this(_ => handler, value => handler.SourceName == value.Name, isEnabledFilter)
        {
        }

        public DiagnosticSourceSubscriber(
            Func<string, ListenerHandler> handlerFactory,
            Func<DiagnosticListener, bool> diagnosticSourceFilter,
            Func<string, object, object, bool> isEnabledFilter)
        {
            this.listenerSubscriptions = new List<IDisposable>();
            this.handlerFactory = handlerFactory ?? throw new ArgumentNullException(nameof(handlerFactory));
            this.diagnosticSourceFilter = diagnosticSourceFilter;
            this.isEnabledFilter = isEnabledFilter;
        }

        public DiagnosticSourceSubscriber(
          Func<string, ListenerMetricsHandler> metricsHandlerFactory,
          Func<DiagnosticListener, bool> diagnosticSourceFilter,
          Func<string, object, object, bool> isEnabledFilter)
        {
            this.listenerSubscriptions = new List<IDisposable>();
            this.metricsHandlerFactory = metricsHandlerFactory ?? throw new ArgumentNullException(nameof(metricsHandlerFactory));
            this.diagnosticSourceFilter = diagnosticSourceFilter;
            this.isEnabledFilter = isEnabledFilter;
        }

        public void Subscribe()
        {
            if (this.allSourcesSubscription == null)
            {
                this.allSourcesSubscription = DiagnosticListener.AllListeners.Subscribe(this);
            }
        }

        public void OnNext(DiagnosticListener value)
        {
            if ((Interlocked.Read(ref this.disposed) == 0) &&
                this.diagnosticSourceFilter(value))
            {
                DiagnosticSourceListener listener;

                // TODO: Handle this better.
                if (this.handlerFactory != null) 
                {
                    var handler = this.handlerFactory(value.Name);
                    listener = new DiagnosticSourceListener(handler);
                }
                else
                {
                    var handler = this.metricsHandlerFactory(value.Name);
                    listener = new DiagnosticSourceListener(handler);
                }

                var subscription = this.isEnabledFilter == null ?
                    value.Subscribe(listener) :
                    value.Subscribe(listener, this.isEnabledFilter);

                lock (this.listenerSubscriptions)
                {
                    this.listenerSubscriptions.Add(subscription);
                }
            }
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref this.disposed, 1, 0) == 1)
            {
                return;
            }

            lock (this.listenerSubscriptions)
            {
                foreach (var listenerSubscription in this.listenerSubscriptions)
                {
                    listenerSubscription?.Dispose();
                }

                this.listenerSubscriptions.Clear();
            }

            this.allSourcesSubscription?.Dispose();
            this.allSourcesSubscription = null;
        }
    }
}
