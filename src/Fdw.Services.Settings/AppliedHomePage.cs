using System;

namespace Fdw.Services.Settings;

/// <summary>
/// The home page that applies to a person and the scope it comes from (FDW-2325).
/// </summary>
/// <param name="SourceKind">The scope the page comes from: one of <see cref="HomePageKinds"/>; <see cref="HomePageKinds.None"/> when no scope holds a published page.</param>
/// <param name="SourceId">The id of that scope; null for none.</param>
/// <param name="Body">The published page as JSON; null for none.</param>
public sealed record AppliedHomePage(string SourceKind, Guid? SourceId, string? Body);
