FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/services/WeakAppWrapper.WebApi/WeakAppWrapper.WebApi.csproj", "src/services/WeakAppWrapper.WebApi/"]
RUN dotnet restore "src/services/WeakAppWrapper.WebApi/WeakAppWrapper.WebApi.csproj"

COPY . .
RUN dotnet publish "src/services/WeakAppWrapper.WebApi/WeakAppWrapper.WebApi.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WeakAppWrapper.WebApi.dll"]
