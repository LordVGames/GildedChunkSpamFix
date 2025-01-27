using BepInEx;
using RoR2;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;

namespace GildedChunkSpamFix
{
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "LordVGames";
        public const string PluginName = "GildedChunkSpamFix";
        public const string PluginVersion = "1.0.1";

        public void Awake()
        {
            Log.Init(Logger);
            IL.RoR2.AffixAurelioniteBehavior.OnServerDamageDealt += ILHooks.AffixAurelioniteBehavior_OnServerDamageDealt;
        }
    }

    public static class ILHooks
    {
        public static void AffixAurelioniteBehavior_OnServerDamageDealt(ILContext il)
        {
            ILCursor c = new(il);


            if (!c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<AffixAurelioniteBehavior>("isPlayer"),
                x => x.MatchBrfalse(out _)
            ))
            {
                LogILError(il, c);
            }
            // create label here to jump past the isPlayer check later
            ILLabel doCooldownStuff = c.DefineLabel();
            c.MarkLabel(doCooldownStuff);


            // resetting cursor position & going before the isPlayer check using AfterLabel to not mess anything up (or at least it shouldn't)
            c.Index = 0;
            if (!c.TryGotoNext(MoveType.AfterLabel,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<AffixAurelioniteBehavior>("isPlayer"),
                x => x.MatchBrfalse(out _)
            ))
            {
                LogILError(il, c);
            }
            // make it always jump to the stuff for cooldowns for spawning gold, regardless of who is attacker/victim
            c.Emit(OpCodes.Br, doCooldownStuff);


#if DEBUG
            Log.Warning("SUCCESSFULLY IL HOOKED AffixAurelioniteBehavior OnServerDamageDealt");
            Log.Warning($"cursor is {c}");
            Log.Warning($"il is {il}");
#endif
        }

        private static void LogILError(ILContext il, ILCursor c)
        {
            Log.Error("COULD NOT IL HOOK AffixAurelioniteBehavior OnServerDamageDealt");
            Log.Error($"cursor is {c}");
            Log.Error($"il is {il}");
        }
    }
}