FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY services/GatewayService/GatewayService.csproj services/GatewayService/
RUN dotnet restore services/GatewayService/GatewayService.csproj
COPY services/GatewayService services/GatewayService
RUN dotnet publish services/GatewayService/GatewayService.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "GatewayService.dll"]
