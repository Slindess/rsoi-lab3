FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY services/LoyaltyService/LoyaltyService.csproj services/LoyaltyService/
RUN dotnet restore services/LoyaltyService/LoyaltyService.csproj
COPY services/LoyaltyService services/LoyaltyService
RUN dotnet publish services/LoyaltyService/LoyaltyService.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "LoyaltyService.dll"]
