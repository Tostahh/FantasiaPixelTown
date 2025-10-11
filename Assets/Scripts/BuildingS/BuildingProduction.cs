using UnityEngine;

public enum ResourceType { Gold, Materials, Mana }

[RequireComponent(typeof(Building))]
public class BuildingProduction : MonoBehaviour
{
    [Header("Production Settings")]
    public ResourceType resourceType;
    public int[] productionPerTier = new int[] { 0, 1, 2 }; // Amount produced per tick per tier
    public float productionInterval = 5f; // seconds between production ticks

    private Building building;
    private float productionTimer;

    private void Awake()
    {
        building = GetComponent<Building>();
        productionTimer = productionInterval;
    }

    private void Update()
    {
        if (building == null || building.CurrentTier < 0) return;

        productionTimer -= Time.deltaTime;
        if (productionTimer <= 0f)
        {
            Produce();
            productionTimer = productionInterval;
        }
    }

    private void Produce()
    {
        int tierIndex = Mathf.Clamp(building.CurrentTier, 0, productionPerTier.Length - 1);
        int amount = productionPerTier[tierIndex];

        switch (resourceType)
        {
            case ResourceType.Gold:
                if(amount > 0)
                ResourceManager.Instance.AddGold(amount);
                break;
            case ResourceType.Materials:
                if(amount > 0)
                ResourceManager.Instance.AddMaterials(amount);
                break;
            case ResourceType.Mana:
                if (amount > 0)
                ResourceManager.Instance.AddMana(amount);
                break;
        }

        // Optional: add visual or audio feedback for production
        // e.g., a floating text showing +Gold
    }
}
