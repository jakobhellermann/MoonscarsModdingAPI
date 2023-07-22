using System.IO;
using UnityEngine.Device;

namespace ModdingAPI;

public static class Logger {
    private static readonly string Path = System.IO.Path.Combine(Application.persistentDataPath, "ModLog.txt");
    private static readonly object Locker = new();

    private static FileStream _fileStream;
    private static StreamWriter _writer;

    internal static void InitializeFileStream() {
        if (_fileStream != null) return;

        _fileStream = new FileStream(Path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        lock (Locker) {
            _writer = new StreamWriter(_fileStream);
        }
    }

    public static void Log(string message) {
        WriteToFile(message);
    }

    public static void LogDebug(string message) {
    }

    public static void LogError(string message) {
        WriteToFile(message);
    }

    private static void WriteToFile(string text) {
        lock (Locker) {
            _writer?.WriteLine(text);
            _writer?.Flush();
        }
    }
}