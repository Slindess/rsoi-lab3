FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY services/ReservationService/ReservationService.csproj services/ReservationService/
RUN dotnet restore services/ReservationService/ReservationService.csproj
COPY services/ReservationService services/ReservationService
RUN dotnet publish services/ReservationService/ReservationService.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ReservationService.dll"]
