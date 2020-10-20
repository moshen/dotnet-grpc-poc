using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace DotnetGrpcPoc
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        private readonly ActivitySource _activitySource;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
            _activitySource= new ActivitySource("DotnetGrpcPoc.GreeterService");
        }

        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            // This is an unnecessary manual span
            var span = _activitySource.StartActivity("SayHello");
            var task = Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
            var awaiter = task.GetAwaiter();
            awaiter.OnCompleted(() => span.Dispose());

            return task;
        }
    }
}
