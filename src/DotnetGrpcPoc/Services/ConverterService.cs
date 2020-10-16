using System.Diagnostics;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace DotnetGrpcPoc
{
    public class ConverterService : Converter.ConverterBase
    {
        const int ReadBufSize = 1024;
        private readonly ILogger<ConverterService> _logger;
        private readonly Tracer _tracer;

        private readonly ActivitySource _activitySource;
        public ConverterService(ILogger<ConverterService> logger, TracerProvider tracerProvider, ActivitySource activitySource)
        {
            _logger = logger;
            _tracer = tracerProvider.GetTracer("DotnetGrpcPoc");
            _activitySource = activitySource;
        }

        public override async Task Convert(
            IAsyncStreamReader<Chunk> requestStream,
            IServerStreamWriter<Chunk> responseStream,
            ServerCallContext context)
        {
            try {
            var activity = _activitySource.StartActivity("Loading DotnetGrpcPoc");
            activity?.Stop();
            } catch(System.Exception ex) {
                System.Console.Out.WriteLine("Caught something: %s", ex);
            }

            var span = _tracer.StartActiveSpan("Convert");
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

            var receivingSpan = _tracer.StartActiveSpan("Receiving");
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
                receivingSpan.End();
            }

            standardInput.Close();

            var sendingSpan = _tracer.StartActiveSpan("Sending");
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
                sendingSpan.End();
            }

            convertProcess.WaitForExit();
            span.SetAttribute("ExitCode", convertProcess.ExitCode);
            span.End();
        }
    }
}
