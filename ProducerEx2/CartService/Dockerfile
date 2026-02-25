# Use the ASP.NET Core runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CartService.csproj", "./"]
RUN dotnet restore "./CartService.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "CartService.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "CartService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Use a simple sleep to delay startup (30 seconds)
CMD ["sh", "-c", "sleep 30 && dotnet CartService.dll"]

