# Use official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS build

# Install ffmpeg in the build image
RUN apt-get update && apt-get install -y ffmpeg

# Check FFmpeg version to verify it's installed correctly
RUN ffmpeg -version

# Set working directory
WORKDIR /app

# Copy the project files to the container
COPY . ./

# Restore and build the application
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Use a runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Copy the build output from the build stage to the runtime image
COPY --from=build /app/out /app

# Set the working directory
WORKDIR /app

# Expose the port the app will listen on
EXPOSE 80

# Run the application
ENTRYPOINT ["dotnet", "videobot2.dll"]
