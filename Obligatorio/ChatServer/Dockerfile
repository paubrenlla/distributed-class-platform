FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ChatServer/ChatServer.csproj ChatServer/
COPY AuthGrpc/AuthGrpc.csproj AuthGrpc/
RUN dotnet restore ChatServer/ChatServer.csproj

COPY ChatServer/ ChatServer/
COPY AuthGrpc/ AuthGrpc/
RUN dotnet publish ChatServer/ChatServer.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENV AUTH_GRPC_URL=http://authgrpc:5191
EXPOSE 8080
ENTRYPOINT ["dotnet", "ChatServer.dll"]
