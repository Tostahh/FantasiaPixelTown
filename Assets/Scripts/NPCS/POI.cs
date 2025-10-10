using UnityEngine;

public class POI : MonoBehaviour
{
    public POIType poiType;
    public Transform entryPoint; // where NPCs actually walk to (can be an empty child)

    private void OnEnable()
    {
        POIManager.RegisterPOI(this);
    }

    private void OnDisable()
    {
        POIManager.UnregisterPOI(this);
    }
}

public enum POIType
{
    None,
    Inn,
    Blacksmith,
    Shrine,
    Market,
    GuardPost
}
