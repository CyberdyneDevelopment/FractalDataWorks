using System.Collections.Immutable;

namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Model for an abstract method or property that needs a stub implementation in the Empty sentinel.
/// </summary>
internal readonly record struct AbstractMemberModel(
    string Name,
    string ReturnType,
    bool IsProperty,
    bool IsMethod,
    bool HasGetter,
    bool HasSetter,
    ImmutableArray<ParameterModel> Parameters,
    string? MatchingParameterName, // Parameter whose type matches return type (for identity transforms)
    bool IsOverride, // True if from abstract base class (needs override), false if from interface (just implement)
    string? ExplicitInterfaceType, // If set, generate as explicit interface implementation (e.g., "IFoo")
    ImmutableArray<string> TypeParameters // Method-level generic type parameters (e.g., "TLeft", "TRight", "TResult")
);