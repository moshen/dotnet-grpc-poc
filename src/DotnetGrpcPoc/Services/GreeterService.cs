using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Configuration;

namespace DotnetGrpcPoc
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        private readonly Tracer _tracer;
        public GreeterService(ILogger<GreeterService> logger, TracerFactory tracerFactory)
        {
            _logger = logger;
            _tracer = tracerFactory.GetTracer("DotnetGrpcPoc");
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            // This is an unnecessary manual span
            var span = _tracer.StartSpan("SayHello");
            var task = Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
            var awaiter = task.GetAwaiter();
            awaiter.OnCompleted(() => span.End());

            return task;
        }
    }
}
