FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app


COPY Server/*.csproj ./Server/
COPY Common/*.csproj ./Common/
COPY Domain/*.csproj ./Domain/
COPY Repository/*.csproj ./Repository/

RUN dotnet restore ./Server/Server.csproj

COPY Server/. ./Server
COPY Common/. ./Common
COPY Domain/. ./Domain
COPY Repository/. ./Repository

RUN dotnet publish ./Server/Server.csproj -c Release -o /app/publish

ENTRYPOINT ["dotnet", "/app/publish/Server.dll"]
