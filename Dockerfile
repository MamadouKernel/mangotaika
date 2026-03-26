FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["MangoTaika.csproj", "./"]
RUN dotnet restore "MangoTaika.csproj"

COPY . .
RUN dotnet publish "MangoTaika.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

RUN mkdir -p /app/wwwroot/uploads \
    /app/data-protection-keys \
    /app/wwwroot/uploads/activites \
    /app/wwwroot/uploads/actualites \
    /app/wwwroot/uploads/commissaire \
    /app/wwwroot/uploads/formations \
    /app/wwwroot/uploads/galerie \
    /app/wwwroot/uploads/historique \
    /app/wwwroot/uploads/partenaires \
    /app/wwwroot/uploads/profils \
    /app/wwwroot/uploads/tickets \
    && chown -R $APP_UID /app

VOLUME ["/app/wwwroot/uploads", "/app/data-protection-keys"]

EXPOSE 8080

USER $APP_UID

ENTRYPOINT ["dotnet", "MangoTaika.dll"]
