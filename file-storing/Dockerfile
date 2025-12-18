FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FileStoring.csproj", "./"]
RUN dotnet restore "FileStoring.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "FileStoring.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FileStoring.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "FileStoring.dll"]