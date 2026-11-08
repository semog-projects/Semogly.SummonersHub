# Semogly.SummonersHub

[![CI](https://github.com/semog-projects/Semogly.SummonersHub/actions/workflows/ci.yml/badge.svg)](https://github.com/semog-projects/Semogly.SummonersHub/actions/workflows/ci.yml)

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
src/Apps/web                               Front-end React
src/Aspire/Semogly.SummonersHub.AppHost    Orquestra Postgres, API e front
src/Aspire/Semogly.SummonersHub.ServiceDefaults  Telemetria, health checks, resiliência
src/Core/Semogly.SummonersHub.Domain          Entidades e regras do jogo (sem dependências)
src/Core/Semogly.SummonersHub.Application     Casos de uso (ICommandHandler / IQueryHandler, Result)
src/Core/Semogly.SummonersHub.Infrastructure  EF Core / PostgreSQL / Identity
src/Services/Semogly.SummonersHub.Api         Endpoints HTTP e composição
src/Tools/Semogly.SummonersHub.GameDataSync  Sincroniza o bestiário com o SWARFARM
tests/Semogly.SummonersHub.ArchitectureTests  Regra de dependência da Clean Architecture
tests/Semogly.SummonersHub.UnitTests       Testes de domínio e aplicação
tests/Semogly.SummonersHub.IntegrationTests  API em memória + PostgreSQL em container (Testcontainers)
```

As dependências apontam sempre para dentro: `Api → Infrastructure → Application → Domain`.

## Como executar

Pré-requisitos: .NET SDK 10, Node 24+ e Docker Desktop rodando.

```bash
dotnet run --project src/Aspire/Semogly.SummonersHub.AppHost
```

O link do dashboard do Aspire aparece no terminal; por ele se acessam o front (`web`), a API (`api`,
documentação em `/scalar`) e o pgAdmin. O front faz proxy de `/api` para a API, sem CORS.

## Autenticação

ASP.NET Core Identity com os endpoints prontos em `/api/auth` (`register`, `login`, `refresh`, `manage/info`...)
e `POST /api/auth/logout`. O front faz login com `?useCookies=true`: a sessão fica em um cookie HttpOnly
(`summonershub.auth`, SameSite=Lax), sem token acessível ao JavaScript. Outros clientes podem usar o bearer token
do login sem cookie.

- Endpoints de conta (`/api/imports`, `/api/accounts/**`) exigem login; `/api/info` e `/api/game-data` são públicos.
- O primeiro usuário que importa um invocador vira o dono dele (`WizardAccount`). Outro usuário recebe 409 ao
  importar e 404 ao consultar, para não revelar quais invocadores existem.
- Um usuário pode ter vários invocadores: `GET /api/accounts` lista o último import de cada um.
- As chaves de Data Protection (que assinam cookie e tokens) ficam no PostgreSQL, então a sessão sobrevive a
  restarts e funciona com mais de uma instância.

## Import da conta

O front lê o JSON do SW Exporter no navegador, remove os dados sensíveis e envia o resultado para
`POST /api/imports`. Cada import com conteúdo novo gera uma versão (`AccountSnapshot`) do invocador. Reenviar
o mesmo conteúdo, mesmo vindo de outro export, devolve a versão existente: a idempotência é feita pelo hash
SHA-256 do conteúdo, ignorando a data do export.

Migrations do EF Core ficam em `src/Core/Semogly.SummonersHub.Infrastructure/Persistence/Migrations` e são aplicadas
na inicialização em desenvolvimento. Para criar uma nova:

```bash
dotnet ef migrations add <Nome> --project src/Core/Semogly.SummonersHub.Infrastructure --output-dir Persistence/Migrations
```

## Dados do jogo

O bestiário (nome, elemento, arquétipo, estrelas e ícone de cada monstro) fica em
`src/Core/Semogly.SummonersHub.Infrastructure/GameData/monsters.json`, embarcado na API e carregado em memória.
Ele é gerado a partir da [API do SWARFARM](https://swarfarm.com/api/v2/). Para atualizar depois de um patch do jogo:

```bash
dotnet run --project src/Tools/Semogly.SummonersHub.GameDataSync
```

Sets, atributos de runa, efeitos de artefato e itens de craft são enums do domínio
(`Domain/GameData`), com rótulos em pt-BR no front (`src/Apps/web/src/lib/game/labels.ts`).
Os ícones dos monstros são servidos pelo SWARFARM.

## Testes

```bash
dotnet test                           # testes .NET (os de integração precisam do Docker)
cd src/Apps/web && npm test -- --run      # testes do front
```

### CI

O workflow `.github/workflows/ci.yml` roda a cada PR (inclusive os abertos sobre outros branches) e a cada push na
`main`, com dois jobs em paralelo:

- **API**: restore, build em Release (avisos como erro, inclusive vulnerabilidades de pacotes) e todos os testes
  .NET. Os de integração sobem o PostgreSQL com Testcontainers no Docker do runner.
- **Web**: `npm ci`, lint (oxlint), testes (Vitest) e build de produção (`tsc -b` + `vite build`).
