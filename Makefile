SHELL:=/bin/bash
.PHONEY: fmt test run bootstrap

test:
	dotnet test tests/DotnetGrpcPoc.Tests/
	tests/scripts/runClientServerTests.bash

fmt:
	dotnet tool run dotnet-format

run:
	dotnet run --project src/DotnetGrpcPoc/

bootstrap:
	dotnet restore --locked-mode
	cd src/NodeGrpcClient/ \
		&& npm ci
