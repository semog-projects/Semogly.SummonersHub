using Semogly.SummonersHub.Domain.Accounts;

namespace Semogly.SummonersHub.Api.Endpoints;

/// <summary>Corpo de PATCH /api/accounts/{wizardId}/inventory-marks.</summary>
internal sealed record UpdateInventoryMarksRequest(
    InventoryItemKind Kind,
    IReadOnlyList<long> ItemIds,
    bool? Locked,
    IReadOnlyList<string>? AddTags,
    IReadOnlyList<string>? RemoveTags);
