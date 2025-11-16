FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

# 1. Copia AMBOS archivos .csproj para restaurar dependencias
COPY LogsConsumer/LogsConsumer.csproj ./LogsConsumer/
COPY Common/Common.csproj ./Common/

# 2. Restaura las dependencias de LogsConsumer (que restaurará Common)
RUN dotnet restore ./LogsConsumer/LogsConsumer.csproj

# 3. Copia AMBOS directorios de código fuente
COPY LogsConsumer/. ./LogsConsumer/
COPY Common/. ./Common/

# 4. Publica
RUN dotnet publish ./LogsConsumer/LogsConsumer.csproj -c Release -o /app/publish

# 5. Define el punto de entrada
ENTRYPOINT ["dotnet", "/app/publish/LogsConsumer.dll"]