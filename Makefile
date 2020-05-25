SHELL:=/bin/bash
.PHONEY: fmt test run run-jaeger bootstrap

test:
	dotnet test tests/DotnetGrpcPoc.Tests/
	tests/scripts/runClientServerTests.bash

fmt:
	dotnet tool run dotnet-format

run:
	dotnet run --project src/DotnetGrpcPoc/

run-jaeger:
	docker run -d --name jaeger -p 6831:6831/udp -p 16686:16686 jaegertracing/all-in-one:1.18

bootstrap:
	dotnet restore --locked-mode
	cd src/NodeGrpcClient/ \
		&& npm ci
