using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [Header("Inventory Shape")]
    public AdvancedInventoryShape shape;
    public GameObject[] fillObjects;
    public GridLayoutGroup layoutParent;

    [Header("Link visuals")]
    [Tooltip("Sprites for link counts 0..4 (0 = empty / isolated, 1..4 = number of connected neighbors)")]
    public Sprite[] linkSprites;
    [Tooltip("Optional: assign explicitly, otherwise filled automatically by FillChillds()")]
    public Image[] cellImages;

    [HideInInspector] public bool[] freeCells;

    private void ResetCellsArray()
    {
        if (shape == null)
        {
            freeCells = new bool[0];
            cellImages = new Image[0];
            return;
        }

        int total = shape.width * shape.height;
        if (freeCells == null || freeCells.Length != total)
            freeCells = new bool[total];

        // keep cellImages consistent length if already present
        if (cellImages == null || cellImages.Length != total)
            cellImages = new Image[total];
    }

    [Button]
    public void FillChillds()
    {
        var parent = layoutParent.transform;
        for (int i = parent.childCount - 1; i >= 0; --i)
        {
            var child = parent.GetChild(i);
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            else
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        ResetCellsArray();

        for (int y = 0; y < shape.height; y++)
        {
            for (int x = 0; x < shape.width; x++)
            {
                int index = y * shape.width + x;
                var point = shape.Get(x, y);

                freeCells[index] = point == 0; // if any kind of free cell

                point = Mathf.Clamp(point, 0, fillObjects.Length - 1);
                var prefab = fillObjects[point];
                var inst = Instantiate(prefab, layoutParent.transform);
                inst.name = $"cell ({x},{y})";
            }

        }

        // After instantiating all children, populate cellImages[] automatically.
        RefreshCellImagesArray();
        // After refresh, ensure visuals reflect occupancy:
        // If you want to set initial sprites, update entire grid:
        UpdateAllOrDeltaAfterFullRefresh();
    }

    /// <summary>
    /// Populate cellImages[] by reading children under layoutParent.
    /// Tries to find Image on the child root; if not found, searches in children.
    /// </summary>
    private void RefreshCellImagesArray()
    {
        if (layoutParent == null || shape == null) return;

        int total = shape.width * shape.height;
        cellImages = new Image[total];

        var parent = layoutParent.transform;
        int childCount = parent.childCount;

        for (int i = 0; i < total && i < childCount; i++)
        {
            var child = parent.GetChild(i);
            Image img = child.GetComponent<Image>();
            if (img == null)
            {
                img = child.GetComponentInChildren<Image>();
            }
            cellImages[i] = img;
        }
    }

    /// <summary>
    /// Helper used after FillChillds to ensure sprites match current freeCells.
    /// Uses delta approach but since we just refreshed all children it's simplest to update all.
    /// </summary>
    private void UpdateAllOrDeltaAfterFullRefresh()
    {
        // If you prefer full-scan for clarity:
        var all = new List<int>(freeCells.Length);
        for (int i = 0; i < freeCells.Length; i++) all.Add(i);
        UpdateLinkSpritesForIndices(all);
    }

    public bool GetCellUnderPointer(Vector2 screenPos, Camera cam, out Vector2 pos)
    {
        pos = new Vector2(-1, -1);

        if (layoutParent == null || layoutParent.transform.childCount == 0) return false;

        var parent = layoutParent.transform;
        int childCount = parent.childCount;

        for (int i = 0; i < childCount; i++)
        {
            var childRT = parent.GetChild(i) as RectTransform;
            if (childRT == null) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(childRT, screenPos, cam))
            {
                int width = shape.width;
                if (width <= 0) width = 1;

                int index = i;
                int x = index % width;
                int y = index / width;
                pos.x = x;
                pos.y = y;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// True if the inventory cell (x,y) is inside bounds.
    /// </summary>
    public bool IsInsideInventory(int x, int y)
    {
        return x >= 0 && y >= 0 && x < shape.width && y < shape.height;
    }

    /// <summary>
    /// Returns true if the inventory cell at x,y is free (not occupied and not disabled).
    /// </summary>
    public bool IsCellFree(int x, int y)
    {
        if (!IsInsideInventory(x, y)) return false;
        int idx = y * shape.width + x;
        return freeCells != null && idx >= 0 && idx < freeCells.Length && freeCells[idx];
    }

    public bool CanPlaceShapeAt(InventoryShapeBase itemShape, Vector2 itemOrigin, Vector2 inventoryPos)
    {
        if (itemShape == null) return false;

        for (int y = 0; y < itemShape.height; y++)
        {
            for (int x = 0; x < itemShape.width; x++)
            {
                var cellVal = itemShape is AdvancedInventoryShape adv ? adv.Get(x, y) : (itemShape as InventoryShape).Get(x, y);
                // only check occupied cells of the item
                if (cellVal == 0) continue;

                int targetX = ((int)inventoryPos.x) - ((int)itemOrigin.x) + x;
                int targetY = ((int)inventoryPos.y) - ((int)itemOrigin.y) + y;

                // out of bounds -> can't place
                if (!IsInsideInventory(targetX, targetY)) return false;

                if (!IsCellFree(targetX, targetY)) return false;
            }

        }

        return true;
    }

    /// <summary>
    /// Marks the inventory cells as occupied (sets freeCells=false) for the placement.
    /// Returns the list of linear indices that were occupied (useful to update visuals).
    /// Caller should only call this when CanPlaceShapeAt() returned true.
    /// This version updates only affected cells (delta).
    /// </summary>
    public List<int> PlaceShapeAt(InventoryShapeBase itemShape, Vector2 itemOrigin, Vector2 inventoryPos)
    {
        var occupiedIndices = new List<int>();

        if (itemShape == null) return occupiedIndices;

        for (int y = 0; y < itemShape.height; y++)
        {
            for (int x = 0; x < itemShape.width; x++)
            {
                var cellVal = itemShape is AdvancedInventoryShape adv ? adv.Get(x, y) : (itemShape as InventoryShape).Get(x, y);
                if (cellVal == 0) continue;

                int targetX = ((int)inventoryPos.x) - ((int)itemOrigin.x) + x;
                int targetY = ((int)inventoryPos.y) - ((int)itemOrigin.y) + y;

                if (!IsInsideInventory(targetX, targetY))
                {
                    // shouldn't happen if caller checked CanPlaceShapeAt
                    continue;
                }

                int idx = targetY * shape.width + targetX;
                if (idx >= 0 && idx < freeCells.Length)
                {
                    freeCells[idx] = false; // mark occupied
                    occupiedIndices.Add(idx);
                }
            }
        }

        // Delta update only the affected cells and neighbors
        var affected = GatherAffectedIndices(occupiedIndices);
        UpdateLinkSpritesForIndices(affected);

        return occupiedIndices;
    }

    /// <summary>
    /// Optional helper to free previously occupied indices (e.g. when removing an item).
    /// Now also updates visuals for affected indices via delta update.
    /// </summary>
    public void FreeIndices(IEnumerable<int> indices)
    {
        var actuallyChanged = new List<int>();

        foreach (var idx in indices)
        {
            if (idx >= 0 && idx < freeCells.Length)
            {
                if (!freeCells[idx])
                {
                    freeCells[idx] = true;
                    actuallyChanged.Add(idx);
                }
            }
        }

        if (actuallyChanged.Count == 0) return;

        var affected = GatherAffectedIndices(actuallyChanged);
        UpdateLinkSpritesForIndices(affected);
    }

    /// <summary>
    /// Compute which grid cell is under the given screen position using layout math (padding/spacing/cellSize).
    /// Returns true and outputs (x,y) in pos when inside the inventory bounds.
    /// This is independent of child GameObjects and works with all Canvas render modes (pass the correct cam).
    /// </summary>
    public bool GetCellUnderPointerByLayout(Vector2 screenPos, Camera cam, out Vector2 pos)
    {
        pos = new Vector2(-1, -1);

        if (layoutParent == null) return false;

        RectTransform layoutRect = layoutParent.GetComponent<RectTransform>();
        if (layoutRect == null) return false;

        // convert screen point to local point in layoutRect space (origin at pivot)
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(layoutRect, screenPos, cam, out Vector2 localPoint))
            return false;

        // sizes
        float rectW = layoutRect.rect.width;
        float rectH = layoutRect.rect.height;

        // GridLayoutGroup settings
        Vector2 cellSize = layoutParent.cellSize;
        Vector2 spacing = layoutParent.spacing;
        RectOffset padding = layoutParent.padding;

        // compute left and top edges positions (local coordinates origin = pivot)
        // leftEdgeLocal = -rectW * pivot.x
        float leftEdgeLocal = -rectW * layoutRect.pivot.x;
        // topEdgeLocal = rectH * (1 - pivot.y)
        float topEdgeLocal = rectH * (1f - layoutRect.pivot.y);

        // compute point offset from left edge (distance in x) and from top edge (distance in y)
        float xFromLeft = localPoint.x - leftEdgeLocal - padding.left;
        float yFromTop = topEdgeLocal - localPoint.y - padding.top;

        // early out if outside padded content rect
        float contentWidth = rectW - padding.left - padding.right;
        float contentHeight = rectH - padding.top - padding.bottom;
        if (xFromLeft < 0f || yFromTop < 0f || xFromLeft > contentWidth || yFromTop > contentHeight)
            return false;

        // compute index using cell size + spacing
        float cellFullW = cellSize.x + spacing.x;
        float cellFullH = cellSize.y + spacing.y;

        int col = Mathf.FloorToInt(xFromLeft / cellFullW);
        int row = Mathf.FloorToInt(yFromTop / cellFullH);

        // clamp and ensure inside logical width/height from shape
        if (col < 0 || row < 0 || shape == null) return false;
        if (col >= shape.width || row >= shape.height) return false;

        pos.x = col;
        pos.y = row;
        return true;
    }

    public RectTransform GetChild(Vector2 pos)
    {
        int i = (int)(pos.y * shape.width) + ((int)pos.x);
        return layoutParent.transform.GetChild(i) as RectTransform;
    }

    // ----------------- Delta update helpers -----------------

    private bool IsIndexValid(int idx)
    {
        return freeCells != null && idx >= 0 && idx < freeCells.Length;
    }

    private int XYToIndex(int x, int y)
    {
        return y * shape.width + x;
    }

    private void IndexToXY(int idx, out int x, out int y)
    {
        x = idx % shape.width;
        y = idx / shape.width;
    }

    /// <summary>
    /// Returns true if the cell at x,y is occupied (i.e., not free).
    /// Out-of-bounds is considered not occupied.
    /// </summary>
    private bool IsOccupiedXY(int x, int y)
    {
        if (!IsInsideInventory(x, y)) return false;
        int idx = XYToIndex(x, y);
        if (!IsIndexValid(idx)) return false;
        return !freeCells[idx];
    }

    /// <summary>
    /// Given a collection of changed indices (cells that toggled occupied/free), returns
    /// the set of indices that must be recalculated: each changed index + its 4 neighbors.
    /// </summary>
    private HashSet<int> GatherAffectedIndices(IEnumerable<int> changedIndices)
    {
        var affected = new HashSet<int>();
        foreach (var idx in changedIndices)
        {
            if (!IsIndexValid(idx)) continue;

            // add self
            affected.Add(idx);

            IndexToXY(idx, out int x, out int y);

            // up
            if (IsInsideInventory(x, y - 1)) affected.Add(XYToIndex(x, y - 1));
            // down
            if (IsInsideInventory(x, y + 1)) affected.Add(XYToIndex(x, y + 1));
            // left
            if (IsInsideInventory(x - 1, y)) affected.Add(XYToIndex(x - 1, y));
            // right
            if (IsInsideInventory(x + 1, y)) affected.Add(XYToIndex(x + 1, y));
        }
        return affected;
    }

    /// <summary>
    /// Update link sprites for the given set of indices (only those indices will be recalculated).
    /// Indices outside the inventory will be ignored.
    /// </summary>
    public void UpdateLinkSpritesForIndices(IEnumerable<int> indices)
    {
        if (linkSprites == null || linkSprites.Length < 5)
        {
            Debug.LogWarning("linkSprites not set or too small. Need sprites for indices 0..4.");
            return;
        }
        if (cellImages == null || cellImages.Length != shape.width * shape.height)
        {
            Debug.LogWarning("cellImages null or wrong length. Expected length width*height.");
            return;
        }
        if (freeCells == null || freeCells.Length != shape.width * shape.height)
        {
            Debug.LogWarning("freeCells null or wrong length. Expected length width*height.");
            return;
        }

        foreach (var rawIdx in indices)
        {
            if (!IsIndexValid(rawIdx)) continue;

            if (cellImages[rawIdx] == null)
            {
                // nothing to update visually for this index, just skip
                continue;
            }

            // If the cell is free, assign linkSprites[0] (or set to null if you prefer empty look)
            if (freeCells[rawIdx])
            {
                cellImages[rawIdx].sprite = linkSprites[0];
                continue;
            }

            IndexToXY(rawIdx, out int x, out int y);

            int links = 0;
            if (IsOccupiedXY(x, y - 1)) links++; // up
            if (IsOccupiedXY(x, y + 1)) links++; // down
            if (IsOccupiedXY(x - 1, y)) links++; // left
            if (IsOccupiedXY(x + 1, y)) links++; // right

            links = Mathf.Clamp(links, 0, 4);
            cellImages[rawIdx].sprite = linkSprites[links];
        }
    }

    public void SaveState(ref InventorySaveState save)
    {
        save.freecells = this.freeCells;
    }

    public void Load(InventorySaveState save)
    {
        this.freeCells = save.freecells;
    }

}

[System.Serializable]
public struct InventorySaveState
{
    public bool[] freecells;
}