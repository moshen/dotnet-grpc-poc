#!/usr/bin/env bash

# Run in parent dir

mkdir -p testOutput
dotnet run --project src/DotnetGrpcPoc/ >testOutput/server 2>&1 &
DOTNETPID=$!
sleep 5
cd src/NodeGrpcClient/ || exit 1
npx mocha
TESTSTATUS=$?
kill $DOTNETPID
exit $TESTSTATUS
