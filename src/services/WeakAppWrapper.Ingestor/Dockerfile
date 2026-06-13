FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/services/WeakAppWrapper.Ingestor/WeakAppWrapper.Ingestor.csproj", "src/services/WeakAppWrapper.Ingestor/"]
RUN dotnet restore "src/services/WeakAppWrapper.Ingestor/WeakAppWrapper.Ingestor.csproj"

COPY . .
RUN dotnet publish "src/services/WeakAppWrapper.Ingestor/WeakAppWrapper.Ingestor.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WeakAppWrapper.Ingestor.dll"]
