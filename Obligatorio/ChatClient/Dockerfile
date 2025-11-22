FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Common/Common.csproj Common/
COPY AuthGrpc/AuthGrpc.csproj AuthGrpc/
COPY Domain/Domain.csproj Domain/
COPY Repository/Repository.csproj Repository/
COPY ChatClient/ChatClient.csproj ChatClient/
RUN dotnet restore ChatClient/ChatClient.csproj

COPY Common/ Common/
COPY AuthGrpc/ AuthGrpc/
COPY Domain/ Domain/
COPY Repository/ Repository/
COPY ChatClient/ ChatClient/
RUN dotnet publish ChatClient/ChatClient.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet","ChatClient.dll"]
