using System.Collections.Generic;
using Fdw.Configuration;

namespace Fdw.Operations.Configuration;

/// <summary>The contract every Threshold implementation carries (FDW-3936).</summary>
public interface IThresholdImplementationConfiguration : IImplementationConfiguration
{
    /// <summary>Gets or sets what the threshold is for.</summary>
    string? Description { get; set; }

    /// <summary>Gets or sets whether the threshold is judged. A disabled threshold is kept and not judged.</summary>
    bool IsEnabled { get; set; }

    /// <summary>Gets or sets whether a rule fires its consequence only when its criteria start to hold, and again only after they stopped holding; false fires on every report that matches. Stated for every threshold.</summary>
    bool FiresOnEntry { get; set; }

    /// <summary>Gets or sets the kind of subject the threshold judges; it must equal the SubjectKind of the threshold type the Implementation names.</summary>
    string SubjectKind { get; set; }

    /// <summary>Gets or sets the parameters of the threshold: how its type's measure is taken.</summary>
    IList<ThresholdParameterConfiguration> Parameters { get; set; }

    /// <summary>Gets or sets the rules, judged in the order of their Ordinal.</summary>
    IList<ThresholdRuleConfiguration> Rules { get; set; }
}
