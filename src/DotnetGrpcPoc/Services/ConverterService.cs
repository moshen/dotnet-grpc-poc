using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace DotnetGrpcPoc
{
    public class ConverterService : Converter.ConverterBase
    {
        private readonly ILogger<ConverterService> _logger;
        public ConverterService(ILogger<ConverterService> logger)
        {
            _logger = logger;
        }

        public override async Task Convert(
            IAsyncStreamReader<Chunk> requestStream,
            IServerStreamWriter<Chunk> responseStream,
            ServerCallContext context)
        {
            // Echo back bytes
            while(await requestStream.MoveNext())
            {
                await responseStream.WriteAsync(requestStream.Current);
            }
        }
    }
}
