using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.GameData;

/// <summary>Bestiário do jogo (somente leitura).</summary>
public interface IMonsterCatalog
{
    MonsterDefinition? Find(int com2usId);

    /// <summary>Todos os monstros do bestiário (inclusive NPCs e bosses não obteníveis).</summary>
    IReadOnlyList<MonsterDefinition> All();

    /// <summary>Monstros que dá para criar à mão (com atributos no bestiário) cujo nome contém o termo.</summary>
    IReadOnlyList<MonsterDefinition> Search(string name, int limit);
}
