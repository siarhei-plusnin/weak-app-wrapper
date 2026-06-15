FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/services/WeakAppWrapper.Processor/WeakAppWrapper.Processor.csproj", "src/services/WeakAppWrapper.Processor/"]
COPY ["src/shared/WeakAppWrapper.Contracts/WeakAppWrapper.Contracts.csproj", "src/shared/WeakAppWrapper.Contracts/"]
RUN dotnet restore "src/services/WeakAppWrapper.Processor/WeakAppWrapper.Processor.csproj"

COPY . .
RUN dotnet publish "src/services/WeakAppWrapper.Processor/WeakAppWrapper.Processor.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WeakAppWrapper.Processor.dll"]
