SHELL:=/bin/bash
.PHONEY: fmt test test-dotnet test-e2e run run-jaeger stop-jaeger restart-jaeger bootstrap bootstrap-dotnet bootstrap-node upgrade-dotnet-dependencies

test: test-dotnet test-e2e

test-dotnet:
	dotnet test tests/DotnetGrpcPoc.Tests/

test-e2e:
	tests/scripts/runClientServerTests.bash

fmt:
	dotnet tool run dotnet-format

run:
	dotnet run --project src/DotnetGrpcPoc/

run-jaeger:
	docker run -d --name jaeger -p 6831:6831/udp -p 6832:6832/udp -p 16686:16686 jaegertracing/all-in-one:1.18

stop-jaeger:
	docker stop jaeger || exit 0
	docker rm jaeger || exit 0

restart-jaeger: stop-jaeger run-jaeger

docker-build:
	docker build -t dotnet-grpc-poc -f src/DotnetGrpcPoc/Dockerfile ./

docker-run: docker-build
	docker run -d -p 5000:5000 --name dotnet-grpc-poc dotnet-grpc-poc

docker-stop:
	docker stop dotnet-grpc-poc || exit 0
	docker rm dotnet-grpc-poc || exit 0

docker-restart: docker-stop docker-run

upgrade-dotnet-dependencies:
	cd src/DotnetGrpcPoc/ \
	&& dotnet list DotnetGrpcPoc.csproj package \
	  | awk '/^ +> / && !/^ +> OpenTelemetry/{ print $$2 }' \
	  | xargs -n1 dotnet add DotnetGrpcPoc.csproj package \
	&& dotnet list DotnetGrpcPoc.csproj package \
	  | awk '/^ +> OpenTelemetry/{ print $$2 }' \
	  | xargs -n1 dotnet add DotnetGrpcPoc.csproj package --prerelease

	cd tests/DotnetGrpcPoc.Tests/ \
	&& dotnet list DotnetGrpcPoc.Tests.csproj package \
	  | awk '/^ +> / && !/^ +> OpenTelemetry/{ print $$2 }' \
	  | xargs -n1 dotnet add DotnetGrpcPoc.Tests.csproj package \
	&& dotnet list DotnetGrpcPoc.Tests.csproj package \
	  | awk '/^ +> OpenTelemetry/{ print $$2 }' \
	  | xargs -n1 dotnet add DotnetGrpcPoc.Tests.csproj package --prerelease

bootstrap: bootstrap-dotnet bootstrap-node

bootstrap-dotnet:
	dotnet restore --locked-mode

bootstrap-node:
	cd src/NodeGrpcClient/ \
		&& npm ci
