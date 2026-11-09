using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.GameData;

/// <summary>Bestiário do jogo (somente leitura).</summary>
public interface IMonsterCatalog
{
    MonsterDefinition? Find(int com2usId);

    /// <summary>Monstros que dá para criar à mão (com atributos no bestiário) cujo nome contém o termo.</summary>
    IReadOnlyList<MonsterDefinition> Search(string name, int limit);
}
