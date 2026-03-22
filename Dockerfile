FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FinancialPlatform.WebUI/FinancialPlatform.WebUI.csproj", "FinancialPlatform.WebUI/"]
COPY ["FinancialPlatform.Application/FinancialPlatform.Application.csproj", "FinancialPlatform.Application/"]
COPY ["FinancialPlatform.Core/FinancialPlatform.Core.csproj", "FinancialPlatform.Core/"]
COPY ["FinancialPlatform.Infrastructure/FinancialPlatform.Infrastructure.csproj", "FinancialPlatform.Infrastructure/"]
RUN dotnet restore "FinancialPlatform.WebUI/FinancialPlatform.WebUI.csproj"
COPY . .
WORKDIR "/src/FinancialPlatform.WebUI"
RUN dotnet build "FinancialPlatform.WebUI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FinancialPlatform.WebUI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FinancialPlatform.WebUI.dll"]
