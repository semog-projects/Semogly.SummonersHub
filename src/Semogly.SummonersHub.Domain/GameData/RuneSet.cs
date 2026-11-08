namespace Semogly.SummonersHub.Domain.GameData;

/// <summary>Set de runa. Os valores seguem o campo <c>set_id</c> do jogo.</summary>
public enum RuneSet
{
    Energy = 1,
    Guard = 2,
    Swift = 3,
    Blade = 4,
    Rage = 5,
    Focus = 6,
    Endure = 7,
    Fatal = 8,
    Despair = 10,
    Vampire = 11,
    Violent = 13,
    Nemesis = 14,
    Will = 15,
    Shield = 16,
    Revenge = 17,
    Destroy = 18,
    Fight = 19,
    Determination = 20,
    Enhance = 21,
    Accuracy = 22,
    Tolerance = 23,
    Seal = 24,
    Intangible = 25,

    /// <summary>Usado apenas por gems e grinds immemorial: valem para qualquer set.</summary>
    Immemorial = 99,
}

public static class RuneSetExtensions
{
    /// <summary>
    /// Quantidade de runas necessária para ativar o bônus do set. Intangible e Immemorial
    /// não têm bônus próprio (retornam 0).
    /// </summary>
    public static int RequiredPieces(this RuneSet set) => set switch
    {
        RuneSet.Swift or RuneSet.Rage or RuneSet.Fatal or RuneSet.Despair or RuneSet.Vampire or RuneSet.Violent => 4,
        RuneSet.Intangible or RuneSet.Immemorial => 0,
        _ => 2,
    };
}
