FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY services/PaymentService/PaymentService.csproj services/PaymentService/
RUN dotnet restore services/PaymentService/PaymentService.csproj
COPY services/PaymentService services/PaymentService
RUN dotnet publish services/PaymentService/PaymentService.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "PaymentService.dll"]
