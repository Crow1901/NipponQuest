# Use the .NET 10.0 SDK to build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY . ./
RUN dotnet publish -c Release -o out

# Use the matching .NET 10.0 ASP.NET runtime for production
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .

# Render environment configurations
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "NipponQuest.dll"]