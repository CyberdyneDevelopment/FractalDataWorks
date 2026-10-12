using System;
using Fdw.Collections;

namespace Fdw.Data.Transformations;

/// <summary>
/// Interface for named timezone type options used by field transforms.
/// </summary>
public interface ITimeZoneType : ITypeOption<int, ITimeZoneType>
{
    /// <summary>
    /// Gets the IANA/Windows timezone identifier for <see cref="System.TimeZoneInfo.FindSystemTimeZoneById"/>.
    /// </summary>
    string TimeZoneId { get; }

    /// <summary>
    /// Resolves the <see cref="TimeZoneInfo"/> for this timezone.
    /// </summary>
    /// <returns>The resolved timezone info.</returns>
    TimeZoneInfo Resolve();
}
