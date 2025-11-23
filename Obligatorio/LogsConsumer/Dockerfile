FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

COPY LogsConsumer/LogsConsumer.csproj ./LogsConsumer/
COPY Common/Common.csproj ./Common/

RUN dotnet restore ./LogsConsumer/LogsConsumer.csproj

COPY LogsConsumer/. ./LogsConsumer/
COPY Common/. ./Common/

RUN dotnet publish ./LogsConsumer/LogsConsumer.csproj -c Release -o /app/publish

ENTRYPOINT ["dotnet", "/app/publish/LogsConsumer.dll"]