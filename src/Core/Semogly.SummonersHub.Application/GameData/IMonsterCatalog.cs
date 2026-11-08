using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.GameData;

/// <summary>Bestiário do jogo (somente leitura).</summary>
public interface IMonsterCatalog
{
    MonsterDefinition? Find(int com2usId);
}
