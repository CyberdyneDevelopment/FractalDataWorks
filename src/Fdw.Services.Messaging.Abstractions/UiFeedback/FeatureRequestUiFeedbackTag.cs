using Fdw.Collections.Attributes;

namespace Fdw.Services.Messaging;

/// <summary>Something the screen should do and does not</summary>
[TypeOption(typeof(UiFeedbackTags), "FeatureRequest")]
public sealed class FeatureRequestUiFeedbackTag : UiFeedbackTagBase
{
    /// <summary>Initializes a new instance of the <see cref="FeatureRequestUiFeedbackTag"/> class.</summary>
    public FeatureRequestUiFeedbackTag() : base("FeatureRequest", "Feature Request", "Something the screen should do and does not", "info")
    {
    }
}
