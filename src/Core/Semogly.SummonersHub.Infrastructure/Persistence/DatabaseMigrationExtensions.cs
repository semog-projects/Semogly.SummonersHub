using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Semogly.SummonersHub.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Aplica as migrations pendentes na inicialização. Roda em desenvolvimento e, em produção, quando
    /// <c>Database:MigrateOnStartup</c> está ligado (Vercel: não há etapa de release para rodá-las à parte).
    /// O EF Core serializa instâncias concorrentes com um lock no banco.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IHost app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SummonersHubDbContext>();
        await db.Database.MigrateAsync();
    }
}
