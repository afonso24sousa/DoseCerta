# Imagem da API DoseCerta (ASP.NET Core 8). Construída a partir da pasta backend/.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/DoseCerta.Api/DoseCerta.Api.csproj --configuration Release --output /app/publish /p:UseAppHost=false

# Imagem Debian: inclui os dados de fuso horário (Europe/Lisbon, RNF-047) e ICU.
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "DoseCerta.Api.dll"]
