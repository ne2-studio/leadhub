FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["LeadHub.Api/LeadHub.Api.csproj", "LeadHub.Api/"]
COPY ["LeadHub/LeadHub.csproj", "LeadHub/"]
COPY ["LeadHub.Infra/LeadHub.Infra.csproj", "LeadHub.Infra/"]
RUN dotnet restore "LeadHub.Api/LeadHub.Api.csproj"

# Copy the rest of the code
COPY . .

# Build and publish
RUN dotnet build "LeadHub.Api/LeadHub.Api.csproj" -c Release -o /app/build
RUN dotnet publish "LeadHub.Api/LeadHub.Api.csproj" -c Release -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "LeadHub.Api.dll"]
