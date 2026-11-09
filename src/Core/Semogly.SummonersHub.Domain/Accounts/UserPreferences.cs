using Semogly.SummonersHub.Domain.Abstractions;

namespace Semogly.SummonersHub.Domain.Accounts;

/// <summary>Tema da interface.</summary>
public enum ThemePreference
{
    /// <summary>Segue o tema do sistema operacional.</summary>
    System = 0,
    Light = 1,
    Dark = 2,
}

/// <summary>Preferências do usuário, salvas no servidor para valer em qualquer dispositivo.</summary>
public sealed class UserPreferences
{
    /// <summary>Tema de quem ainda não escolheu (o app nasceu escuro).</summary>
    public const ThemePreference DefaultTheme = ThemePreference.Dark;

    // Construtor do EF Core
    private UserPreferences()
    {
    }

    private UserPreferences(Guid ownerId, DateTimeOffset updatedAt)
    {
        OwnerId = ownerId;
        Theme = DefaultTheme;
        UpdatedAt = updatedAt;
    }

    public Guid OwnerId { get; private set; }

    public ThemePreference Theme { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static UserPreferences Create(Guid ownerId, DateTimeOffset now)
    {
        if (ownerId == Guid.Empty)
        {
            throw new DomainException("As preferências precisam de um usuário.");
        }

        return new UserPreferences(ownerId, now);
    }

    public void SetTheme(ThemePreference theme, DateTimeOffset now)
    {
        if (!Enum.IsDefined(theme))
        {
            throw new DomainException($"Tema inválido: {theme}.");
        }

        Theme = theme;
        UpdatedAt = now;
    }
}

public interface IUserPreferencesRepository
{
    Task<UserPreferences?> FindAsync(Guid ownerId, CancellationToken cancellationToken);

    void Add(UserPreferences preferences);
}
