namespace StardewEnergistics.Core.Storage;

/// <summary>How many items were stored, and how many were left over (no room).</summary>
public sealed record InsertResult(int Inserted, int Remaining)
{
    public bool All => Remaining == 0;
}

/// <summary>How many items were taken out, and how many were asked for but missing.</summary>
public sealed record ExtractResult(int Extracted, int Missing)
{
    public bool All => Missing == 0;
}
