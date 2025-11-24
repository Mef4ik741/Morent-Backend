FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["WebAPI/WebAPI.csproj", "WebAPI/"]
COPY ["WebAPI.Application/WebAPI.Application.csproj", "WebAPI.Application/"]
COPY ["WebAPI.Domain/WebAPI.Domain.csproj", "WebAPI.Domain/"]
COPY ["WebAPI.Infrastructure/WebAPI.Infrastructure.csproj", "WebAPI.Infrastructure/"]
RUN dotnet restore "WebAPI/WebAPI.csproj"

COPY . .
WORKDIR /src/WebAPI
RUN dotnet publish "WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "WebAPI.dll"]
