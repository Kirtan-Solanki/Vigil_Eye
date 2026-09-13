FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8082

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["shared/VE.Shared.csproj", "shared/"]
COPY ["alert-engine/VE.AlertEngine.csproj", "alert-engine/"]

RUN dotnet restore "alert-engine/VE.AlertEngine.csproj"

COPY ["shared/", "shared/"]
COPY ["alert-engine/", "alert-engine/"]

WORKDIR "/src/alert-engine"
RUN dotnet build "VE.AlertEngine.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VE.AlertEngine.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VE.AlertEngine.dll"]
