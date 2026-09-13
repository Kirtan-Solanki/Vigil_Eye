FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["shared/VE.Shared.csproj", "shared/"]
COPY ["ingestion/VE.Ingestion.csproj", "ingestion/"]

RUN dotnet restore "ingestion/VE.Ingestion.csproj"

COPY ["shared/", "shared/"]
COPY ["ingestion/", "ingestion/"]

WORKDIR "/src/ingestion"
RUN dotnet build "VE.Ingestion.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VE.Ingestion.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VE.Ingestion.dll"]
