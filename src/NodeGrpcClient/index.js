const path = require('path');
const { NodeTracerProvider } = require("@opentelemetry/node");
const { SimpleSpanProcessor } = require("@opentelemetry/tracing");
const { JaegerExporter } = require("@opentelemetry/exporter-jaeger");

const tracerProvider = new NodeTracerProvider();
tracerProvider.addSpanProcessor(
  new SimpleSpanProcessor(
    new JaegerExporter({
      serviceName: 'NodeGrpcClient',
      host: 'localhost',
      port: 6832
    })
  )
);
tracerProvider.register();

const grpc = require('grpc');
const protoLoader = require('@grpc/proto-loader');
const PROTO_PATH = path.resolve(path.join(__dirname, '../protos/'));
const greetPackageDefinition = protoLoader.loadSync(
  path.join(PROTO_PATH, 'greet.proto'),
  {
    keepCase: true,
    longs: String,
    enums: String,
    defaults: true,
    oneofs: true
  }
);
const greetProto = grpc.loadPackageDefinition(greetPackageDefinition).greet;
const convertPackageDefinition = protoLoader.loadSync(
  path.join(PROTO_PATH, 'convert.proto'),
  {
    keepCase: true,
    longs: String,
    enums: String,
    defaults: true,
    oneofs: true
  }
);
const convertProto = grpc.loadPackageDefinition(convertPackageDefinition).convert;

function createGreeterClient(
  host = 'localhost:5000',
  credentials = grpc.credentials.createInsecure()
) {
  return new greetProto.Greeter(host, credentials);
}

function createConverterClient(
  host = 'localhost:5000',
  credentials = grpc.credentials.createInsecure()
) {
  return new convertProto.Converter(host, credentials);
}

module.exports = {
  createGreeterClient,
  createConverterClient,
};
