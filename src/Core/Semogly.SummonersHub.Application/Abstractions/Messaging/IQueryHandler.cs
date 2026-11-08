using Semogly.SummonersHub.Domain.Abstractions;

namespace Semogly.SummonersHub.Application.Abstractions.Messaging;

/// <summary>Marcador de uma consulta (operação somente leitura).</summary>
public interface IQuery<TResponse>;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
