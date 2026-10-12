namespace SyntheticLib;

/// <summary>A synthetic class with two named symbols for test fixture use.</summary>
public class SyntheticClass
{
    /// <summary>Gets the answer to everything.</summary>
    public int Answer() => 42;
}

/// <summary>A second synthetic class in the same namespace.</summary>
public class AnotherSyntheticClass
{
    /// <summary>Returns a greeting.</summary>
    public string Greet(string name) => $"Hello, {name}";
}
