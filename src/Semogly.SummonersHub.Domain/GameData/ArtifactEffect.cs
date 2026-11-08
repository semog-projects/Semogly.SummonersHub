namespace Semogly.SummonersHub.Domain.GameData;

/// <summary>Tipo de artefato. Os valores seguem o campo <c>type</c> do jogo.</summary>
public enum ArtifactType
{
    /// <summary>Artefato de elemento (slot esquerdo).</summary>
    Element = 1,

    /// <summary>Artefato de arquétipo (slot direito).</summary>
    Archetype = 2,
}

/// <summary>
/// Efeito de artefato. 100–102 são atributos principais; os demais, subs.
/// Os valores seguem o jogo (mesmo mapeamento do SW Exporter).
/// </summary>
public enum ArtifactEffect
{
    // Principais
    HpFlat = 100,
    AttackFlat = 101,
    DefenseFlat = 102,

    // Condicionais e de efeito
    AttackIncreasedByLostHp = 200,
    DefenseIncreasedByLostHp = 201,
    SpeedIncreasedByLostHp = 202,
    SpeedUnderInabilityEffects = 203,
    AttackIncreasingEffect = 204,
    DefenseIncreasingEffect = 205,
    SpeedIncreasingEffect = 206,
    CriticalRateIncreasingEffect = 207,
    CounterattackDamage = 208,
    CoopAttackDamage = 209,
    BombDamage = 210,
    ReflectedDamage = 211,
    CrushingHitDamage = 212,
    DamageReceivedUnderInability = 213,
    CriticalDamageReceived = 214,
    LifeDrain = 215,
    HpWhenRevived = 216,
    AttackBarWhenRevived = 217,
    AdditionalDamageByHp = 218,
    AdditionalDamageByAttack = 219,
    AdditionalDamageByDefense = 220,
    AdditionalDamageBySpeed = 221,
    CriticalDamageVsGoodHp = 222,
    CriticalDamageVsBadHp = 223,
    SingleTargetCriticalDamage = 224,
    CounterOrCoopAttackDamage = 225,
    AttackDefenseUpEffect = 226,

    // Elementais
    DamageDealtOnFire = 300,
    DamageDealtOnWater = 301,
    DamageDealtOnWind = 302,
    DamageDealtOnLight = 303,
    DamageDealtOnDark = 304,
    DamageReceivedFromFire = 305,
    DamageReceivedFromWater = 306,
    DamageReceivedFromWind = 307,
    DamageReceivedFromLight = 308,
    DamageReceivedFromDark = 309,

    // De habilidade
    Skill1CriticalDamage = 400,
    Skill2CriticalDamage = 401,
    Skill3CriticalDamage = 402,
    Skill4CriticalDamage = 403,
    Skill1Recovery = 404,
    Skill2Recovery = 405,
    Skill3Recovery = 406,
    Skill1Accuracy = 407,
    Skill2Accuracy = 408,
    Skill3Accuracy = 409,
    Skill3Or4CriticalDamage = 410,
    FirstAttackCriticalDamage = 411,
}
