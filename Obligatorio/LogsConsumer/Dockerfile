FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

# Copiamos sólo el csproj y restauramos
COPY LogsConsumer/LogsConsumer.csproj ./LogsConsumer/
RUN dotnet restore ./LogsConsumer/LogsConsumer.csproj

# Copiamos el resto del código
COPY LogsConsumer/. ./LogsConsumer/

# Publicamos
RUN dotnet publish ./LogsConsumer/LogsConsumer.csproj -c Release -o /app/publish

# Vars por defecto para apuntar al broker del compose
ENV RABBIT_HOST=rabbitmq
ENV RABBIT_USER=app
ENV RABBIT_PASS=app

ENTRYPOINT ["dotnet", "/app/publish/LogsConsumer.dll"]
