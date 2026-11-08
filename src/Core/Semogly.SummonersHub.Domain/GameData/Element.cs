namespace Semogly.SummonersHub.Domain.GameData;

/// <summary>Elemento do monstro. Os valores seguem o campo <c>attribute</c> do jogo.</summary>
public enum Element
{
    Water = 1,
    Fire = 2,
    Wind = 3,
    Light = 4,
    Dark = 5,

    /// <summary>Monstros de material e evolução (Devilmon, Rainbowmon...).</summary>
    Pure = 6,
}

/// <summary>Arquétipo do monstro. Os valores seguem o campo <c>unit_style</c> do jogo.</summary>
public enum Archetype
{
    None = 0,
    Attack = 1,
    Defense = 2,
    Hp = 3,
    Support = 4,
    Material = 5,
}
