using UnityEngine;

public class IsometricSortingOrder : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Transform sortingPivot; // bottom-center point of the sprite
    public int sortingOrderBase = 5000;
    public float multiplier = 100f;
    public float offset = 0f; // Y offset to adjust sorting manually
    public bool dynamic = false; // update every frame if true (for NPCs), else static

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (sortingPivot == null)
            sortingPivot = transform; // fallback to transform if no pivot assigned

        if (!dynamic)
            UpdateSortingOrder();
    }

    void LateUpdate()
    {
        if (dynamic)
            UpdateSortingOrder();
    }

    public void UpdateSortingOrder()
    {
        if (spriteRenderer == null) return;

        // Lower Y = higher sorting order, apply offset
        int order = Mathf.RoundToInt(sortingOrderBase - (sortingPivot.position.y + offset) * multiplier);
        spriteRenderer.sortingOrder = order;
    }
}
