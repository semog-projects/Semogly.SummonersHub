using Semogly.SummonersHub.Application.Abstractions;
using Semogly.SummonersHub.Application.Abstractions.Messaging;
using Semogly.SummonersHub.Application.Accounts.GetMonsterDetail;
using Semogly.SummonersHub.Application.GameData;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Application.Accounts.SharedBuilds;

/// <param name="Version">Versão do import mostrada pelo link.</param>
public sealed record ShareLinkResponse(string Token, int Version, DateTimeOffset CreatedAt);

/// <summary>Build compartilhada: o detalhe do monstro na versão do link, sem dados do invocador.</summary>
public sealed record SharedBuildResponse(MonsterDetailResponse Monster, DateTimeOffset ImportedAt);

/// <summary>Cria (ou devolve, se já existe para a versão atual) o link da build do monstro no último import.</summary>
public sealed record CreateShareLinkCommand(long WizardId, long MonsterId) : ICommand<ShareLinkResponse>;

public sealed record ListShareLinksQuery(long WizardId, long MonsterId) : IQuery<IReadOnlyList<ShareLinkResponse>>;

public sealed record RevokeShareLinkCommand(string Token) : ICommand;

/// <summary>Consulta pública (sem login).</summary>
public sealed record GetSharedBuildQuery(string Token) : IQuery<SharedBuildResponse>;

public static class SharedBuildErrors
{
    public static readonly Error NotFound = Error.NotFound("SharedBuilds.NotFound", "Link de build não encontrado ou revogado.");
}

internal sealed class CreateShareLinkCommandHandler(
    IAccountSnapshotRepository snapshots,
    IWizardAccountRepository wizardAccounts,
    ISharedBuildRepository builds,
    IUserContext userContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ICommandHandler<CreateShareLinkCommand, ShareLinkResponse>
{
    public async Task<Result<ShareLinkResponse>> HandleAsync(CreateShareLinkCommand command, CancellationToken cancellationToken)
    {
        var result = await snapshots.GetOwnedLatestAsync(wizardAccounts, command.WizardId, userContext.UserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error;
        }

        var snapshot = result.Value;
        if (snapshot.Monsters.All(m => m.GameId != command.MonsterId))
        {
            return AccountErrors.MonsterNotFound(command.MonsterId);
        }

        // Um link por monstro e versão: clicar de novo devolve o mesmo
        var existing = (await builds.ListAsync(userContext.UserId, command.WizardId, command.MonsterId, cancellationToken))
            .FirstOrDefault(b => b.SnapshotId == snapshot.Id);
        if (existing is not null)
        {
            return new ShareLinkResponse(existing.Token, snapshot.Version, existing.CreatedAt);
        }

        var build = SharedBuild.Create(userContext.UserId, command.WizardId, command.MonsterId, snapshot.Id, timeProvider.GetUtcNow());
        builds.Add(build);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ShareLinkResponse(build.Token, snapshot.Version, build.CreatedAt);
    }
}

internal sealed class ListShareLinksQueryHandler(
    IAccountReadServiceForShares readService,
    IWizardAccountRepository wizardAccounts,
    ISharedBuildRepository builds,
    IUserContext userContext) : IQueryHandler<ListShareLinksQuery, IReadOnlyList<ShareLinkResponse>>
{
    public async Task<Result<IReadOnlyList<ShareLinkResponse>>> HandleAsync(ListShareLinksQuery query, CancellationToken cancellationToken)
    {
        if (!await wizardAccounts.IsOwnedByAsync(query.WizardId, userContext.UserId, cancellationToken))
        {
            return AccountErrors.NotFound(query.WizardId);
        }

        var links = await builds.ListAsync(userContext.UserId, query.WizardId, query.MonsterId, cancellationToken);
        var versions = await readService.VersionsOfAsync([.. links.Select(l => l.SnapshotId)], cancellationToken);

        return Result.Success<IReadOnlyList<ShareLinkResponse>>(
            [.. links.Select(l => new ShareLinkResponse(l.Token, versions.GetValueOrDefault(l.SnapshotId), l.CreatedAt))]);
    }
}

internal sealed class RevokeShareLinkCommandHandler(
    ISharedBuildRepository builds,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<RevokeShareLinkCommand>
{
    public async Task<Result> HandleAsync(RevokeShareLinkCommand command, CancellationToken cancellationToken)
    {
        var build = await builds.FindByTokenAsync(command.Token, cancellationToken);
        if (build is null || build.OwnerId != userContext.UserId)
        {
            return Result.Failure(SharedBuildErrors.NotFound);
        }

        builds.Remove(build);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class GetSharedBuildQueryHandler(
    ISharedBuildRepository builds,
    IAccountSnapshotRepository snapshots,
    IMonsterCatalog catalog) : IQueryHandler<GetSharedBuildQuery, SharedBuildResponse>
{
    public async Task<Result<SharedBuildResponse>> HandleAsync(GetSharedBuildQuery query, CancellationToken cancellationToken)
    {
        if (query.Token.Length != SharedBuild.TokenLength)
        {
            return SharedBuildErrors.NotFound;
        }

        var build = await builds.FindByTokenAsync(query.Token, cancellationToken);
        var snapshot = build is null ? null : await snapshots.GetByIdAsync(build.SnapshotId, cancellationToken);
        var monster = snapshot?.Monsters.FirstOrDefault(m => m.GameId == build!.MonsterId);
        if (snapshot is null || monster is null)
        {
            return SharedBuildErrors.NotFound;
        }

        return new SharedBuildResponse(MonsterDetailBuilder.Build(snapshot, monster, catalog), snapshot.ImportedAt);
    }
}

/// <summary>Versão (número) de cada snapshot, sem carregar o agregado.</summary>
public interface IAccountReadServiceForShares
{
    Task<Dictionary<Guid, int>> VersionsOfAsync(IReadOnlyCollection<Guid> snapshotIds, CancellationToken cancellationToken);
}
