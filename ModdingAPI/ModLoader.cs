using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Application = UnityEngine.Device.Application;
using Object = UnityEngine.Object;

namespace ModdingAPI;

public static class ModLoader {
    private enum ModLoadState {
        None,
        Loaded
    }

    private static ModLoadState _loadState = ModLoadState.None;


    private static Dictionary<Type, ModInstance> ModInstances { get; } = new();

    private enum ModErrorState {
        Construct,
        Duplicate,
        Load,
        Unload
    }

    private class ModInstance {
        public readonly Mod Mod;
        public readonly string Name;
        public bool Enabled;
        public ModErrorState? ErrorState;

        public ModInstance(Mod mod, string name, bool enabled, ModErrorState? errorState) {
            Mod = mod;
            Name = name;
            Enabled = enabled;
            ErrorState = errorState;
        }
    }


    private static void AddModInstance(Type ty, ModInstance instance) {
        Logger.Log($"Loading Mod {instance.Name}");
        ModInstances.Add(ty, instance);
    }

    public static void Load() {
        if (_loadState == ModLoadState.Loaded) {
            throw new Exception("attempted to load twice");
        }

        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        Logger.LogDebug("Loading mods");

        var managedPath = SystemInfo.operatingSystemFamily switch {
            OperatingSystemFamily.Linux or OperatingSystemFamily.Windows => Path.Combine(Application.dataPath,
                "Managed"),
            OperatingSystemFamily.MacOSX => Path.Combine(Application.dataPath, "Resources", "Data", "Managed"),
            _ => null
        };
        if (managedPath is null) {
            Logger.LogError("Unknown OS, cannot load");
            return;
        }

        var modsPath = Path.Combine(managedPath, "Mods");

        var dlls = Directory.GetDirectories(modsPath).Except(new[] { Path.Combine(modsPath, "Disabled") })
            .SelectMany(d => Directory.GetFiles(d, "*.dll"))
            .ToArray();
        Logger.Log(string.Join(",", dlls));

        Assembly Resolve(object sender, ResolveEventArgs args) {
            var name = new AssemblyName(args.Name);

            if (dlls.FirstOrDefault(x => x.EndsWith($"{name.Name}.dll")) is { } path) {
                return Assembly.LoadFile(path);
            }

            return null;
        }

        AppDomain.CurrentDomain.AssemblyResolve += Resolve;


        List<Assembly> asms = new(dlls.Length);
        foreach (var path in dlls) {
            try {
                asms.Add(Assembly.LoadFrom(path));
            } catch (Exception e) {
                Logger.LogError($"Unable to load assembly: {e.ToString()}");
            }
        }

        foreach (var asm in asms) {
            var name = asm.GetName();
            Logger.Log($"Loading mods in assembly {name.Name} ({name.Version})");

            var foundMod = false;
            foreach (var ty in asm.GetTypes()) {
                if (!ty.IsClass || ty.IsAbstract || !ty.IsSubclassOf(typeof(Mod))) {
                    continue;
                }

                foundMod = true;

                try {
                    if (ty.GetConstructor(Type.EmptyTypes)?.Invoke(Array.Empty<object>()) is Mod mod) {
                        AddModInstance(ty, new ModInstance(mod, mod.GetName(), false, null));
                    }
                } catch (Exception e) {
                    Logger.LogError($"Failed to load mod {asm.FullName.ToString()}: {e.ToString()}");
                    AddModInstance(ty, new ModInstance(null, ty.Name, false, ModErrorState.Construct));
                }
            }

            if (!foundMod) {
                var info = asm.GetName();
                Logger.LogError($"Assembly {info.Name.ToString()} loaded with 0 mods");
            }
        }

        foreach (var mod in ModInstances.Values) {
            if (mod.ErrorState is not null) {
                continue;
            }

            LoadMod(mod);
            mod.Enabled = true;
        }

        Logger.LogDebug("Finished loading mods");

        var version = new GameObject();
        var draw = version.AddComponent<ModVersionUI>();
        Object.DontDestroyOnLoad(version);
        draw.drawString = ModVersionText();

        _loadState = ModLoadState.Loaded;
    }

    private static string ModVersionText() {
        var builder = new StringBuilder();

        foreach (var instance in ModInstances.Values) {
            var status = instance.ErrorState switch {
                ModErrorState.Construct => "Failed to call constructor! Check ModLog.txt",
                ModErrorState.Load => "Failed to load! Check ModLog.txt",
                ModErrorState.Duplicate => "Failed to Load! Duplicate Mod detected",
                ModErrorState.Unload => "Failed to unload! Check ModLog.txt",
                null => $"{instance.Mod?.Version() ?? "Failed to get version"}",
                _ => throw new ArgumentOutOfRangeException()
            };

            builder.AppendLine($"{instance.Name}: {status}");
        }

        return builder.ToString();
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) {
        var exception = e.ExceptionObject;

        string msg;
        if (exception is Exception ex) {
            msg = ex.ToString();
        } else {
            msg = e.ToString();
        }

        Logger.LogError($"Unhandled exception: {msg}");
    }

    private static void LoadMod(ModInstance mod) {
        try {
            if (mod is { Enabled: false, ErrorState: null }) {
                mod.Enabled = true;
                mod.Mod?.Load();
            }
        } catch (Exception e) {
            mod.ErrorState = ModErrorState.Load;
            Logger.LogError($"Failed to initialize mod {mod.Name}: {e.ToString()}");
        }
    }
}