using System.Diagnostics;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace DotnetGrpcPoc
{
    public class ConverterService : Converter.ConverterBase
    {
        const int ReadBufSize = 1024;
        private readonly ILogger<ConverterService> _logger;
        private readonly ActivitySource _activitySource;
        public ConverterService(ILogger<ConverterService> logger)
        {
            _logger = logger;
            _activitySource = new ActivitySource("DotnetGrpcPoc.ConverterService");
        }

        public override async Task Convert(
            IAsyncStreamReader<Chunk> requestStream,
            IServerStreamWriter<Chunk> responseStream,
            ServerCallContext context)
        {
            var span = _activitySource.StartActivity("Convert");
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

            using (var receivingSpan = _activitySource.StartActivity("Receiving"))
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

                receivingSpan.SetTag("Chunks", chunk);
                receivingSpan.SetTag("Size", size);
            }

            standardInput.Close();

            using (var sendingSpan = _activitySource.StartActivity("Sending"))
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

                sendingSpan.SetTag("Size", size);
            }

            convertProcess.WaitForExit();
            span.SetTag("ExitCode", convertProcess.ExitCode);
            span.Stop();
        }
    }
}
