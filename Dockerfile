# Imagem da API para o Render. Build: docker build -t rateio-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restaura primeiro, copiando só os .csproj: aproveita o cache de camadas entre builds
COPY Directory.Build.props ./
COPY src/Rateio.Dominio/Rateio.Dominio.csproj src/Rateio.Dominio/
COPY src/Rateio.Infra/Rateio.Infra.csproj src/Rateio.Infra/
COPY src/Rateio.Infra.Postgres/Rateio.Infra.Postgres.csproj src/Rateio.Infra.Postgres/
COPY src/Rateio.Api/Rateio.Api.csproj src/Rateio.Api/
RUN dotnet restore src/Rateio.Api/Rateio.Api.csproj

COPY src/Rateio.Dominio/ src/Rateio.Dominio/
COPY src/Rateio.Infra/ src/Rateio.Infra/
COPY src/Rateio.Infra.Postgres/ src/Rateio.Infra.Postgres/
COPY src/Rateio.Api/ src/Rateio.Api/
RUN dotnet publish src/Rateio.Api/Rateio.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# Usuário sem privilégios de administrador, já incluído na imagem oficial
USER $APP_UID

ENV ASPNETCORE_ENVIRONMENT=Production \
    Banco__Provedor=Postgres \
    Banco__AplicarMigrationsAoIniciar=true \
    Proxy__ConfiarEmCabecalhosEncaminhados=true

# O Render informa a porta na variável PORT
CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet Rateio.Api.dll"]
