using StardewEnergistics.Core.Graph;
using Xunit;

namespace StardewEnergistics.Tests;

public class LinkGroupsTests
{
    [Fact]
    public void ThreeBuildingsJoinThroughTheFarm()
    {
        var nodes = new[]
        {
            new LinkNode("house", true, IsHome: true),
            new LinkNode("shed", true),
            new LinkNode("barn", true),
            new LinkNode("farm", false),
            new LinkNode("coop", true),
        };
        var links = new[] { ("house", "farm"), ("shed", "farm"), ("farm", "barn") };
        var g = new LinkGroups(nodes, links);

        var house = g.GroupOf("house")!;
        Assert.Same(house, g.GroupOf("shed"));
        Assert.Same(house, g.GroupOf("farm"));
        Assert.Equal(4, house.Members.Count);
        Assert.Equal("house", house.Primary);

        var coop = g.GroupOf("coop")!;
        Assert.Single(coop.Members);
        Assert.Equal("coop", coop.Primary);
        Assert.Equal(2, g.All.Count());
    }

    [Fact]
    public void OutdoorWireUsesThePrimarysPower()
    {
        var g = new LinkGroups(new[] { new LinkNode("house", true, true), new LinkNode("farm", false) }, new[] { ("farm", "house") });
        Assert.Equal("house", g.PowerSourceOf("farm", hasCore: false));
        Assert.Equal("house", g.PowerSourceOf("house", hasCore: true));
    }

    [Fact]
    public void GroupWithoutCoreDoesNotWork()
    {
        var g = new LinkGroups(new[] { new LinkNode("farm", false), new LinkNode("brokenShed", false) }, new[] { ("farm", "brokenShed") });
        Assert.False(g.GroupOf("farm")!.Working);
        Assert.Null(g.PowerSourceOf("farm", false));
    }

    [Fact]
    public void WithoutFarmhouseTheFirstCoreLeads()
    {
        var g = new LinkGroups(new[] { new LinkNode("shed", true), new LinkNode("barn", true), new LinkNode("farm", false) },
            new[] { ("shed", "farm"), ("barn", "farm") });
        Assert.Equal("barn", g.GroupOf("farm")!.Primary);
    }

    [Fact]
    public void UnknownLinksAreIgnored()
    {
        var g = new LinkGroups(new[] { new LinkNode("a", true) }, new[] { ("a", "ghost") });
        Assert.Single(g.GroupOf("a")!.Members);
        Assert.Null(g.GroupOf("ghost"));
    }
}
