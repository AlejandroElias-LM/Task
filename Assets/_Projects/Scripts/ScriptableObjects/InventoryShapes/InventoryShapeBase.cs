using UnityEngine;

public class InventoryShapeBase : ScriptableObject
{
    public int width, height;

    public int Get(int x, int y) { return 1; }
}
