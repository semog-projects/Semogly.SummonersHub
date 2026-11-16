using System.Globalization;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Anthropic.Models.Messages.Batches;

/// <summary>
/// Traduz para pt-BR as descrições das skills (o SWARFARM só tem inglês) pela Message Batches API do Claude
/// (50% mais barata, sem pressa). A tradução é incremental: o arquivo <c>skill-descriptions-ptbr.json</c> guarda
/// texto em inglês → português, e só textos novos ou alterados vão para o lote. Exige ANTHROPIC_API_KEY.
/// </summary>
internal static class SkillTranslator
{
    public const string FileName = "skill-descriptions-ptbr.json";

    private const string Model = "claude-opus-5-5";

    // Descrições por requisição: poucas o bastante para a saída caber folgada em MaxTokens
    private const int ChunkSize = 40;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private static readonly string[] ItemRequired = ["id", "pt"];

    private static readonly string[] RootRequired = ["translations"];

    private const string SystemPrompt = """
        Você traduz descrições de skills do jogo Summoners War: Sky Arena do inglês para o português do Brasil.
        O texto aparece no bestiário de um site para jogadores brasileiros, que conhecem os termos do jogo.

        Regras:
        - Traduza fielmente: mantenha números, porcentagens, turnos, condições e a ordem dos efeitos. Não resuma nem acrescente nada.
        - Mantenha em inglês nomes próprios de monstros, skills, sets de runa e itens.
        - Use estas abreviações de atributo, iguais às do site: ATK, DEF, VEL (Speed), HP, Taxa Crít. (Critical Rate),
          Dano Crít. (Critical Damage), RES (Resistance), PRE (Accuracy).
        - Glossário de efeitos (use sempre estes termos):
          Attack Bar → Barra de Ataque; Increase ATK → Aumento de ATK; Increase DEF → Aumento de DEF;
          Increase SPD → Aumento de VEL; Increase CRI Rate → Aumento de Taxa Crít.; Decrease ATK → Redução de ATK;
          Decrease DEF → Redução de DEF; Decrease SPD → Redução de VEL; Decrease Accuracy → Redução de PRE;
          Stun → Atordoamento; Freeze → Congelamento; Sleep → Sono; Provoke → Provocação; Silence → Silêncio;
          Oblivion → Esquecimento; Unrecoverable → Irrecuperável; Continuous Damage → Dano Contínuo;
          Bomb → Bomba; Brand → Marca; Glancing Hit → Golpe de Raspão; Crushing Hit → Golpe Esmagador;
          Immunity → Imunidade; Invincibility → Invencibilidade; Endure → Resistir à Morte; Shield → Escudo;
          Counterattack → Contra-ataque; Defend → Proteção; Reflect DMG → Refletir Dano; Recovery → Recuperação;
          Threat → Ameaça; Soul Protection → Proteção da Alma; Block Beneficial Effects → Bloqueio de Efeitos Benéficos;
          beneficial effects → efeitos benéficos; harmful effects → efeitos nocivos; cooltime → recarga;
          Leader Skill → skill de líder; target → alvo; ally/allies → aliado/aliados.
        - Escreva em português natural, na segunda ou terceira pessoa como no original.

        Você recebe um JSON com uma lista de itens {"id", "en"} e devolve um "pt" para cada id, sem pular nenhum.
        """;

    public static async Task<int> RunAsync(string gameDataDirectory, JsonSerializerOptions jsonOptions)
    {
        var pending = await PendingAsync(gameDataDirectory);
        var translations = await LoadAsync(gameDataDirectory);
        pending.RemoveAll(translations.ContainsKey);

        Console.WriteLine($"{pending.Count} descrições sem tradução");
        if (pending.Count == 0)
        {
            return 0;
        }

        AnthropicClient client = new();
        var chunks = pending.Chunk(ChunkSize).ToList();

        var batch = await client.Messages.Batches.Create(new BatchCreateParams
        {
            Requests = [.. chunks.Select((chunk, index) => new Request
            {
                CustomID = $"chunk-{index}",
                Params = new Params
                {
                    Model = Model,
                    MaxTokens = 16000,
                    // O prompt é igual em todas as requisições: o cache reaproveita o prefixo
                    System = new List<TextBlockParam> { new() { Text = SystemPrompt, CacheControl = new CacheControlEphemeral() } },
                    // Tradução direta: esforço baixo basta e reduz o custo
                    OutputConfig = new OutputConfig { Effort = Effort.Low, Format = new JsonOutputFormat { Schema = OutputSchema() } },
                    Messages = [new UserMessageParam { Content = JsonSerializer.Serialize(new { items = chunk.Select((en, id) => new { id, en }) }) }],
                },
            })],
        });

        Console.WriteLine($"Lote {batch.ID} criado com {chunks.Count} requisições; aguardando o processamento...");
        while (batch.ProcessingStatus != ProcessingStatus.Ended)
        {
            await Task.Delay(PollInterval);
            batch = await client.Messages.Batches.Retrieve(batch.ID);
            Console.WriteLine($"{batch.RequestCounts.Succeeded + batch.RequestCounts.Errored}/{chunks.Count} requisições concluídas");
        }

        var failures = 0;
        await foreach (var item in client.Messages.Batches.ResultsStreaming(batch.ID))
        {
            var chunk = chunks[int.Parse(item.CustomID["chunk-".Length..], CultureInfo.InvariantCulture)];
            if (!item.Result.TryPickSucceeded(out var succeeded) || succeeded.Message.StopReason != StopReason.EndTurn)
            {
                failures++;
                Console.Error.WriteLine($"{item.CustomID}: sem resultado ({item.Result.Value})");
                continue;
            }

            var text = string.Concat(succeeded.Message.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
            foreach (var translation in JsonSerializer.Deserialize<TranslationOutput>(text, jsonOptions)?.Translations ?? [])
            {
                // Ignora ids fora do lote e traduções vazias: o texto fica para o próximo sync
                if (translation.Id >= 0 && translation.Id < chunk.Length && !string.IsNullOrWhiteSpace(translation.Pt))
                {
                    translations[chunk[translation.Id]] = translation.Pt.Trim();
                }
            }
        }

        await SaveAsync(gameDataDirectory, translations, jsonOptions);
        var missing = pending.Count(en => !translations.ContainsKey(en));
        Console.WriteLine($"{translations.Count} traduções gravadas; {missing} ainda sem tradução, {failures} requisições com falha");
        return missing == 0 ? 0 : 1;
    }

    // Só as descrições das skills de monstros obteníveis (as que o bestiário mostra), sem repetição
    private static async Task<List<string>> PendingAsync(string gameDataDirectory)
    {
        using var monstersDoc = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(gameDataDirectory, "monsters.json")));
        var skillIds = monstersDoc.RootElement.EnumerateArray()
            .Where(m => m.GetProperty("obtainable").GetBoolean())
            .SelectMany(m => m.GetProperty("skillIds").EnumerateArray().Select(id => id.GetInt32()))
            .ToHashSet();

        using var skillsDoc = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(gameDataDirectory, "skills.json")));
        return [.. skillsDoc.RootElement.EnumerateArray()
            .Where(s => skillIds.Contains(s.GetProperty("id").GetInt32()))
            .Select(s => s.GetProperty("description").GetString())
            .OfType<string>()
            .Where(d => d.Length > 0)
            .Distinct()
            .Order(StringComparer.Ordinal)];
    }

    private static async Task<SortedDictionary<string, string>> LoadAsync(string gameDataDirectory)
    {
        var path = Path.Combine(gameDataDirectory, FileName);
        if (!File.Exists(path))
        {
            return new(StringComparer.Ordinal);
        }

        var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(path)) ?? [];
        return new(entries, StringComparer.Ordinal);
    }

    // Ordenado pelo texto em inglês: diffs pequenos entre sincronizações
    private static async Task SaveAsync(string gameDataDirectory, SortedDictionary<string, string> translations, JsonSerializerOptions jsonOptions)
    {
        var path = Path.Combine(gameDataDirectory, FileName);
        await using var file = File.Create(path);
        await JsonSerializer.SerializeAsync(file, translations, jsonOptions);
        Console.WriteLine($"Gravado em {Path.GetFullPath(path)}");
    }

    private static Dictionary<string, JsonElement> OutputSchema() => new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            translations = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new { id = new { type = "integer" }, pt = new { type = "string" } },
                    required = ItemRequired,
                    additionalProperties = false,
                },
            },
        }),
        ["required"] = JsonSerializer.SerializeToElement(RootRequired),
        ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
    };

    private sealed record TranslationOutput(IReadOnlyList<TranslationItem> Translations);

    private sealed record TranslationItem(int Id, string Pt);
}
