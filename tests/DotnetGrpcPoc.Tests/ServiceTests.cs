using FluentAssertions;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
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

            // 16 pixel test png
            var testPng = Convert.FromBase64String(
                "iVBORw0KGgoAAAANSUhEUgAAAAQAAAAECAIAAAAmkwkpAAABhGlDQ1BJQ0MgcHJvZmlsZQAAKJF9kTtIw0Acxr+mlvqoOJhBxCFDdbLgC3HUKhShQqgVWnUwj76gSUOS4uIouBYcfCxWHVycdXVwFQTBB4iTo5Oii5T4v6TQIsaD4358d9/H3XcAVy8rmtUxBmi6baYScSGTXRXCrwihBzy6MC4pljEnikn4jq97BNh6F2NZ/uf+HL1qzlKAgEA8qximTbxBPL1pG4z3iXmlKKnE58SjJl2Q+JHpssdvjAsucyyTN9OpeWKeWCi0sdzGStHUiKeIo6qmUz6X8VhlvMVYK1eV5j3ZCyM5fWWZ6TSHkMAiliBCgIwqSijDRoxWnRQLKdqP+/gHXb9ILplcJSjkWEAFGiTXD/YHv7u18pMTXlIkDoReHOdjGAjvAo2a43wfO07jBAg+A1d6y1+pAzOfpNdaWvQI6NsGLq5bmrwHXO4AA0+GZEquFKTJ5fPA+xl9UxbovwW617zemvs4fQDS1FXyBjg4BEYKlL3u8+7O9t7+PdPs7wdU0XKboxbKiwAAAAlwSFlzAAAuIwAALiMBeKU/dgAAAAd0SU1FB+QFCxQND2DekzUAAAAZdEVYdENvbW1lbnQAQ3JlYXRlZCB3aXRoIEdJTVBXgQ4XAAAAIElEQVQI103KsREAIAADIeK5/8pvKzULBdtVFtTx2d8eTH4LA6nIyZMAAAAASUVORK5CYII="
            );

            var convertStream = converterClient.Convert();
            var testData = new List<Chunk>();

            // Split the data into chunks for writing
            for (var i = 0; i < testPng.Length; i += 100)
            {
                testData.Add(new Chunk()
                {
                    Data = Google.Protobuf.ByteString.CopyFrom(testPng, i, (testPng.Length - i) >= 100 ? 100 : testPng.Length - i)
                });
            }

            foreach (var item in testData)
            {
                await convertStream.RequestStream.WriteAsync(item);
            }
            await convertStream.RequestStream.CompleteAsync();

            var imageStream = new System.IO.MemoryStream();

            var t = new CancellationToken();
            while (await convertStream.ResponseStream.MoveNext(t))
            {
                convertStream.ResponseStream.Current.Data.WriteTo(imageStream);
            }

            var imageArr = imageStream.ToArray();
            Image.DetectFormat(imageArr).Should().NotBeNull();
            Image.DetectFormat(imageArr).DefaultMimeType.Should().Be("image/png");

            var image = Image.Load(imageArr);
            image.Width.Should().Be(4);
            image.Height.Should().Be(4);
            imageStream.Close();
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
