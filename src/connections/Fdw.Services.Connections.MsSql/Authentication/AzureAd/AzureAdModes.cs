using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.MsSql.Authentication.AzureAd;

/// <summary>
/// The Entra ID modes SqlClient takes, found by the word that names them in upper case (<c>ByWord</c>); a word that matches none gets
/// <c>NotFound</c> and the connection is refused with AzureAdModeUnknown.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(AzureAdModeBase), typeof(IAzureAdMode), typeof(AzureAdModes))]
public abstract partial class AzureAdModes : TypeCollectionBase<AzureAdModeBase, IAzureAdMode>
{
}
