FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY AuthGrpc/AuthGrpc.csproj AuthGrpc/
COPY Domain/Domain.csproj Domain/
COPY Repository/Repository.csproj Repository/
RUN dotnet restore AuthGrpc/AuthGrpc.csproj

COPY AuthGrpc/ AuthGrpc/
COPY Domain/ Domain/
COPY Repository/ Repository/
RUN dotnet publish AuthGrpc/AuthGrpc.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5191
ENTRYPOINT ["dotnet", "AuthGrpc.dll"]
