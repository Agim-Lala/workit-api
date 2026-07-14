FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Workit.Core/Workit.Core.csproj", "Workit.Core/"]
COPY ["Workit.Api/Workit.Api.csproj", "Workit.Api/"]
RUN dotnet restore "Workit.Api/Workit.Api.csproj"

COPY . .

FROM build AS publish
WORKDIR /src/Workit.Api
RUN dotnet publish "Workit.Api.csproj" --no-restore -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Workit.Api.dll"]
