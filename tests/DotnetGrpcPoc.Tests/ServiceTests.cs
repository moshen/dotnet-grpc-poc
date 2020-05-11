using FluentAssertions;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DotnetGrpcPoc.Tests
{
    public class ServiceTests
    {
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private static string GuidS => Guid.NewGuid().ToString("N");
        public ServiceTests()
        {
            var builder = new HostBuilder().ConfigureWebHostDefaults(webHost =>
            {
                webHost
                    .UseTestServer()
                    .UseStartup<Startup>();
            });

            var _host = builder.Start();
            _server = _host.GetTestServer();

            // Need to set the response version to 2.0.
            // Required because of this TestServer issue - https://github.com/aspnet/AspNetCore/issues/16940
            var responseVersionHandler = new ResponseVersionHandler();
            responseVersionHandler.InnerHandler = _server.CreateHandler();

            _client = new HttpClient(responseVersionHandler);
            _client.BaseAddress = new Uri("http://localhost");
        }
        [Fact]
        public void AssureFixture()
        {
        }

        [Fact]
        public async void Test_HelloWorld()
        {
            var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions()
            {
                HttpClient = _client
            });
            var greeterClient = new Greeter.GreeterClient(channel);

            var reply = await greeterClient.SayHelloAsync(
                new HelloRequest { Name = "World" });
            reply.Message.Should().Be("Hello World");
        }

        [Fact]
        public async void Test_ConverterStream()
        {
            var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions()
            {
                HttpClient = _client
            });
            var converterClient = new Converter.ConverterClient(channel);

            var convertStream = converterClient.Convert();
            var testData = new List<Chunk>();
            testData.Add(new Chunk() {
                Data = Google.Protobuf.ByteString.CopyFromUtf8("1")
            });
            testData.Add(new Chunk() {
                Data = Google.Protobuf.ByteString.CopyFromUtf8("2")
            });
            testData.Add(new Chunk() {
                Data = Google.Protobuf.ByteString.CopyFromUtf8("3")
            });

            foreach (var item in testData)
            {
                convertStream.RequestStream.WriteAsync(item);
            }
            await convertStream.RequestStream.CompleteAsync();

            var t = new CancellationToken();
            var i = 0;
            var testDataArr = testData.ToArray();
            while (await convertStream.ResponseStream.MoveNext(t))
            {
                convertStream.ResponseStream.Current.Data.Should().BeEquivalentTo(testDataArr[i++].Data);
            }
        }

        private class ResponseVersionHandler : DelegatingHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = await base.SendAsync(request, cancellationToken);
                response.Version = request.Version;

                return response;
            }
        }
    }
}
