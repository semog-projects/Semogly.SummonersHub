using Semogly.SummonersHub.Application.Accounts;
using Semogly.SummonersHub.Domain.Abstractions;
using Semogly.SummonersHub.Domain.GameData;

namespace Semogly.SummonersHub.Application.Accounts.ImportAccount;

/// <summary>
/// Validação do contrato antes de montar o domínio: limites de tamanho e códigos do jogo conhecidos.
/// Regras de consistência entre entidades ficam no próprio <c>AccountSnapshot</c>.
/// </summary>
internal static class ImportAccountValidator
{
    // Bem acima de uma conta grande, mas protege a API de payloads absurdos
    internal const int MaxMonsters = 5_000;
    internal const int MaxRunes = 15_000;
    internal const int MaxArtifacts = 5_000;
    internal const int MaxCraftItems = 5_000;

    public static Error? Validate(ImportAccountCommand command)
    {
        if (command.SchemaVersion != ImportAccountCommand.SupportedSchemaVersion)
        {
            return AccountErrors.UnsupportedSchemaVersion(command.SchemaVersion);
        }

        if (command.Wizard is null || command.Wizard.Id <= 0)
        {
            return AccountErrors.InvalidWizard;
        }

        if (command.Monsters.Count > MaxMonsters || command.Runes.Count > MaxRunes
            || command.Artifacts.Count > MaxArtifacts || command.CraftItems.Count > MaxCraftItems)
        {
            return AccountErrors.TooLarge;
        }

        var unknownCodes = UnknownCodes(command).Distinct().Take(10).ToList();
        return unknownCodes.Count > 0 ? AccountErrors.UnknownGameCodes(unknownCodes) : null;
    }

    private static IEnumerable<string> UnknownCodes(ImportAccountCommand command)
    {
        foreach (var monster in command.Monsters.Where(m => !Enum.IsDefined((Element)m.Attribute)))
        {
            yield return $"elemento {monster.Attribute}";
        }

        foreach (var rune in command.Runes)
        {
            if (!Enum.IsDefined((RuneSet)rune.SetId))
            {
                yield return $"set {rune.SetId}";
            }

            var stats = rune.Substats.Select(s => s.Type).Append(rune.MainStat.Type);
            if (rune.InnateStat is { } innate)
            {
                stats = stats.Append(innate.Type);
            }

            foreach (var stat in stats.Where(s => !Enum.IsDefined((RuneStat)s)))
            {
                yield return $"atributo de runa {stat}";
            }
        }

        foreach (var artifact in command.Artifacts)
        {
            if (!Enum.IsDefined((ArtifactType)artifact.Type))
            {
                yield return $"tipo de artefato {artifact.Type}";
            }

            var effects = artifact.Substats.Select(s => s.Type).Append(artifact.MainStat.Type);
            foreach (var effect in effects.Where(e => !Enum.IsDefined((ArtifactEffect)e)))
            {
                yield return $"efeito de artefato {effect}";
            }
        }

        foreach (var item in command.CraftItems.Where(c => !Enum.IsDefined((CraftItemType)c.CraftType)))
        {
            yield return $"tipo de craft {item.CraftType}";
        }
    }
}
