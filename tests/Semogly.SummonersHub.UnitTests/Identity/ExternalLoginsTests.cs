using Semogly.SummonersHub.Infrastructure.Identity;

namespace Semogly.SummonersHub.UnitTests.Identity;

public class ExternalLoginsTests
{
    [Theory]
    [InlineData("/contas/1?aba=runas", "/contas/1?aba=runas")]
    [InlineData("/", "/")]
    [InlineData(null, "/")]
    [InlineData("", "/")]
    [InlineData("//evil.example", "/")] // outra origem (protocol-relative)
    [InlineData(@"/\evil.example", "/")] // navegadores tratam \ como /
    [InlineData("https://evil.example", "/")]
    [InlineData("contas", "/")]
    public void SafeReturnUrl_ShouldOnlyAcceptLocalPaths(string? returnUrl, string expected) =>
        Assert.Equal(expected, ExternalLogins.SafeReturnUrl(returnUrl));
}
