# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["global.json", "global.json"]
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["Directory.Packages.props", "Directory.Packages.props"]
COPY ["src/Tansu.Verify.Api/Tansu.Verify.Api.csproj", "src/Tansu.Verify.Api/"]

RUN dotnet restore "src/Tansu.Verify.Api/Tansu.Verify.Api.csproj"
COPY ["src/Tansu.Verify.Api/", "src/Tansu.Verify.Api/"]
RUN dotnet publish "src/Tansu.Verify.Api/Tansu.Verify.Api.csproj" -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8090
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app .
HEALTHCHECK --interval=10s --timeout=3s --start-period=15s --retries=8 \
    CMD curl -fsS http://127.0.0.1:8090/health || exit 1
ENTRYPOINT ["dotnet", "Tansu.Verify.Api.dll"]
