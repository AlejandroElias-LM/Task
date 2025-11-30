using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Advanced Shape", menuName = "ItemShape/New Advanced Shape")]
public class AdvancedInventoryShape: InventoryShapeBase
{

    public int maxValue = 3;   // Max type index

    [SerializeField]
    private List<int> mask = new List<int>();

    private int Size => Mathf.Max(1, width) * Mathf.Max(1, height);

    public void EnsureSize()
    {
        if (mask == null)
            mask = new List<int>();

        int target = Size;

        while (mask.Count < target) mask.Add(0);
        while (mask.Count > target) mask.RemoveAt(mask.Count - 1);
    }

    private int Index(int x, int y)
    {
        int w = Mathf.Max(1, width);
        return y * w + x;
    }

    public int Get(int x, int y)
    {
        EnsureSize();
        return mask[Index(x, y)];
    }

    public void Set(int x, int y, int val)
    {
        Debug.Log($"Incrementing ({x},{y}) to {val}");
        EnsureSize();
        mask[Index(x, y)] = Mathf.Clamp(val, 0, maxValue);
    }

    private void OnValidate()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        maxValue = Mathf.Max(1, maxValue);
        EnsureSize();
    }
}
