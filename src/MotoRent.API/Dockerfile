#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8082
EXPOSE 8083

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/MotoRent.API/MotoRent.API.csproj", "src/MotoRent.API/"]
COPY ["src/MotoRent.Application/MotoRent.Application.csproj", "src/MotoRent.Application/"]
COPY ["src/MotoRent.MessageConsumers/MotoRent.MessageConsumers.csproj", "src/MotoRent.MessageConsumers/"]
COPY ["src/MotoRent.Infrastructure/MotoRent.Infrastructure.csproj", "src/MotoRent.Infrastructure/"]
RUN dotnet restore "./src/MotoRent.API/MotoRent.API.csproj"
COPY . .
WORKDIR "/src/src/MotoRent.API"
RUN dotnet build "./MotoRent.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MotoRent.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MotoRent.API.dll"]