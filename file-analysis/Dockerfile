FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["FileAnalysis.csproj", "./"]
RUN dotnet restore "FileAnalysis.csproj"

COPY . .
RUN dotnet publish "FileAnalysis.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FileAnalysis.dll"]
