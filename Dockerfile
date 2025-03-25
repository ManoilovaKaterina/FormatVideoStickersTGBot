# Use .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set working directory in the container
WORKDIR /app

# Install FFMpeg
RUN apt-get update && apt-get install -y ffmpeg

# Copy the .csproj and restore dependencies first (to leverage Docker layer caching)
COPY *.csproj ./
RUN dotnet restore

# Now copy the rest of the application code
COPY . ./

# Build and publish the application
RUN dotnet publish -c Release -o out

# Use the runtime image to run the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Set the working directory in the container
WORKDIR /app

# Copy the published application from the build image
COPY --from=build /app/out /app

# Expose the port the app will listen on
EXPOSE 80

# Run the application
ENTRYPOINT ["dotnet", "videobot2.dll"]
