using System.Reflection;
using MoonscarsUI;
using UnityEngine;

namespace ModdingAPI;

public static class UIUtils {
    private static MethodInfo _screenUiControllerShowMessageInfo =
        typeof(SystemMessageController).GetMethod("ShowMessage", BindingFlags.Instance | BindingFlags.NonPublic);

    public static bool ShowSystemMessage(string message, Sprite sprite = null) {
        if (ScreenUIController.Instance is not { } screenUIController) return false;

        _screenUiControllerShowMessageInfo.Invoke(
            screenUIController.SystemMessageController,
            new object[] { message, sprite }
        );

        return true;
    }
}