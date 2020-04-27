.PHONEY: fmt test run

fmt:
	dotnet tool run dotnet-format -f .

test:
	dotnet test tests/DotnetGrpcPoc.Tests/

run:
	dotnet run --project src/DotnetGrpcPoc/
