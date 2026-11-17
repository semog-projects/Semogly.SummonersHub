# Imagem da API para a Vercel (service "api" do vercel.json). O contexto de build é a raiz do repositório,
# por causa do Directory.Build.props, Directory.Packages.props, global.json e dos projetos referenciados.
# Local: docker build -f src/Services/Semogly.SummonersHub.Api/Dockerfile -t summonershub-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Restore em camada própria: só refaz quando algum projeto ou versão de pacote muda
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/Aspire/Semogly.SummonersHub.ServiceDefaults/*.csproj src/Aspire/Semogly.SummonersHub.ServiceDefaults/
COPY src/Core/Semogly.SummonersHub.Domain/*.csproj src/Core/Semogly.SummonersHub.Domain/
COPY src/Core/Semogly.SummonersHub.Application/*.csproj src/Core/Semogly.SummonersHub.Application/
COPY src/Core/Semogly.SummonersHub.Infrastructure/*.csproj src/Core/Semogly.SummonersHub.Infrastructure/
COPY src/Services/Semogly.SummonersHub.Api/*.csproj src/Services/Semogly.SummonersHub.Api/
RUN dotnet restore src/Services/Semogly.SummonersHub.Api/Semogly.SummonersHub.Api.csproj

COPY src/Aspire/Semogly.SummonersHub.ServiceDefaults/ src/Aspire/Semogly.SummonersHub.ServiceDefaults/
COPY src/Core/ src/Core/
COPY src/Services/Semogly.SummonersHub.Api/ src/Services/Semogly.SummonersHub.Api/
RUN dotnet publish src/Services/Semogly.SummonersHub.Api/Semogly.SummonersHub.Api.csproj \
    --no-restore --configuration Release --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./

# A Vercel encaminha o tráfego para a porta 80 (ou a definida em PORT); o Program.cs escuta em PORT
ENV PORT=80 \
    DOTNET_EnableDiagnostics=0 \
    ReverseProxy__TrustForwardedHeaders=true \
    Database__MigrateOnStartup=true

ENTRYPOINT ["dotnet", "Semogly.SummonersHub.Api.dll"]
