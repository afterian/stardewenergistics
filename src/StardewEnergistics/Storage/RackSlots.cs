using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace StardewEnergistics.Storage;

/// <summary>Reads and writes comma-separated lists kept in a rack's modData.</summary>
internal static class RackSlots
{
    public const int SlotsPerRack = 4;

    public static List<string> Get(SObject rack, string key) =>
        rack.modData.TryGetValue(key, out string? raw) && !string.IsNullOrEmpty(raw)
            ? raw.Split(',').Where(s => s.Length > 0).ToList()
            : new List<string>();

    public static void Set(SObject rack, string key, IEnumerable<string> values)
    {
        string raw = string.Join(",", values);
        if (raw.Length == 0)
            rack.modData.Remove(key);
        else
            rack.modData[key] = raw;
    }

    public static List<int> GetInts(SObject rack, string key) =>
        Get(rack, key).Select(s => int.TryParse(s, out int n) ? n : 0).ToList();

    public static void SetInts(SObject rack, string key, IEnumerable<int> values) => Set(rack, key, values.Select(v => v.ToString()));

    public static int GetInt(SObject obj, string key) =>
        obj.modData.TryGetValue(key, out string? raw) && int.TryParse(raw, out int n) ? n : 0;

    /// <summary>Shows a rack's fill level by picking its sprite frame (frame n = n items in slots).</summary>
    public static void ShowFrame(SObject obj, int frame) => obj.ParentSheetIndex = frame;
}
