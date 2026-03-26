FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore
COPY DocumentProcessor.Api/DocumentProcessor.Api.csproj DocumentProcessor.Api/
RUN dotnet restore DocumentProcessor.Api/DocumentProcessor.Api.csproj

# Build
COPY . .
RUN dotnet publish DocumentProcessor.Api --configuration Release --output /app

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "DocumentProcessor.Api.dll"]
