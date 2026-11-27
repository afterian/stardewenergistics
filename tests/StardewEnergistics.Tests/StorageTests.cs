using System.Text.Json;
using StardewEnergistics.Core.Persistence;
using StardewEnergistics.Core.Storage;

namespace StardewEnergistics.Tests;

public class StorageTests
{
    private static DiskContents Disk(string id, DiskTier tier = DiskTier.Copper) => new(new DiskId(id), tier, DiskSpec.For(tier));

    private static ItemKey Item(int n, int quality = 0) => new($"(O){n}", quality);

    [Fact]
    public void CopperDiskHolds20Kinds()
    {
        var disk = Disk("d1");
        var index = new StorageIndex(new[] { disk });

        for (int i = 0; i < 20; i++)
            Assert.True(index.Insert(Item(i), 10).All);

        var full = index.Insert(Item(99), 1);
        Assert.Equal(0, full.Inserted);
        Assert.Equal(1, full.Remaining);
        Assert.Equal(20, index.KindsUsed(disk.Id));
    }

    [Fact]
    public void QualityAndFlavourShareOneKind()
    {
        var disk = Disk("d1");
        var index = new StorageIndex(new[] { disk });

        index.Insert(new ItemKey("(O)24", 0), 5);
        index.Insert(new ItemKey("(O)24", 1), 5);
        index.Insert(new ItemKey("(O)24", 2), 5);
        index.Insert(new ItemKey("(O)344", PreserveId: "(O)613"), 1); // Apple Jelly
        index.Insert(new ItemKey("(O)344", PreserveId: "(O)634"), 1); // Apricot Jelly

        Assert.Equal(2, disk.KindsUsed);
        Assert.Equal(5, index.All().Count()); // each still its own terminal row
        Assert.Equal(5, index.Count(new ItemKey("(O)24", 1)));
    }

    [Fact]
    public void ItemLimitStopsInserts()
    {
        var index = new StorageIndex(new[] { Disk("d1") });
        var r = index.Insert(Item(1), 2_500);
        Assert.Equal(2_000, r.Inserted);
        Assert.Equal(500, r.Remaining);
        Assert.Equal(0, index.RoomFor(Item(2)));
    }

    [Fact]
    public void InsertsFillDisksInRackOrderAndSpill()
    {
        var a = Disk("a");
        var b = Disk("b");
        var index = new StorageIndex(new[] { a, b });

        Assert.True(index.Insert(Item(1), 2_500).All);

        Assert.Equal(2_000, a.TotalItems);
        Assert.Equal(500, b.TotalItems);
        Assert.Equal(2_500, index.Count(Item(1)));
    }

    [Fact]
    public void InsertsPreferADiskThatAlreadyHasTheKind()
    {
        var a = Disk("a");
        var b = Disk("b");
        b.Add(Item(7), 1);
        var index = new StorageIndex(new[] { a, b });

        index.Insert(Item(7, quality: 2), 10);

        Assert.Equal(0, a.KindsUsed);
        Assert.Equal(11, b.TotalItems);
    }

    [Fact]
    public void ExtractTakesFromLastDisksFirstAndReportsMissing()
    {
        var a = Disk("a");
        var b = Disk("b");
        a.Add(Item(1), 5);
        b.Add(Item(1), 5);
        var index = new StorageIndex(new[] { a, b });

        var r = index.Extract(Item(1), 7);
        Assert.Equal(7, r.Extracted);
        Assert.Equal(0, b.TotalItems);
        Assert.Equal(3, a.TotalItems);

        var short_ = index.Extract(Item(1), 10);
        Assert.Equal(3, short_.Extracted);
        Assert.Equal(7, short_.Missing);
        Assert.Equal(0, a.KindsUsed); // emptying a kind frees it
    }

    [Fact]
    public void FreeingTheLastQualityFreesTheKind()
    {
        var disk = Disk("d");
        disk.Add(Item(1, 0), 1);
        disk.Add(Item(1, 2), 1);
        disk.Take(Item(1, 0), 1);
        Assert.Equal(1, disk.KindsUsed);
        disk.Take(Item(1, 2), 1);
        Assert.Equal(0, disk.KindsUsed);
    }

    [Fact]
    public void PrismaticDiskHasUnlimitedKinds()
    {
        var index = new StorageIndex(new[] { Disk("p", DiskTier.Prismatic) });
        for (int i = 0; i < 500; i++)
            Assert.True(index.Insert(Item(i), 1).All);
    }

    [Fact]
    public void UpgradingADiskKeepsItsContents()
    {
        var disk = Disk("d");
        for (int i = 0; i < 20; i++)
            disk.Add(Item(i), 100);

        disk.Upgrade(DiskTier.Iron, DiskSpec.For(DiskTier.Iron));

        Assert.Equal(2_000, disk.TotalItems);
        Assert.Equal(20, disk.KindsUsed);
        Assert.Equal(6_000, disk.RoomFor(Item(50)));
    }

    [Fact]
    public void DiskSurvivesAJsonRoundTrip()
    {
        var disk = Disk("d", DiskTier.Gold);
        disk.Add(new ItemKey("(O)24", 2), 33);
        disk.Add(new ItemKey("(O)344", PreserveId: "(O)613"), 4);
        disk.Add(new ItemKey("(O)597", Color: 0x7F3F1F), 2);

        var model = new SaveModel();
        model.Disks["d"] = DiskContentsMapper.ToDto(disk);
        var loaded = JsonSerializer.Deserialize<SaveModel>(JsonSerializer.Serialize(model))!;
        var restored = DiskContentsMapper.FromDto(new DiskId("d"), loaded.Disks["d"]);

        Assert.Equal(DiskTier.Gold, restored.Tier);
        Assert.Equal(disk.Stacks.OrderBy(s => s.Key.ToString()), restored.Stacks.OrderBy(s => s.Key.ToString()));
        Assert.Equal(3, restored.KindsUsed);
    }

    [Fact]
    public void LoadingIntoASmallerConfigKeepsEveryItem()
    {
        var dto = new DiskContentsDto { Tier = "Copper" };
        for (int i = 0; i < 30; i++)
            dto.Stacks.Add(new StoredStackDto { QualifiedId = $"(O){i}", Count = 100 });

        var disk = DiskContentsMapper.FromDto(new DiskId("d"), dto);

        Assert.Equal(30, disk.KindsUsed);
        Assert.Equal(3_000, disk.TotalItems);
        Assert.Equal(0, disk.RoomFor(new ItemKey("(O)99"))); // over the limit: nothing new goes in, nothing is lost
    }
}
