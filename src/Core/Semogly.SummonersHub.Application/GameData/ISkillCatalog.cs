using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.GameData;

/// <summary>Habilidades dos monstros (somente leitura).</summary>
public interface ISkillCatalog
{
    SkillDefinition? Find(int skillId);
}
