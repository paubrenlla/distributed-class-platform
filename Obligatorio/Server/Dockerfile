FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY Server/Server.csproj Server/
COPY Common/Common.csproj Common/
COPY Domain/Domain.csproj Domain/
COPY Repository/Repository.csproj Repository/
RUN dotnet restore Server/Server.csproj

COPY . .
RUN dotnet publish Server/Server.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Server.dll"]
