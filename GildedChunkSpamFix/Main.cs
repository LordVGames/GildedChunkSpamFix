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
        public const string PluginVersion = "1.0.0";

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
            // replace "this.isPlayer" check at 241 (or 112 in IL) with "!DamageReport.victimBody.isPlayerControlled"
            // that way both player and AI attackers will have a cooldown for spawning gilded chunks, not just players
            // gold chunk spam can still happen on player victims, but im 99.999% sure that'll never be a problem in normal gameplay
            if (c.TryGotoNext(MoveType.Before,
                    x => x.MatchLdloc(3),
                    x => x.MatchLdcR4(out _),
                    x => x.MatchBleUn(out _)
                ))
            {
                ILLabel addressJustStealMoney = c.DefineLabel();
                c.MarkLabel(addressJustStealMoney);

                // resetting the cursor position because the label was further than where we need to emit code
                c.Index = 0;
                c.GotoNext(MoveType.AfterLabel,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<AffixAurelioniteBehavior>("isPlayer"),
                    x => x.MatchBrfalse(out _)
                );
                c.RemoveRange(3);
                c.Emit(OpCodes.Ldarg_1);
                c.Emit<DamageReport>(OpCodes.Ldfld, "victimBody");
                c.Emit<CharacterBody>(OpCodes.Callvirt, "get_isPlayerControlled");
                c.Emit(OpCodes.Brtrue_S, addressJustStealMoney);

#if DEBUG
                Log.Warning("SUCCESSFULLY IL HOOKED AffixAurelioniteBehavior OnServerDamageDealt");
                Log.Warning($"cursor is {c}");
                Log.Warning($"il is {il}");
#endif
            }
            else
            {
                Log.Error("COULD NOT IL HOOK AffixAurelioniteBehavior OnServerDamageDealt");
                Log.Error($"cursor is {c}");
                Log.Error($"il is {il}");
            }
        }
    }
}