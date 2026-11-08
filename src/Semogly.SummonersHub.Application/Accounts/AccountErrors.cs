using Semogly.SummonersHub.Domain.Abstractions;

namespace Semogly.SummonersHub.Application.Accounts;

public static class AccountErrors
{
    public static readonly Error InvalidWizard = Error.Validation(
        "Accounts.InvalidWizard",
        "O import não tem um invocador válido.");

    public static readonly Error TooLarge = Error.Validation(
        "Accounts.TooLarge",
        "O import tem mais itens do que uma conta do jogo comporta.");

    public static Error UnsupportedSchemaVersion(int version) => Error.Validation(
        "Accounts.UnsupportedSchemaVersion",
        $"Versão de import {version} não suportada. Atualize a página e envie o arquivo de novo.");

    public static Error UnknownGameCodes(IEnumerable<string> codes) => Error.Validation(
        "Accounts.UnknownGameCodes",
        $"O export contém códigos do jogo ainda não suportados: {string.Join(", ", codes)}.");

    public static Error NotFound(long wizardId) => Error.NotFound(
        "Accounts.NotFound",
        $"Nenhum import encontrado para o invocador {wizardId}.");
}
