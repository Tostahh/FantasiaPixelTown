using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class POIManager : MonoBehaviour
{
    public static POIManager instance;

    public void Awake()
    {
        if(instance == null)
            instance = this;
    }

    private static Dictionary<POIType, List<POI>> poiRegistry = new();

    public static void RegisterPOI(POI poi)
    {
        if (!poiRegistry.ContainsKey(poi.poiType))
            poiRegistry[poi.poiType] = new List<POI>();
        poiRegistry[poi.poiType].Add(poi);
    }

    public static void UnregisterPOI(POI poi)
    {
        if (poiRegistry.ContainsKey(poi.poiType))
            poiRegistry[poi.poiType].Remove(poi);
    }

    public static POI GetClosestPOI(POIType type, Vector3 fromPosition)
    {
        if (!poiRegistry.ContainsKey(type) || poiRegistry[type].Count == 0)
            return null;

        return poiRegistry[type]
            .OrderBy(p => Vector3.Distance(fromPosition, p.entryPoint.position))
            .FirstOrDefault();
    }

    public static POI GetRandomPOI(POIType type)
    {
        if (!poiRegistry.ContainsKey(type) || poiRegistry[type].Count == 0)
            return null;
        var list = poiRegistry[type];
        return list[Random.Range(0, list.Count)];
    }
}
