using System;
using StardewModdingAPI;

namespace StardewEnergistics.Integrations;

/// <summary>Adds the mod's options to Generic Mod Config Menu, if it's installed.</summary>
internal static class ConfigMenu
{
    public static void Register(IModHelper helper, IManifest manifest, ModConfig config, Action applied)
    {
        var api = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api == null)
            return;

        string T(string key) => helper.Translation.Get(key);

        api.Register(manifest,
            reset: () =>
            {
                Copy(new ModConfig(), config);
                applied();
            },
            save: () =>
            {
                helper.WriteConfig(config);
                applied();
            });

        api.AddSectionTitle(manifest, () => T("config.general"));
        api.AddKeybindList(manifest, () => config.NetworkViewKey, v => config.NetworkViewKey = v, () => T("config.networkViewKey"), () => T("config.networkViewKey.tip"));
        api.AddKeybindList(manifest, () => config.GuideKey, v => config.GuideKey = v, () => T("config.guideKey"), () => T("config.guideKey.tip"));
        api.AddNumberOption(manifest, () => config.WrenchHoldTicks, v => config.WrenchHoldTicks = v, () => T("config.wrenchHold"), () => T("config.wrenchHold.tip"), min: 10, max: 120, interval: 5);
        api.AddBoolOption(manifest, () => config.WireUnderBlockOnly, v => config.WireUnderBlockOnly = v, () => T("config.wireUnder"), () => T("config.wireUnder.tip"));
        api.AddBoolOption(manifest, () => config.TierGates, v => config.TierGates = v, () => T("config.tierGates"), () => T("config.tierGates.tip"));
        api.AddBoolOption(manifest, () => config.AnimateDataCables, v => config.AnimateDataCables = v, () => T("config.animateCables"), () => T("config.animateCables.tip"));
        api.AddBoolOption(manifest, () => config.TerminalReachAllBeforePorts, v => config.TerminalReachAllBeforePorts = v, () => T("config.reachAll"), () => T("config.reachAll.tip"));

        api.AddSectionTitle(manifest, () => T("config.testing"));
        api.AddBoolOption(manifest, () => config.TestMode, v => config.TestMode = v, () => T("config.testMode"), () => T("config.testMode.tip"));
        api.AddKeybindList(manifest, () => config.TestMenuKey, v => config.TestMenuKey = v, () => T("config.testMenuKey"), () => T("config.testMenuKey.tip"));

        api.AddSectionTitle(manifest, () => T("config.power"));
        api.AddNumberOption(manifest, () => config.BatteryPower, v => config.BatteryPower = v, () => T("config.batteryPower"), () => T("config.batteryPower.tip"), min: 10, max: 1000, interval: 10);
        api.AddNumberOption(manifest, () => config.CoalPower, v => config.CoalPower = v, () => T("config.coalPower"), null, min: 1, max: 50);
        api.AddNumberOption(manifest, () => config.HardwoodPower, v => config.HardwoodPower = v, () => T("config.hardwoodPower"), null, min: 1, max: 50);
    }

    private static void Copy(ModConfig from, ModConfig to)
    {
        to.NetworkViewKey = from.NetworkViewKey;
        to.GuideKey = from.GuideKey;
        to.WrenchHoldTicks = from.WrenchHoldTicks;
        to.AnimateDataCables = from.AnimateDataCables;
        to.TierGates = from.TierGates;
        to.WireUnderBlockOnly = from.WireUnderBlockOnly;
        to.BatteryPower = from.BatteryPower;
        to.CoalPower = from.CoalPower;
        to.HardwoodPower = from.HardwoodPower;
        to.ConfigVersion = ModConfig.CurrentVersion;
        to.TerminalReachAllBeforePorts = from.TerminalReachAllBeforePorts;
        to.TestMode = from.TestMode;
        to.TestMenuKey = from.TestMenuKey;
    }
}
