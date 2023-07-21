#pragma warning disable CS0626

using System;
using System.Collections;
using MonoMod;
using UnityEngine.SceneManagement;

namespace ModdingAPI.Patches;

// ReSharper disable InconsistentNaming
[MonoModPatch("global::MoonscarsUI.SplashScreen")]
public class patch_SplashScreen : MoonscarsUI.SplashScreen {
    [MonoModConstructor]
    public void ctor() {
        Logger.InitializeFileStream();

        try {
            ModLoader.Load();
        } catch (Exception e) {
            Logger.LogError(e.ToString());
        }
    }
}
