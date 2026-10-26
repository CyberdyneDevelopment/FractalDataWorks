using System;

namespace Fdw.Services.Workspaces;

/// <summary>A version of a definition at a place the caller may read (FDW-2560).</summary>
/// <typeparam name="TPayload">What the version says: the words, formula or other content of the definition at that place.</typeparam>
/// <param name="PlaceKind">The kind of place: Tenant, Organization or Workspace.</param>
/// <param name="PlaceId">The id of the place.</param>
/// <param name="Depth">How far above the place the caller acts in it is: 0 is the caller's own place.</param>
/// <param name="Payload">What the version says.</param>
/// <param name="Label">What to show for it: the definition's name, followed for an organization or a workspace by the place's name in parentheses.</param>
/// <param name="IsBase">True for the definition itself, which is the tenant's version.</param>
/// <param name="Applies">True for the nearest version to the caller, which is the one that applies.</param>
public sealed record PlaceVersion<TPayload>(string PlaceKind, Guid PlaceId, int Depth, TPayload Payload, string Label, bool IsBase, bool Applies);
