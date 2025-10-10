using UnityEngine;

/// <summary>
/// Destroys the GameObject after a set amount of time.
/// </summary>
public class destroyOnTime : MonoBehaviour
{
    [Tooltip("Time (in seconds) before this object is destroyed.")]
    public float lifetime = 3f;

    private void Start()
    {
        // Schedule destruction after the specified duration
        Destroy(gameObject, lifetime);
    }
}
