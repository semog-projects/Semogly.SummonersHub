namespace Semogly.SummonersHub.Application.Accounts;

/// <summary>Quantidade de itens por chave (estrelas, elemento, slot, raridade...).</summary>
public sealed record CountBy<TKey>(TKey Key, int Count);
