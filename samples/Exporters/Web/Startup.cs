using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Exporter.Prometheus;
using OpenTelemetry.Trace.Configuration;
using OpenTelemetry.Metrics.Configuration;
using OpenTelemetry.Exporter.Web;

namespace API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });



            //services.AddOpenTelemetry((sp, builder) =>
            //{
            //    builder
            //        .SetSampler(Samplers.AlwaysSample)
            //        .UseApplicationInsights(telemetryConfiguration =>
            //        {
            //            var instrumentationKey = this.Configuration.GetValue<string>("ApplicationInsights:InstrumentationKey");
            //            telemetryConfiguration.InstrumentationKey = instrumentationKey;
            //        })
            //        .AddRequestCollector()
            //        .AddDependencyCollector();
            //});

            var exporter = new PrometheusExporter(new PrometheusExporterOptions
            {
                Url = "http://localhost:9184/metrics/",

            });
            services.AddOpenTelemetryMetrics((sp, builder) =>
            {
                builder.AddProcessorPipeline(b => b.SetExporter(exporter)).AddRequestCollector();
                
            });


            services.AddSingleton(new PrometheusExporterMetricsHttpServer(exporter));
            services.AddHostedService<PrometheusHostedService>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
