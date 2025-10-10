using UnityEngine;

/// <summary>
/// Clickable rubble or tutorial collectable that grants resources to the player.
/// </summary>
public class CollectibleRubble : MonoBehaviour
{
    [Header("Resource Reward")]
    public int goldReward;
    public int materialReward;
    public int manaReward;

    [Header("Optional Settings")]
    public bool destroyOnCollect = true;
    public GameObject collectEffect; // e.g., particle effect or sparkle

    private bool collected = false;

    public void Collect()
    {
        if (collected) return; // prevent double-collection
        collected = true;

        // Add rewards
        if (goldReward > 0)
            ResourceManager.Instance.AddGold(goldReward);

        if (materialReward > 0)
            ResourceManager.Instance.AddMaterials(materialReward);

        if (manaReward > 0)
            ResourceManager.Instance.AddMana(manaReward);

        Debug.Log($"[CollectibleRubble] Collected! +{goldReward}G +{materialReward}M +{manaReward}Mana");

        // Optional visual feedback
        if (collectEffect != null)
            Instantiate(collectEffect, transform.position, Quaternion.identity, null);

        if (destroyOnCollect)
            Destroy(gameObject);
    }
}
