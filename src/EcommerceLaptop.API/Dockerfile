FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/EcommerceLaptop.API/EcommerceLaptop.csproj", "EcommerceLaptop.API/"]
COPY ["src/EcommerceLaptop.Core/EcommerceLaptop.Core.csproj", "EcommerceLaptop.Core/"]
COPY ["src/EcommerceLaptop.Infrastructure/EcommerceLaptop.Infrastructure.csproj", "EcommerceLaptop.Infrastructure/"]

RUN dotnet restore "EcommerceLaptop.API/EcommerceLaptop.csproj"
COPY src/ .
WORKDIR "/src/EcommerceLaptop.API"
RUN dotnet build "EcommerceLaptop.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EcommerceLaptop.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EcommerceLaptop.dll"]
