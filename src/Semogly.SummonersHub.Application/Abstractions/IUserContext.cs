namespace Semogly.SummonersHub.Application.Abstractions;

/// <summary>Usuário autenticado da requisição atual.</summary>
public interface IUserContext
{
    /// <summary>Id do usuário. Lança exceção se a requisição não estiver autenticada.</summary>
    Guid UserId { get; }
}
