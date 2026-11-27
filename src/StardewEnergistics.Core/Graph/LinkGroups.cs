namespace StardewEnergistics.Core.Graph;

/// <summary>A network as the farm-wide linker sees it.</summary>
/// <param name="Id">The network's ID (unique across locations, since block IDs include the location).</param>
/// <param name="HasCore">True if the network has exactly one Computer Core.</param>
/// <param name="IsHome">True for the farmhouse network: it leads its group when present.</param>
public sealed record LinkNode(string Id, bool HasCore, bool IsHome = false);

/// <summary>Networks joined through Network Ports. <see cref="Primary"/> is null if no member has a Core.</summary>
public sealed record LinkGroup(IReadOnlyList<string> Members, string? Primary)
{
    public bool Working => Primary != null;
}

/// <summary>
/// Joins per-location networks into farm-wide groups. A Port links the building network it sits beside
/// (inside) with the outdoor network its wire is on; every network sharing an outdoor network ends up in
/// one group. A group works if any member has a Core: storage is shared across the whole group,
/// while power stays per building (a member with its own Core runs on its own power;
/// a member without a Core, like outdoor wire, uses the group's primary network's power).
/// </summary>
public sealed class LinkGroups
{
    private readonly Dictionary<string, LinkGroup> byMember = new();

    public LinkGroups(IEnumerable<LinkNode> nodes, IEnumerable<(string A, string B)> links)
    {
        var all = nodes.GroupBy(n => n.Id).ToDictionary(g => g.Key, g => g.First());
        var parent = all.Keys.ToDictionary(k => k, k => k);

        string Find(string x)
        {
            while (parent[x] != x)
                x = parent[x] = parent[parent[x]];
            return x;
        }

        foreach (var (a, b) in links)
        {
            if (!parent.ContainsKey(a) || !parent.ContainsKey(b))
                continue;
            string ra = Find(a), rb = Find(b);
            if (ra != rb)
                parent[ra] = rb;
        }

        foreach (var set in all.Keys.GroupBy(Find))
        {
            var members = set.OrderBy(s => s, StringComparer.Ordinal).ToList();
            string? primary = members.Select(m => all[m])
                .Where(n => n.HasCore)
                .OrderByDescending(n => n.IsHome)
                .ThenBy(n => n.Id, StringComparer.Ordinal)
                .Select(n => n.Id)
                .FirstOrDefault();
            var group = new LinkGroup(members, primary);
            foreach (var m in members)
                byMember[m] = group;
        }
    }

    public IEnumerable<LinkGroup> All => byMember.Values.Distinct();

    /// <summary>The group a network is in (a group of one if it has no links), or null if unknown.</summary>
    public LinkGroup? GroupOf(string networkId) => byMember.TryGetValue(networkId, out var g) ? g : null;

    /// <summary>The network whose power a member uses: itself if it has a Core, else the group's primary.</summary>
    public string? PowerSourceOf(string networkId, bool hasCore) => hasCore ? networkId : GroupOf(networkId)?.Primary;
}
