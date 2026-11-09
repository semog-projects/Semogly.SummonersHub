using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

// Uso: dotnet run --project src/Tools/Semogly.SummonersHub.GameDataSync [all|monsters|dungeons]
// Sobrescreve os JSONs embarcados na Infrastructure (GameData/monsters.json e GameData/dungeons.json).
var target = args.Length > 0 ? args[0].ToLowerInvariant() : "all";
if (target is not ("all" or "monsters" or "dungeons"))
{
    Console.Error.WriteLine("Uso: GameDataSync [all|monsters|dungeons]");
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

return 0;

async Task SyncMonstersAsync()
{
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
            m.Obtainable)));

        Console.WriteLine($"{monsters.Count}/{page.Count} monstros");
        url = page.Next;
        await PauseAsync();
    }

    // Ordenado e sem duplicatas: diffs pequenos e estáveis entre sincronizações
    var ordered = monsters.DistinctBy(m => m.Com2usId).OrderBy(m => m.Com2usId).ToList();
    await WriteAsync("monsters.json", ordered);
    Console.WriteLine($"{ordered.Count} monstros gravados");
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
    bool Obtainable);

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
    bool Obtainable);

internal sealed record SwarfarmDungeon(int Id, string Name, IReadOnlyList<int> Levels);

internal sealed record SwarfarmLevel(int Id, int Floor, IReadOnlyList<SwarfarmWave> Waves);

internal sealed record SwarfarmWave(IReadOnlyList<SwarfarmEnemy> Enemies);

internal sealed record SwarfarmEnemy(int Speed, int Resist, int Hp);

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
