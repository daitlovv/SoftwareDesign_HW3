FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
RUN mkdir -p /app/files
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FileStorage.dll"]
