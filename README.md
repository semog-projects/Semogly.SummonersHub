# Semogly.SummonersHub

Companion web de **Summoners War**: o jogador exporta a conta com o
[SW Exporter](https://github.com/Xzandro/sw-exporter), envia o JSON e recebe um painel com monstros,
runas e artefatos.

> O export contém dados sensíveis (`session_key`, `session_key_node`, `account_info.token`).
> Eles são removidos **no navegador**, antes do upload. Nunca comite um export real no repositório.

## Stack

| Camada | Tecnologia |
|---|---|
| Orquestração local | .NET Aspire 13 (AppHost + ServiceDefaults) |
| API | ASP.NET Core 10 (minimal APIs), Clean Architecture em 4 projetos |
| Persistência | PostgreSQL + EF Core |
| Front-end | React 19 + Vite + TypeScript + Tailwind 4 + shadcn/ui + TanStack Query |
| Testes | xUnit v3 (testes de arquitetura), Vitest + Testing Library |

## Estrutura

```
apps/web/                                  Front-end React
src/Aspire/Semogly.SummonersHub.AppHost    Orquestra Postgres, API e front
src/Aspire/Semogly.SummonersHub.ServiceDefaults  Telemetria, health checks, resiliência
src/Semogly.SummonersHub.Domain            Entidades e regras do jogo (sem dependências)
src/Semogly.SummonersHub.Application       Casos de uso (ICommandHandler / IQueryHandler, Result)
src/Semogly.SummonersHub.Infrastructure    EF Core / PostgreSQL
src/Semogly.SummonersHub.Api               Endpoints HTTP e composição
tests/Semogly.SummonersHub.ArchitectureTests  Regra de dependência da Clean Architecture
tests/Semogly.SummonersHub.UnitTests       Testes de domínio e aplicação
tools/Semogly.SummonersHub.GameDataSync    Sincroniza o bestiário com o SWARFARM
```

As dependências apontam sempre para dentro: `Api → Infrastructure → Application → Domain`.

## Como executar

Pré-requisitos: .NET SDK 10, Node 24+ e Docker Desktop rodando.

```bash
dotnet run --project src/Aspire/Semogly.SummonersHub.AppHost
```

O link do dashboard do Aspire aparece no terminal; por ele se acessam o front (`web`), a API (`api`,
documentação em `/scalar`) e o pgAdmin. O front faz proxy de `/api` para a API, sem CORS.

## Dados do jogo

O bestiário (nome, elemento, arquétipo, estrelas e ícone de cada monstro) fica em
`src/Semogly.SummonersHub.Infrastructure/GameData/monsters.json`, embarcado na API e carregado em memória.
Ele é gerado a partir da [API do SWARFARM](https://swarfarm.com/api/v2/). Para atualizar depois de um patch do jogo:

```bash
dotnet run --project tools/Semogly.SummonersHub.GameDataSync
```

Sets, atributos de runa, efeitos de artefato e itens de craft são enums do domínio
(`Domain/GameData`), com rótulos em pt-BR no front (`apps/web/src/lib/game/labels.ts`).
Os ícones dos monstros são servidos pelo SWARFARM.

## Testes

```bash
dotnet test                           # testes .NET
cd apps/web && npm test -- --run      # testes do front
```
