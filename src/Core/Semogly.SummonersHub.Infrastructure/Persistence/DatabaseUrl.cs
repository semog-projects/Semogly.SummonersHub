using Npgsql;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

/// <summary>
/// Converte a URL do PostgreSQL (formato <c>postgresql://usuario:senha@host:porta/banco?sslmode=require</c>,
/// usado pelo Neon/Vercel em <c>DATABASE_URL</c>) para a connection string do Npgsql.
/// </summary>
public static class DatabaseUrl
{
    /// <summary>Variáveis lidas, em ordem: a conexão direta primeiro (migrations usam lock de sessão, que não passa pelo pooler).</summary>
    public static readonly string[] EnvironmentVariables = ["DATABASE_URL_UNPOOLED", "DATABASE_URL"];

    public static string ToConnectionString(string url)
    {
        var uri = new Uri(url);
        if (uri.Scheme is not ("postgres" or "postgresql"))
        {
            throw new ArgumentException($"Esquema de URL não suportado: {uri.Scheme}.", nameof(url));
        }

        var credentials = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null,
            // Autenticação por senha: sem tentativa de Kerberos (a imagem não tem libgssapi)
            GssEncryptionMode = GssEncryptionMode.Disable,
        };

        foreach (var (key, value) in ParseQuery(uri.Query))
        {
            switch (key)
            {
                case "sslmode":
                    builder.SslMode = Enum.Parse<SslMode>(value.Replace("-", "", StringComparison.Ordinal), ignoreCase: true);
                    break;
                case "channel_binding":
                    builder.ChannelBinding = Enum.Parse<ChannelBinding>(value, ignoreCase: true);
                    break;
            }
        }

        return builder.ConnectionString;
    }

    private static IEnumerable<(string Key, string Value)> ParseQuery(string query) =>
        query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Select(parts => (
                Uri.UnescapeDataString(parts[0]).ToLowerInvariant(),
                parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty));
}
