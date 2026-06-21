# Use the SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish ./src/Service/Service.csproj -c Release -o /app

# Use the ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .

# Configure environment variables for HTTPS
ENV ASPNETCORE_URLS="https://+:8081;http://+:8080"
EXPOSE 8080 8081

ENTRYPOINT ["dotnet", "Service.dll"]