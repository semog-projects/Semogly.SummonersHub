using Microsoft.AspNetCore.Identity;

namespace Semogly.SummonersHub.Infrastructure.Identity;

/// <summary>Usuário do site (ASP.NET Core Identity), com id Guid.</summary>
public sealed class ApplicationUser : IdentityUser<Guid>;
