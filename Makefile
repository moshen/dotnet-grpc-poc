SHELL:=/bin/bash
.PHONEY: fmt test run run-jaeger stop-jaeger restart-jaeger bootstrap

test:
	dotnet test tests/DotnetGrpcPoc.Tests/
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

bootstrap:
	dotnet restore --locked-mode
	cd src/NodeGrpcClient/ \
		&& npm ci
