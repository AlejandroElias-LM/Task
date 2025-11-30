using UnityEngine;

public class LoadPackage { };
public interface ISaveListener
{
    public void SaveState();
    public void LoadState(LoadPackage package);
}
