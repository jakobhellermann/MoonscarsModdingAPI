namespace ModdingAPI;

// ReSharper disable once InconsistentNaming
public abstract class Mod {
    public abstract void Load();
    public abstract void Unload(); // TODO unloading
    public abstract string GetName();
    public abstract string Version();
}