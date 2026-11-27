using StardewEnergistics.Core.Wires;

namespace StardewEnergistics.Core.Graph;

/// <summary>Flood-fills a location's wire layer and blocks into networks.</summary>
public sealed class NetworkGraphBuilder
{
    public NetworkGraphBuilder(ConnectionRule rule = ConnectionRule.OwnTile) => Rule = rule;

    public ConnectionRule Rule { get; }

    public NetworkGraph Build(WireLayer wires, IReadOnlyList<BlockNode> blocks) => throw new NotImplementedException();
}
