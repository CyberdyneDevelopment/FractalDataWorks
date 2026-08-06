using System;
using Fdw.Configuration;

namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Marker interface for a calculation entity's polymorphic typed body (Formula / Windowed).
/// </summary>
/// <remarks>
/// Why: the PocoMapperGenerator detects a header's typed body by a property named <c>Configuration</c>
/// whose type implements <see cref="IGenericConfiguration"/> through its interface set (mirroring
/// Connection's <c>IConnectionConfiguration</c>). A property typed as <see cref="IGenericConfiguration"/>
/// directly is NOT matched, so the calc header's Configuration property is typed as this derived marker.
/// It also carries the parent FK so the header provider can stamp it on save without reflection.
/// </remarks>
public interface ICalculationTypedConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the parent CalculationEntity's logical Id (FK to calc.CalculationEntity.Id).</summary>
    Guid CalculationEntityId { get; set; }
}
