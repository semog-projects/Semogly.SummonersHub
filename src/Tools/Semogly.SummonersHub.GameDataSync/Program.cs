using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

// Uso: dotnet run --project src/Tools/Semogly.SummonersHub.GameDataSync [caminho de saída]
// Sem argumento, sobrescreve o monsters.json embarcado na Infrastructure.
var outputPath = args.Length > 0
    ? args[0]
    : Path.Combine(FindRepositoryRoot(), "src", "Core", "Semogly.SummonersHub.Infrastructure", "GameData", "monsters.json");

using var http = new HttpClient { BaseAddress = new Uri("https://swarfarm.com/api/v2/") };
http.DefaultRequestHeaders.UserAgent.ParseAdd("Semogly.SummonersHub-GameDataSync/1.0");

var monsters = new List<MonsterEntry>();
var url = "monsters/?page_size=100";

while (url is not null)
{
    var page = await http.GetFromJsonAsync<SwarfarmPage>(url)
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

    // API comunitária: espaça as requisições para não sobrecarregar o serviço
    await Task.Delay(TimeSpan.FromMilliseconds(500));
}

// Ordenado e sem duplicatas: diffs pequenos e estáveis entre sincronizações
var ordered = monsters.DistinctBy(m => m.Com2usId).OrderBy(m => m.Com2usId).ToList();

await using (var file = File.Create(outputPath))
{
    await JsonSerializer.SerializeAsync(file, ordered, new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false,
    });
}

Console.WriteLine($"{ordered.Count} monstros gravados em {Path.GetFullPath(outputPath)}");

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Semogly.SummonersHub.slnx")))
    {
        directory = directory.Parent;
    }

    return directory?.FullName ?? throw new InvalidOperationException("Raiz do repositório não encontrada.");
}

internal sealed record SwarfarmPage(int Count, string? Next, IReadOnlyList<SwarfarmMonster> Results);

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
