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
        public ConverterService(ILogger<ConverterService> logger)
        {
            _logger = logger;
        }

        public override async Task Convert(
            IAsyncStreamReader<Chunk> requestStream,
            IServerStreamWriter<Chunk> responseStream,
            ServerCallContext context)
        {
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

            while (await requestStream.MoveNext())
            {
                var data = requestStream.Current.Data.ToByteArray();
                await standardInput.WriteAsync(data, 0, data.Length);
            }

            standardInput.Close();

            var buf = new byte[ReadBufSize];
            while (true)
            {
                var outCount = await standardOutput.ReadAsync(buf, 0, ReadBufSize);
                if (outCount == 0)
                {
                    break;
                }

                await responseStream.WriteAsync(new Chunk()
                {
                    Data = ByteString.CopyFrom(buf, 0, outCount)
                });
            }

            convertProcess.WaitForExit();
        }
    }
}
