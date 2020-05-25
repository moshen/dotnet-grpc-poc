using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using OpenTelemetry.Trace.Configuration;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Samplers;

namespace DotnetGrpcPoc
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var jaegerUrl = Configuration["JaegerUrl"];
            var jaegerPort = Configuration.GetValue<int>("JaegerPort");

            if (jaegerUrl == null)
            {
                jaegerUrl = "localhost";
            }

            if (jaegerPort < 1)
            {
                jaegerPort = 6831;
            }

            services.AddOpenTelemetry((sp, builder) =>
            {
                // Sample everything
                builder.SetSampler(new AlwaysOnSampler())
                .UseJaeger(o =>
                {
                    o.ServiceName = "DotnetGrpcPoc";
                    o.AgentHost = jaegerUrl;
                    o.AgentPort = jaegerPort;
                })
                .AddRequestInstrumentation()
                .AddDependencyInstrumentation();
            });
            services.AddGrpc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<GreeterService>();
                endpoints.MapGrpcService<ConverterService>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}
