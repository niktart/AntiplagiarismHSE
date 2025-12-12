# STAGE 1 — build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем правильный csproj
COPY ["FileStoring.csproj", "./"]
RUN dotnet restore "FileStoring.csproj"

# Копируем остальной код
COPY . .
RUN dotnet publish "FileStoring.csproj" -c Release -o /app/publish

# STAGE 2 — runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FileStoring.dll"]
