using UnityEngine;

public class ItemAnimator : MonoBehaviour
{
    [Header("Scales")]
    public Vector3 InactiveScale = Vector3.one;
    public Vector3 SelectedScale = Vector3.one * 1.08f;
    public Vector3 PlacedScale = Vector3.one * 0.95f;

    [Header("Lerp / Speed")]
    public float scaleLerpSpeed = 12f;
    public float rotationLerpSpeed = 10f;

    [Header("Jiggle / Rotation")]
    [Tooltip("Strength of the jiggle (applied to small rotation/position offsets)")]
    public float jiggleForce = 6f;
    public float jiggleSpeed = 10f;
    public Vector2 jiggleScale = Vector2.one;
    [Range(0f, 1f)] public float jiggleApplied = 1f;

    [Tooltip("Distance (in local UI units) at which the mouse fully influences rotation. If mouse is closer, item stays upright.")]
    public float maxInfluenceDistance = 200f;

    private RectTransform rt;
    private InventoryItem inventoryItem;

    Vector3 targetScale;
    bool selected = false;
    float jiggleTimer = 0f;
    float currentAngle = 0f;
    bool hovering = false;
    Vector2 dragOffset;
    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        inventoryItem = GetComponent<InventoryItem>();

        // default scale
        targetScale = InactiveScale;
        rt.localScale = InactiveScale;

        // subscribe to events if item exists
        if (inventoryItem != null)
        {
            inventoryItem.OnGrabbed.AddListener(HandleGrabbed);
            inventoryItem.OnReleased.AddListener(HandleReleased);
            inventoryItem.OnHoveringInventory.AddListener(HandleHover);
            inventoryItem.OnPlaced.AddListener(HandlePlaced);
        }
    }
    void OnDestroy()
    {
        if (inventoryItem != null)
        {
            inventoryItem.OnGrabbed.RemoveListener(HandleGrabbed);
            inventoryItem.OnReleased.RemoveListener(HandleReleased);
            inventoryItem.OnPlaced.RemoveListener(HandlePlaced);
        }
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Smooth scale lerp
        rt.localScale = Vector3.Lerp(rt.localScale, targetScale, Time.deltaTime * scaleLerpSpeed);

        // Rotation & jiggle only when selected
        if (selected)
        {
            jiggleTimer += Time.deltaTime * jiggleSpeed;

            // compute mouse local pos relative to parent canvas rect
            RectTransform parentRect = rt.parent as RectTransform;
            Vector2 localMouse;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.mousePosition, null, out localMouse);

            // direction from item to mouse
            Vector2 dir = localMouse - (rt.anchoredPosition-dragOffset);
            float distance = dir.magnitude;

            // angle between up (0,1) and direction in degrees.
            // we want 0 when mouse is straight above (up), and rotate as mouse goes around.
            // using Atan2(x, y) so that 0 corresponds to upwards.
            float angleToMouse = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg; // degrees

            // influence: 0 when very close, 1 when distance >= maxInfluenceDistance
            float influence = Mathf.Clamp01(distance / Mathf.Max(0.0001f, maxInfluenceDistance));

            // target angle is blend between 0 (upright) and angleToMouse
            float targetAngle = Mathf.Lerp(0f, angleToMouse, influence);

            // add small rotational jiggle from Perlin/Sin
            float noiseRot = (Mathf.Sin(jiggleTimer) + (Mathf.PerlinNoise(jiggleTimer, 0f) - 0.5f) * 2f * jiggleScale.x) * jiggleForce * 0.5f * jiggleApplied;
            float desiredAngle = targetAngle + noiseRot;

            // lerp the angle smoothly
            currentAngle = Mathf.LerpAngle(currentAngle, desiredAngle, Time.deltaTime * rotationLerpSpeed);

            // apply rotation around Z (UI)
            rt.localEulerAngles = new Vector3(0f, 0f, -currentAngle);

            // small positional jiggle (adds micro offsets while dragging)
            float noiseX = Mathf.Sin(jiggleTimer) + Mathf.PerlinNoise(jiggleTimer, 0f) * jiggleScale.x;
            float noiseY = Mathf.Sin(jiggleTimer) + Mathf.PerlinNoise(0f, jiggleTimer) * jiggleScale.y;
            Vector2 jiggleOffset = new Vector2(noiseX, noiseY) * jiggleApplied * (jiggleForce * 0.5f);

            // Apply a subtle offset to anchoredPosition; do not override the drag follow done by InventoryItem,
            // instead add a small offset each frame so both work together.
            rt.anchoredPosition += jiggleOffset * Time.deltaTime;
        }
        else
        {
            // Smoothly go back to upright when not selected
            currentAngle = Mathf.LerpAngle(currentAngle, 0f, Time.deltaTime * rotationLerpSpeed);
            rt.localEulerAngles = new Vector3(0f, 0f, -currentAngle);
        }
    }

    void HandleGrabbed(Vector2 dragOffset)
    {
        selected = true;
        targetScale = SelectedScale;
        jiggleTimer = 0f;
        this.dragOffset = dragOffset;
    }

    void HandleReleased()
    {
        selected = false;
        targetScale = InactiveScale;
        inventoryItem.ChangeColor(inventoryItem.normal);
    }
    void HandleHover(bool onHover)
    {
        hovering = onHover;
    }

    public void HandlePlaced()
    {
        selected = false;
        targetScale = PlacedScale;
    }
}
