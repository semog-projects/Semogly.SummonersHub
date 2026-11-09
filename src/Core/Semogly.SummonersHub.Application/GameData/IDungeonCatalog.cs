using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.GameData;

/// <summary>Atributos dos inimigos das dungeons (somente leitura).</summary>
public interface IDungeonCatalog
{
    DungeonStats? Find(Dungeon dungeon);
}
