using System.Collections.Generic;
using UnityEngine;
using Immersal.AR;

public class MapMemoryManager : MonoBehaviour
{
    public float keepDistance = 1.5f;
    private Dictionary<int, ARMap> allMaps = new Dictionary<int, ARMap>();

    void Start()
    {
        ARMap[] maps = GameObject.FindObjectsOfType<ARMap>();
        foreach (var map in maps)
        {
            if (!allMaps.ContainsKey(map.mapId))
            {
                allMaps.Add(map.mapId, map);
            }
        }
    }

    public void UnloadFarMaps(List<Node> path)
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("[MapMemoryManager] 経路が空です");
            return;
        }

        HashSet<int> keepMapIds = new HashSet<int>();

        foreach (var node in path)
        {
            ARMap map = node.GetComponentInParent<ARMap>();
            if (map != null)
            {
                keepMapIds.Add(map.mapId);
            }
        }

        foreach (var mapPair in allMaps)
        {
            int mapId = mapPair.Key;
            ARMap map = mapPair.Value;

            if (!keepMapIds.Contains(mapId))
            {
                map.FreeMap();
                Debug.Log($"[MapMemoryManager] Map {mapId} をアンロードしました");
            }
        }
    }

    public void ReloadAllMaps()
    {
        foreach (var map in allMaps.Values)
        {
            if (map.mapFile != null)
            {
                map.LoadMap();
                Debug.Log($"[MapMemoryManager] Map {map.mapId} を再ロードしました");
            }
        }
    }
}
