using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using ModdingAPI;
using MonoMod.RuntimeDetour;

namespace ExampleMod;

[UsedImplicitly]
public class ExampleMod : Mod {
    public override string GetName() {
        return Assembly.GetExecutingAssembly().GetName().Name;
    }

    private Hook _playerDashHook;

    public override void Load() {
        _playerDashHook = new Hook(
            typeof(PlayerPawn).GetMethod("Dash", BindingFlags.Instance | BindingFlags.Public),
            typeof(ExampleMod).GetMethod("HookDash", BindingFlags.Static | BindingFlags.Public)
        );
    }

    public override void Unload() {
        _playerDashHook.Dispose();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void HookDash(Action<PlayerPawn> orig, PlayerPawn player) {
        orig(player);

        var field = typeof(PlayerPawn).GetField("_dashCooldown", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(player, 0.0f);
    }


    private void OnDash(On.PlayerPawn.orig_Dash origDash, PlayerPawn player) {
        try {
            Logger.Log("try dash");
            origDash(player);

            Logger.Log("dashed");

            var field = typeof(PlayerPawn).GetField("_dashCooldown", BindingFlags.NonPublic | BindingFlags.Instance);
            Logger.LogError($"field is null: {field == null}");
            field?.SetValue(player, 0.0f);

            var now = (float)(field?.GetValue(player) ?? 100.0);
            Logger.LogError($"field is now: {now}");

            Logger.Log("finished dash");
        } catch (Exception e) {
            Logger.LogError($"Error in OnDash: {e.ToString()}");
        }
    }
}