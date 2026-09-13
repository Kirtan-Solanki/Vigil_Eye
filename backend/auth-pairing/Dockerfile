FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared project
COPY ["shared/VE.Shared.csproj", "shared/"]
COPY ["auth-pairing/VE.AuthPairing.csproj", "auth-pairing/"]

RUN dotnet restore "auth-pairing/VE.AuthPairing.csproj"

# Copy everything else
COPY ["shared/", "shared/"]
COPY ["auth-pairing/", "auth-pairing/"]

WORKDIR "/src/auth-pairing"
RUN dotnet build "VE.AuthPairing.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VE.AuthPairing.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VE.AuthPairing.dll"]
