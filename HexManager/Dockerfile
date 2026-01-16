# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["HexManager.csproj", "./"]
RUN dotnet restore "HexManager.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "HexManager.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "HexManager.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Create directory for database
RUN mkdir -p /app/data

# Copy published app
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080
EXPOSE 8081

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the app
ENTRYPOINT ["dotnet", "HexManager.dll"]
