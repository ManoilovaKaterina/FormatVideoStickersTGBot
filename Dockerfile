# Install FFMpeg in the Docker container
RUN apt-get update && apt-get install -y ffmpeg

# Use .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy and restore dependencies
COPY . .
RUN dotnet restore

# Build the application
RUN dotnet publish -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /out .

CMD ["dotnet", "videobot2.dll"]
