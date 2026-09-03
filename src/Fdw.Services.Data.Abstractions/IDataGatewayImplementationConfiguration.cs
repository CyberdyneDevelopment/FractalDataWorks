using System;
using Fdw.Configuration;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// What one data gateway implementation is configured with.
/// </summary>
/// <remarks>
/// The marker an implementation implements so the domain provider can hold every implementation's
/// configuration in one dictionary without naming any of them.
/// </remarks>
public interface IDataGatewayImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets the domain record this body belongs to.</summary>
    Guid DataGatewayId { get; set; }
}
