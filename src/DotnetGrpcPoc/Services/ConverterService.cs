using System.Diagnostics;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using OpenTelemetry.Trace.Configuration;

namespace DotnetGrpcPoc
{
    public class ConverterService : Converter.ConverterBase
    {
        const int ReadBufSize = 1024;
        private readonly ILogger<ConverterService> _logger;
        private readonly Tracer _tracer;
        public ConverterService(ILogger<ConverterService> logger, TracerFactory tracerFactory)
        {
            _logger = logger;
            _tracer = tracerFactory.GetTracer("DotnetGrpcPoc");
        }

        public override async Task Convert(
            IAsyncStreamReader<Chunk> requestStream,
            IServerStreamWriter<Chunk> responseStream,
            ServerCallContext context)
        {
            var span = _tracer.StartSpan("Convert");
            var convertProcess = new Process();
            // Imagemagick
            convertProcess.StartInfo.FileName = "convert";
            convertProcess.StartInfo.Arguments = "- png:-";
            convertProcess.StartInfo.UseShellExecute = false;
            convertProcess.StartInfo.RedirectStandardInput = true;
            convertProcess.StartInfo.RedirectStandardOutput = true;
            convertProcess.Start();

            var standardInput = convertProcess.StandardInput.BaseStream;
            var standardOutput = convertProcess.StandardOutput.BaseStream;

            using (_tracer.StartActiveSpan("Receiving", out var receivingSpan))
            {
                var chunk = 0;
                var size = 0;

                while (await requestStream.MoveNext())
                {
                    var data = requestStream.Current.Data.ToByteArray();
                    chunk++;
                    size += data.Length;
                    await standardInput.WriteAsync(data, 0, data.Length);
                }

                receivingSpan.SetAttribute("Chunks", chunk);
                receivingSpan.SetAttribute("Size", size);
            }

            standardInput.Close();


            using (_tracer.StartActiveSpan("Sending", out var sendingSpan))
            {
                var buf = new byte[ReadBufSize];
                var size = 0;
                while (true)
                {
                    var outCount = await standardOutput.ReadAsync(buf, 0, ReadBufSize);
                    if (outCount == 0)
                    {
                        break;
                    }

                    size += outCount;

                    await responseStream.WriteAsync(new Chunk()
                    {
                        Data = ByteString.CopyFrom(buf, 0, outCount)
                    });
                }

                sendingSpan.SetAttribute("Size", size);
            }

            convertProcess.WaitForExit();
            span.SetAttribute("ExitCode", convertProcess.ExitCode);
            span.End();
        }
    }
}
