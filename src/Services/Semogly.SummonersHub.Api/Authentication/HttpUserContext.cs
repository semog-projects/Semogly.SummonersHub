using System.Security.Claims;
using Semogly.SummonersHub.Application.Abstractions;

namespace Semogly.SummonersHub.Api.Authentication;

/// <summary>Lê o usuário autenticado do cookie ou bearer token da requisição atual.</summary>
internal sealed class HttpUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("A requisição não está autenticada; o endpoint precisa de RequireAuthorization.");
}
