.PHONEY: fmt run

fmt:
	dotnet tool run dotnet-format -f .


run:
	dotnet run --project src/DotnetGrpcPoc/
