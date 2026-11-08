using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.GameData;

/// <summary>Times de referência das dungeons (somente leitura).</summary>
public interface IPveTeamCatalog
{
    IReadOnlyList<PveTeam> All();
}
