using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shape", menuName = "ItemShape/New Shape")]
public class InventoryShape : InventoryShapeBase
{

    // Unity can serialize List<bool> but not bool[,].
    [SerializeField] private List<bool> mask = new List<bool>();

    private int Size => Mathf.Max(1, width) * Mathf.Max(1, height);

    // Ensure the list has the correct number of entries
    public void EnsureSize()
    {
        if (mask == null) mask = new List<bool>();
        int target = Size;
        while (mask.Count < target) mask.Add(false);
        while (mask.Count > target) mask.RemoveAt(mask.Count - 1);
    }

    // Safe index helpers
    private int Index(int x, int y)
    {
        int w = Mathf.Max(1, width);
        x = Mathf.Clamp(x, 0, w - 1);
        y = Mathf.Clamp(y, 0, Mathf.Max(1, height) - 1);
        return y * w + x;
    }

    public int Get(int x, int y)
    {
        if (mask == null) return 0;
        EnsureSize();
        return mask[Index(x, y)]? 1 : 0;
    }

    public void Set(int x, int y, bool val)
    {
        EnsureSize();
        mask[Index(x, y)] = val;
    }

    // Bulk operations used by editor
    public void Clear()
    {
        EnsureSize();
        for (int i = 0; i < mask.Count; i++) mask[i] = false;
    }

    public void Fill()
    {
        EnsureSize();
        for (int i = 0; i < mask.Count; i++) mask[i] = true;
    }

    public void Invert()
    {
        EnsureSize();
        for (int i = 0; i < mask.Count; i++) mask[i] = !mask[i];
    }

    private void OnValidate()
    {
        // keep mask size consistent when values change in inspector
        EnsureSize();
    }
}
