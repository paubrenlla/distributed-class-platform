# --- build ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Common/Common.csproj Common/
COPY ChatClient/ChatClient.csproj ChatClient/

RUN dotnet restore ChatClient/ChatClient.csproj

COPY Common/ Common/
COPY ChatClient/ ChatClient/

RUN dotnet publish ChatClient/ChatClient.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

ENV CHAT_WS_URL=ws://chatserver:8080/ws

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "ChatClient.dll"]