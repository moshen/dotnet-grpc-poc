const should = require('should');
const { promisify } = require('util');
const fileType = require('file-type');
const { 
  createGreeterClient,
  createConverterClient
} = require('../index.js');

describe('Greeter Client', function() {
  let client;
  before(function() {
    client = createGreeterClient();
  });

  it('Should respond appropriately', async function() {
    const res = await promisify(client.sayHello.bind(client))({ name: 'test' });
    should(res).have.property('message', 'Hello test');
  });
});

describe('Converter Client', function() {
  let client;
  before(function() {
    client = createConverterClient();
  });

  it('Should respond with a png', async function() {
    const data = Buffer.from(
      'iVBORw0KGgoAAAANSUhEUgAAAAQAAAAECAIAAAAmkwkpAAABhGlDQ1BJQ0MgcHJvZmlsZQAAKJF9kTtIw0Acxr+mlvqoOJhBxCFDdbLgC3HUKhShQqgVWnUwj76gSUOS4uIouBYcfCxWHVycdXVwFQTBB4iTo5Oii5T4v6TQIsaD4358d9/H3XcAVy8rmtUxBmi6baYScSGTXRXCrwihBzy6MC4pljEnikn4jq97BNh6F2NZ/uf+HL1qzlKAgEA8qximTbxBPL1pG4z3iXmlKKnE58SjJl2Q+JHpssdvjAsucyyTN9OpeWKeWCi0sdzGStHUiKeIo6qmUz6X8VhlvMVYK1eV5j3ZCyM5fWWZ6TSHkMAiliBCgIwqSijDRoxWnRQLKdqP+/gHXb9ILplcJSjkWEAFGiTXD/YHv7u18pMTXlIkDoReHOdjGAjvAo2a43wfO07jBAg+A1d6y1+pAzOfpNdaWvQI6NsGLq5bmrwHXO4AA0+GZEquFKTJ5fPA+xl9UxbovwW617zemvs4fQDS1FXyBjg4BEYKlL3u8+7O9t7+PdPs7wdU0XKboxbKiwAAAAlwSFlzAAAuIwAALiMBeKU/dgAAAAd0SU1FB+QFCxQND2DekzUAAAAZdEVYdENvbW1lbnQAQ3JlYXRlZCB3aXRoIEdJTVBXgQ4XAAAAIElEQVQI103KsREAIAADIeK5/8pvKzULBdtVFtTx2d8eTH4LA6nIyZMAAAAASUVORK5CYII=',
      'base64'
    );
    const converterStream = client.convert();
    const chunks = [];
    let resolve;
    const wait = new Promise((_resolve) => {
      resolve = _resolve;
    });
    converterStream.on('data', (chunk) => {
      chunks.push(chunk.data);
    });
    converterStream.on('end', () => {
      resolve();
    });
    converterStream.on('error', (err) => {
      console.error(err);
    });

    converterStream.write({ data });
    converterStream.end();

    await wait;

    const res = await fileType.fromBuffer(Buffer.concat(chunks));
    should(res).have.property('mime', 'image/png');
  });
});
