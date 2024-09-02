# Step 1: Use an official .NET SDK image as a build environment
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

# Step 2: Copy the CSPROJ file and restore any dependencies
COPY *.csproj ./
RUN dotnet restore

# Step 3: Copy the rest of the application code
COPY . ./

# Step 4: Build the application
RUN dotnet publish -c Release -o out

# Step 5: Use an official .NET runtime as a runtime environment
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app

# Step 6: Copy the published output from the build environment
COPY --from=build-env /app/out .

# Step 7: Set the entry point to your application
ENTRYPOINT ["dotnet", "ConfessBot.dll"]
