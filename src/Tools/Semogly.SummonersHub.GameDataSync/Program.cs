using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

// Uso: dotnet run --project src/Tools/Semogly.SummonersHub.GameDataSync [all|monsters|dungeons|skills]
// Sobrescreve os JSONs embarcados na Infrastructure (GameData/monsters.json, dungeons.json e skills.json).
var target = args.Length > 0 ? args[0].ToLowerInvariant() : "all";
if (target is not ("all" or "monsters" or "dungeons" or "skills"))
{
    Console.Error.WriteLine("Uso: GameDataSync [all|monsters|dungeons|skills]");
    return 1;
}

var gameDataDirectory = Path.Combine(FindRepositoryRoot(), "src", "Core", "Semogly.SummonersHub.Infrastructure", "GameData");

using var http = new HttpClient { BaseAddress = new Uri("https://swarfarm.com/api/v2/") };
http.DefaultRequestHeaders.UserAgent.ParseAdd("Semogly.SummonersHub-GameDataSync/1.0");

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
};

if (target is "all" or "monsters")
{
    await SyncMonstersAsync();
}

if (target is "all" or "dungeons")
{
    await SyncDungeonsAsync();
}

if (target is "all" or "skills")
{
    await SyncSkillsAsync();
}

return 0;

async Task SyncMonstersAsync()
{
    // O monstro lista as skills pelo id interno do SWARFARM: traduz para o id do jogo, na ordem do slot
    var skillMap = await LoadSkillMapAsync();
    var monsters = new List<MonsterEntry>();
    var url = "monsters/?page_size=100";

    while (url is not null)
    {
        var page = await http.GetFromJsonAsync<SwarfarmPage<SwarfarmMonster>>(url)
            ?? throw new InvalidOperationException($"Resposta vazia do SWARFARM em {url}");

        monsters.AddRange(page.Results.Select(m => new MonsterEntry(
            m.Com2usId,
            m.FamilyId,
            m.Name,
            m.Element,
            m.Archetype,
            m.NaturalStars,
            m.AwakenLevel,
            m.ImageFilename,
            m.Homunculus,
            m.Obtainable,
            m.MaxLvlHp is > 0
                ? new MonsterStatsEntry(m.MaxLvlHp.Value, m.MaxLvlAttack ?? 0, m.MaxLvlDefense ?? 0, m.Speed ?? 0, m.Resistance ?? 0, m.Accuracy ?? 0, m.CritRate ?? 0, m.CritDamage ?? 0)
                : null,
            [.. (m.Skills ?? [])
                .Where(skillMap.ContainsKey)
                .Select(id => skillMap[id])
                .OrderBy(s => s.Slot)
                .Select(s => s.Com2usId)],
            m.LeaderSkill is { } leader
                ? new LeaderSkillEntry(leader.Attribute, leader.Amount, leader.Area, string.IsNullOrEmpty(leader.Element) ? null : leader.Element)
                : null,
            string.IsNullOrWhiteSpace(m.AwakenBonus) ? null : m.AwakenBonus.Trim(),
            [.. (m.Source ?? []).Select(source => source.Name).Distinct()])));

        Console.WriteLine($"{monsters.Count}/{page.Count} monstros");
        url = page.Next;
        await PauseAsync();
    }

    // Ordenado e sem duplicatas: diffs pequenos e estáveis entre sincronizações
    var ordered = monsters.DistinctBy(m => m.Com2usId).OrderBy(m => m.Com2usId).ToList();
    await WriteAsync("monsters.json", ordered);
    Console.WriteLine($"{ordered.Count} monstros gravados");
}

// Id interno do SWARFARM → (id do jogo, slot) de todas as skills
async Task<Dictionary<int, (int Com2usId, int Slot)>> LoadSkillMapAsync()
{
    var map = new Dictionary<int, (int, int)>();
    var url = "skills/?page_size=100";
    while (url is not null)
    {
        var page = await http.GetFromJsonAsync<SwarfarmPage<SwarfarmSkill>>(url)
            ?? throw new InvalidOperationException($"Resposta vazia do SWARFARM em {url}");
        foreach (var skill in page.Results)
        {
            map[skill.Id] = (skill.Com2usId, skill.Slot);
        }

        Console.WriteLine($"{map.Count}/{page.Count} habilidades (mapa)");
        url = page.Next;
        await PauseAsync();
    }

    return map;
}

// Dungeons cobertas pelos times PvE: id do SWARFARM → nome do enum Dungeon do domínio
async Task SyncDungeonsAsync()
{
    (int SwarfarmId, string Dungeon)[] dungeons =
    [
        (8001, "GiantsKeep"),
        (9001, "DragonsLair"),
        (6001, "Necropolis"),
        (9842, "SpiritualRealm"),
        (9779, "PunishersCrypt"),
    ];

    var entries = new List<DungeonEntry>();
    foreach (var (swarfarmId, dungeon) in dungeons)
    {
        var info = await http.GetFromJsonAsync<SwarfarmDungeon>($"dungeons/{swarfarmId}/")
            ?? throw new InvalidOperationException($"Dungeon {swarfarmId} não encontrada no SWARFARM");

        // A lista de andares não vem ordenada: lê todos e fica com o mais alto
        var levels = new List<SwarfarmLevel>();
        foreach (var levelId in info.Levels)
        {
            await PauseAsync();
            levels.Add(await http.GetFromJsonAsync<SwarfarmLevel>($"levels/{levelId}/")
                ?? throw new InvalidOperationException($"Andar {levelId} não encontrado no SWARFARM"));
        }

        var top = levels.Where(l => l.Waves.Count > 0).MaxBy(l => l.Floor)
            ?? throw new InvalidOperationException($"{info.Name} não tem andares com inimigos");
        var enemies = top.Waves.SelectMany(w => w.Enemies).ToList();
        var boss = top.Waves[^1].Enemies;

        entries.Add(new DungeonEntry(
            dungeon,
            info.Name,
            top.Floor,
            boss.Max(e => e.Resist),
            enemies.Max(e => e.Resist),
            boss.Max(e => e.Speed),
            enemies.Max(e => e.Speed)));

        Console.WriteLine($"{info.Name} B{top.Floor}: RES do boss {entries[^1].BossResistance}");
    }

    await WriteAsync("dungeons.json", entries);
}

// Habilidades: fórmula do multiplicador (bruta, para o front avaliar), golpes e bônus de dano de cada skill-up
async Task SyncSkillsAsync()
{
    var skills = new List<SkillEntry>();
    var url = "skills/?page_size=100";

    while (url is not null)
    {
        var page = await http.GetFromJsonAsync<SwarfarmPage<SwarfarmSkill>>(url)
            ?? throw new InvalidOperationException($"Resposta vazia do SWARFARM em {url}");

        skills.AddRange(page.Results.Select(s => new SkillEntry(
            s.Com2usId,
            s.Name,
            s.Slot,
            s.Hits,
            s.Passive,
            s.Aoe,
            s.MaxLevel,
            string.IsNullOrEmpty(s.MultiplierFormula) ? null : s.MultiplierFormula,
            string.IsNullOrEmpty(s.MultiplierFormula) ? null : s.MultiplierFormulaRaw,
            // Só os upgrades de dano; os demais (efeito, recarga...) valem 0 na posição
            [.. s.Upgrades.Select(u => u.Effect == "Damage +{0}%" ? u.Amount : 0)],
            string.IsNullOrWhiteSpace(s.Description) ? null : s.Description.Trim(),
            s.Cooltime,
            string.IsNullOrEmpty(s.IconFilename) ? null : s.IconFilename,
            s.LevelProgressDescription ?? [])));

        Console.WriteLine($"{skills.Count}/{page.Count} habilidades");
        url = page.Next;
        await PauseAsync();
    }

    await WriteAsync("skills.json", skills.DistinctBy(s => s.Id).OrderBy(s => s.Id).ToList());
}

async Task WriteAsync<T>(string fileName, T content)
{
    var path = Path.Combine(gameDataDirectory, fileName);
    await using var file = File.Create(path);
    await JsonSerializer.SerializeAsync(file, content, jsonOptions);
    Console.WriteLine($"Gravado em {Path.GetFullPath(path)}");
}

// API comunitária: espaça as requisições para não sobrecarregar o serviço
static Task PauseAsync() => Task.Delay(TimeSpan.FromMilliseconds(500));

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Semogly.SummonersHub.slnx")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? throw new InvalidOperationException("Raiz do repositório não encontrada.");
}

internal sealed record SwarfarmPage<T>(int Count, string? Next, IReadOnlyList<T> Results);

internal sealed record SwarfarmMonster(
    [property: JsonPropertyName("com2us_id")] int Com2usId,
    [property: JsonPropertyName("family_id")] int FamilyId,
    string Name,
    string Element,
    string Archetype,
    [property: JsonPropertyName("natural_stars")] int NaturalStars,
    [property: JsonPropertyName("awaken_level")] int AwakenLevel,
    [property: JsonPropertyName("image_filename")] string? ImageFilename,
    bool Homunculus,
    bool Obtainable,
    [property: JsonPropertyName("max_lvl_hp")] int? MaxLvlHp,
    [property: JsonPropertyName("max_lvl_attack")] int? MaxLvlAttack,
    [property: JsonPropertyName("max_lvl_defense")] int? MaxLvlDefense,
    int? Speed,
    int? Resistance,
    int? Accuracy,
    [property: JsonPropertyName("crit_rate")] int? CritRate,
    [property: JsonPropertyName("crit_damage")] int? CritDamage,
    IReadOnlyList<int>? Skills,
    [property: JsonPropertyName("leader_skill")] SwarfarmLeaderSkill? LeaderSkill,
    [property: JsonPropertyName("awaken_bonus")] string? AwakenBonus,
    IReadOnlyList<SwarfarmSource>? Source);

internal sealed record SwarfarmLeaderSkill(string Attribute, int Amount, string Area, string? Element);

internal sealed record SwarfarmSource(string Name);

/// <param name="MaxLevelStats">Atributos base em 6★ nível 40 (sem runas); nulo quando o SWARFARM não informa.</param>
/// <param name="SkillIds">Habilidades (id do jogo), na ordem do slot.</param>
internal sealed record MonsterEntry(
    int Com2usId,
    int FamilyId,
    string Name,
    string Element,
    string Archetype,
    int NaturalStars,
    int AwakenLevel,
    string? ImageFilename,
    bool IsHomunculus,
    bool Obtainable,
    MonsterStatsEntry? MaxLevelStats,
    IReadOnlyList<int> SkillIds,
    LeaderSkillEntry? LeaderSkill,
    string? AwakenBonus,
    IReadOnlyList<string> Sources);

/// <param name="Attribute">Atributo bonificado, como no SWARFARM (ex.: "Attack Power", "Attack Speed").</param>
/// <param name="Area">Onde vale: General, Element, Arena, Guild, Dungeon, Raid...</param>
/// <param name="Element">Elemento exigido quando <paramref name="Area"/> é Element.</param>
internal sealed record LeaderSkillEntry(string Attribute, int Amount, string Area, string? Element);

internal sealed record MonsterStatsEntry(
    int Hp,
    int Attack,
    int Defense,
    int Speed,
    int Resistance,
    int Accuracy,
    int CriticalRate,
    int CriticalDamage);

internal sealed record SwarfarmDungeon(int Id, string Name, IReadOnlyList<int> Levels);

internal sealed record SwarfarmLevel(int Id, int Floor, IReadOnlyList<SwarfarmWave> Waves);

internal sealed record SwarfarmWave(IReadOnlyList<SwarfarmEnemy> Enemies);

internal sealed record SwarfarmEnemy(int Speed, int Resist, int Hp);

internal sealed record SwarfarmSkill(
    int Id,
    [property: JsonPropertyName("com2us_id")] int Com2usId,
    string Name,
    int Slot,
    int Hits,
    bool Passive,
    bool Aoe,
    [property: JsonPropertyName("max_level")] int MaxLevel,
    [property: JsonPropertyName("multiplier_formula")] string? MultiplierFormula,
    [property: JsonPropertyName("multiplier_formula_raw")] string? MultiplierFormulaRaw,
    IReadOnlyList<SwarfarmSkillUpgrade> Upgrades,
    string? Description,
    int? Cooltime,
    [property: JsonPropertyName("icon_filename")] string? IconFilename,
    [property: JsonPropertyName("level_progress_description")] IReadOnlyList<string>? LevelProgressDescription);

internal sealed record SwarfarmSkillUpgrade(string Effect, int Amount);

/// <param name="Formula">Fórmula bruta do SWARFARM (JSON em texto): grupos avaliados da esquerda para a direita.</param>
/// <param name="DamageUpgrades">Bônus de dano (%) de cada skill-up, na ordem; 0 nos upgrades que não são de dano.</param>
internal sealed record SkillEntry(
    int Id,
    string Name,
    int Slot,
    int Hits,
    bool Passive,
    bool Aoe,
    int MaxLevel,
    string? FormulaText,
    string? Formula,
    IReadOnlyList<int> DamageUpgrades,
    string? Description,
    int? Cooldown,
    string? IconFilename,
    IReadOnlyList<string> LevelProgress);

/// <param name="BossResistance">Maior RES da última onda (boss).</param>
/// <param name="MaxResistance">Maior RES entre todos os inimigos do andar.</param>
internal sealed record DungeonEntry(
    string Dungeon,
    string Name,
    int Floor,
    int BossResistance,
    int MaxResistance,
    int BossSpeed,
    int MaxSpeed);
