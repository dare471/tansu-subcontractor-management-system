# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["global.json", "global.json"]
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["Directory.Packages.props", "Directory.Packages.props"]
COPY ["Tansu.sln", "Tansu.sln"]
COPY ["src/", "src/"]

RUN dotnet restore "src/Tansu.Worker/Tansu.Worker.csproj"
RUN dotnet publish "src/Tansu.Worker/Tansu.Worker.csproj" -c Release -o /app /p:UseAppHost=false /p:PreserveCompilationContext=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Tansu.Worker.dll"]
