FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as builder

WORKDIR /app
COPY ./NuGet.Config ./
COPY ./src/DotnetGrpcPoc/DotnetGrpcPoc.csproj \
  ./src/DotnetGrpcPoc/packages.lock.json \
  ./

RUN dotnet restore --locked-mode

COPY ./src/DotnetGrpcPoc/ ./
COPY ./src/protos ../protos

RUN dotnet publish -c Release -o out

#---

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1

RUN apt-get update \
  && apt-get install --no-install-recommends -y imagemagick \
  && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=builder /app/out ./
CMD ["dotnet", "DotnetGrpcPoc.dll"]
EXPOSE 5000

