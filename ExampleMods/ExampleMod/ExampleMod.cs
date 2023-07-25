using System.Reflection;
using JetBrains.Annotations;
using ModdingAPI;
using UnityEngine;
using UnityEngine.InputSystem;
using Logger = ModdingAPI.Logger;

namespace ExampleMod;

[UsedImplicitly]
public class ExampleMod : Mod {
    public override string GetName() => Assembly.GetExecutingAssembly().GetName().Name;

    public override string Version() => Assembly.GetExecutingAssembly().GetName().Version.ToString();

    private InputActionMap _actionMap = null!;

    private static void DebugAllEntities() {
        static void PrintGameObjectComponents(GameObject gameObject, int index = 0) {
            var enabled = gameObject.activeInHierarchy;

            if (!enabled) return;

            var space = new string(' ', index * 4);
            Logger.Log($"{space}{gameObject.name}");

            var components = gameObject.GetComponents<Component>();
            foreach (var component in components) Logger.Log($"{space}- {component.GetType().Name}");


            for (var i = 0; i < gameObject.transform.childCount; i++) {
                var child = gameObject.transform.GetChild(i);
                PrintGameObjectComponents(child.gameObject, index + 1);
            }
        }

        var objects = Object.FindObjectsOfType<GameObject>();
        foreach (var gameObject in objects)
            if (gameObject.transform.parent == null)
                PrintGameObjectComponents(gameObject);
    }


    public override void Load() {
        var obj = new GameObject();
        obj.AddComponent<ModdedComponent>();
        Object.DontDestroyOnLoad(obj);

        var map = new InputActionMap("ModActions");
        var testAction = map.AddAction("TestAction", InputActionType.Button, "<Keyboard>/t");

        testAction.performed += _ => {
            UIUtils.ShowSystemMessage("Test");
            DebugAllEntities();
        };

        map.Enable();
        _actionMap = map;

        On.PlayerPawn.Dash += HookedDash;
    }

    public override void Unload() {
        _actionMap.Dispose();
        On.PlayerPawn.Dash -= HookedDash;
    }

    private static void HookedDash(On.PlayerPawn.orig_Dash orig, PlayerPawn player) {
        orig(player);
        var field = typeof(PlayerPawn).GetField("_dashCooldown", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(player, 0.0f);
    }
}