FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

ENV SERVER_HOSTNAME=server
ENV SERVER_PORT=5000
ENV CLIENT_HOSTNAME=client
ENV CLIENT_PORT=0

COPY Client/*.csproj ./Client/
COPY Common/*.csproj ./Common/
RUN dotnet restore ./Client

COPY Client/. ./Client
COPY Common/. ./Common

RUN dotnet publish ./Client/Client.csproj -c Release -o /app/publish

ENTRYPOINT ["dotnet", "/app/publish/Client.dll"]
