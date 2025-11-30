using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Small helper struct used to represent an item occupying slots in an inventory.
/// Kept here for convenience but you can move it to its own file if you prefer.
/// </summary>
[System.Serializable]
public struct FilledPosition
{
    public Inventory currentInventory;
    public List<int> occupiedSlots;
    public bool locked;

    public FilledPosition(Inventory currentInventory, List<int> occupiedSlots, bool locked)
    {
        this.locked = locked;
        this.currentInventory = currentInventory;
        this.occupiedSlots = occupiedSlots;
    }
}

/// <summary>
/// InventoryItem is responsible for the UI representation of an item inside a grid-based inventory
/// - Supports dragging via pointer events
/// - Shows placement feedback through an array of images (one per grid cell)
/// - Exposes UnityEvents so animation or sound systems can react to grab/release/place
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class InventoryItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    #region Serialized / Inspector fields

    [Header("Item Data")]
    [SerializeField] private WeaponItemData spawnData;

    [Space]
    [InfoBox("Automatically generated runtime data. Use GenerateItemData() to recreate from SpawnData.")]
    [ShowIf(nameof(spawnData))]
    [SerializeField] private WeaponItem itemData; // runtime data (generated from spawnData)

    [Header("Shape")]
    public InventoryShapeBase shape;
    public GridLayoutGroup layoutParent; // used for instantiating hitbox children
    public GameObject[] fillObjects; // prefabs used as cell visuals

    [Header("Movement")]
    [SerializeField] private float followSpeed = 15f;

    [Header("Color Feedback")]
    [Range(0, 100)] public float alpha = 33f;
    public Color normal;
    public Color blocked;
    public Color placeable;
    public Image[] feedbackObjects;

    #endregion

    #region Public / Inspector-accessible API

    /// <summary>
    /// Read-only access to the generated runtime item data.
    /// </summary>
    public WeaponItem GetItemData => itemData;
    public WeaponItem SetItemData (WeaponItem item) => itemData = item;

    [Button]
    public void GenerateItemData()
    {
        if (spawnData == null) return;
        itemData = new WeaponItem();
        itemData.Setup(spawnData);
    }

    public void ConnectItem()
    {
        if (itemData != null) itemData.Subscribe();
    }

    public void RemoveItem()
    {
        if (itemData != null) itemData.Unsubscribe();
    }

    [Button]
    public void FillChildren()
    {
        if (shape == null || layoutParent == null || fillObjects == null || fillObjects.Length == 0) return;

        // clear existing children cleanly (editor vs playmode)
        var parent = layoutParent.transform;
        for (int i = parent.childCount - 1; i >= 0; --i)
        {
            var child = parent.GetChild(i);
            if (!Application.isPlaying)
                DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
        }

        var currentShape = shape as InventoryShape;
        var currentAdvShape = shape as AdvancedInventoryShape;

        feedbackObjects = new Image[shape.width * shape.height];

        for (int y = 0; y < shape.height; y++)
        {
            for (int x = 0; x < shape.width; x++)
            {
                int point = currentAdvShape ? currentAdvShape.Get(x, y) : currentShape.Get(x, y);
                point = Mathf.Clamp(point, 0, fillObjects.Length - 1);
                var prefab = fillObjects[point];
                var inst = Instantiate(prefab, layoutParent.transform);
                inst.name = $"cell ({x},{y})";
                feedbackObjects[y * shape.width + x] = inst.GetComponent<Image>();
            }
        }
    }

    /// <summary>
    /// Coordinates of the clicked child cell relative to the layout.
    /// </summary>
    public Vector2 clickedCell;

    [HideInInspector] public FilledPosition currentFilledPosition;

    #endregion

    #region Events

    // Hidden events so other components (ItemAnimator) can subscribe
    public UnityEvent<Vector2> OnGrabbed = new UnityEvent<Vector2>();
    public UnityEvent OnReleased = new UnityEvent();
    public UnityEvent<bool> OnHoveringInventory = new UnityEvent<bool>();
    public UnityEvent OnPlaced = new UnityEvent();

    #endregion

    #region Private state

    private RectTransform rectTransform;
    private bool isDragging;
    public bool IsBeingDragged => isDragging;
    private Vector2 dragOffset;
    private Color currentColor;
    private bool hovering = false;
    [HideInInspector] public bool insideInventory = false;

    #endregion

    #region Unity callbacks

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // maintain insideInventory state based on events
        OnPlaced.AddListener(() => { insideInventory = true; });
        OnGrabbed.AddListener((Vector2 _) => { insideInventory = false; });
    }

    private void Start()
    {
        InventorySaveManager.instance?.SubscribeItem(this);
        if (itemData == null && spawnData != null)
            GenerateItemData();
    }

    private void OnDisable()
    {
        RemoveItem();
    }

    private void Update()
    {
        if (!isDragging) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            Input.mousePosition,
            null,
            out Vector2 localMousePos
        );

        Vector2 targetPos = localMousePos + dragOffset;
        rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPos, Time.deltaTime * followSpeed);
    }

    #endregion

    #region Pointer events

    public void OnPointerDown(PointerEventData eventData)
    {
        // if item was locked in an inventory, free it first
        if (currentFilledPosition.locked && currentFilledPosition.currentInventory != null)
        {
            currentFilledPosition.currentInventory.FreeIndices(currentFilledPosition.occupiedSlots);
            currentFilledPosition.locked = false;

            // Only remove buffs if taken from the main inventory
            if (currentFilledPosition.currentInventory == GeneralInventoryManager.instance?.currentInventory)
                RemoveItem();
        }

        // Find which child cell was clicked
        bool found = GetClickedChildCoords(eventData, out Vector2 _clickedCell);
        if (found)
        {
            clickedCell = _clickedCell;
            if (GeneralInventoryManager.instance != null)
                GeneralInventoryManager.instance.draggedItem = this;
        }

        // Setup drag offset
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out dragOffset
        );

        dragOffset = rectTransform.anchoredPosition - dragOffset;
        isDragging = true;

        // notify listeners that the item was grabbed
        OnGrabbed?.Invoke(dragOffset);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;

        // notify listeners that the item was released
        OnReleased?.Invoke();

        // Try to place (existing manager call)
        if (GeneralInventoryManager.instance != null)
        {
            GeneralInventoryManager.instance.PlaceObject();
            GeneralInventoryManager.instance.draggedItem = null;
        }
    }

    #endregion

    #region Hovering / Color Helpers

    /// <summary>
    /// Notify listeners when the item enters/leaves an inventory area.
    /// </summary>
    public void SetHover(bool b)
    {
        if (hovering == b) return;
        hovering = b;
        OnHoveringInventory?.Invoke(b);
    }

    /// <summary>
    /// Change overlay color applied to all feedback cell images.
    /// </summary>
    public void ChangeColor(Color newColor)
    {
        // Color is a struct; compare to avoid redundant assignments.
        if (currentColor == newColor) return;

        newColor.a = alpha / 100f; // inspector alpha is 0-100
        if (feedbackObjects != null)
        {
            foreach (var img in feedbackObjects)
            {
                if (img == null) continue;
                img.color = newColor;
            }
        }

        currentColor = newColor;
    }

    #endregion

    #region Child detection / helpers

    /// <summary>
    /// Tries to detect which child of layoutParent was clicked.
    /// Returns true and outputs x,y coordinates (based on instantiation order: y * width + x)
    /// </summary>
    private bool GetClickedChildCoords(PointerEventData eventData, out Vector2 cell)
    {
        cell = Vector2.one * -1;

        if (layoutParent == null || layoutParent.transform.childCount == 0 || shape == null)
            return false;

        var parent = layoutParent.transform;
        int childCount = parent.childCount;
        int width = Mathf.Max(1, shape.width);

        for (int i = 0; i < childCount; i++)
        {
            var child = parent.GetChild(i) as RectTransform;
            if (child == null) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(child, eventData.position, eventData.pressEventCamera))
            {
                int index = i;
                int x = index % width;
                int y = index / width;
                cell.x = x;
                cell.y = y;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the RectTransform of the child cell for the provided local grid coordinates.
    /// </summary>
    public RectTransform GetChild(Vector2 pos)
    {
        int i = (int)(pos.y * shape.width) + (int)pos.x;
        return layoutParent.transform.GetChild(i) as RectTransform;
    }

    public void SaveState(ref ItemSaveState save)
    {
        save.position = transform.position;
        save.scale = transform.localScale;
        save.localEul = transform.localEulerAngles;

        save.currentItem = itemData;
        save.currentFilledPos = currentFilledPosition;

        save.typeOfItem = itemData.name switch
        {
            "Rusted Sword" => ItemSaveState.prefabType.RustedSword,
            "Rusted Axe" => ItemSaveState.prefabType.RustedAxe,
            "Fine Sword" => ItemSaveState.prefabType.FineSword,
            "Fine Axe" => ItemSaveState.prefabType.FineAxe,
            _ => ItemSaveState.prefabType.RustedSword
        };
    }

    public void LoadState(ItemSaveState save)
    {
        transform.position = save.position;
        transform.localScale = save.scale;
        transform.localEulerAngles = save.localEul;

        itemData = save.currentItem;

        currentFilledPosition = save.currentFilledPos;

        Invoke("ConnectItem", .25f);

        GetComponent<ItemAnimator>().HandlePlaced();

        this.insideInventory = true;

    }

    #endregion
}

[System.Serializable]
public struct ItemSaveState
{
    [Header("Basics")]
    public Vector3 position;
    public Vector3 scale;
    public Vector3 localEul;

    [Header("Item Data")]
    public WeaponItem currentItem; // save the stats and modifiers so we can build the buff system later

    [Header("Inventory Occupied Slots")]
    public FilledPosition currentFilledPos;

    public enum prefabType { FineAxe, FineSword, RustedAxe, RustedSword }

    public prefabType typeOfItem;
}
