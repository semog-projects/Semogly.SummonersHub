using System.Text.Json;
using System.Text.Json.Serialization;

namespace Semogly.SummonersHub.IntegrationTests.Infrastructure;

/// <summary>Mesmas opções de JSON da API (camelCase e enums como texto).</summary>
internal static class ApiJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
