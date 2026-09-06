using System.Collections;
using System.Collections.Generic;
using NewWorld.HexTerrain;
using UnityEngine;

[DisallowMultipleComponent]
public class HexBuildingPlacementArea : MonoBehaviour
{
    private const string DefaultRootName = "Placed Buildings";
    private const string ScavengerBuildingId = "__SCAVENGER__";
    private const string ScavengerPrefabResourcePath = "Buildings/Sca/Scavenger";

    [SerializeField] private HexTerrainRegionObject regionObject;
    [SerializeField] private Transform buildingRoot;
    [SerializeField] private float heightOffset = 0.08f;
    [SerializeField] private bool placeUnderConstructionBuildings = true;
    [SerializeField] private bool animateSpawn = true;
    [SerializeField] private float spawnTweenDuration = 0.45f;
    [SerializeField] private float spawnTweenDepth = 0.8f;
    [SerializeField] private AnimationCurve spawnTweenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private readonly Dictionary<int, GameObject> spawnedBySlot = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, string> spawnedBuildingIdsBySlot = new Dictionary<int, string>();
    private readonly Dictionary<int, Coroutine> spawnTweensBySlot = new Dictionary<int, Coroutine>();
    private readonly List<List<Vector2Int>> buildableAreas = new List<List<Vector2Int>>();
    private HexTerrainRegionObject cachedRegionObject;
    private int cachedBuildableCellsCount = -1;
    private int cachedBuildableCellsHash;
    private bool buildableAreasDirty = true;

    public int BuildableSlotCount
    {
        get
        {
            EnsureBuildableAreas();
            return buildableAreas.Count;
        }
    }

    public bool HasBuildableSlots => BuildableSlotCount > 0;

    private void Awake()
    {
        GetRegionObject();
    }

    private void Reset()
    {
        regionObject = GetComponent<HexTerrainRegionObject>();
        InvalidateBuildableAreas();
    }

    private void OnValidate()
    {
        InvalidateBuildableAreas();
    }

    public void InvalidateBuildableAreas()
    {
        buildableAreasDirty = true;
    }

    public bool CanPlaceSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < BuildableSlotCount;
    }

    public bool TryGetSlotCell(int slotIndex, out Vector2Int cell)
    {
        cell = default;
        EnsureBuildableAreas();

        if (slotIndex < 0 || slotIndex >= buildableAreas.Count || buildableAreas[slotIndex].Count == 0)
        {
            return false;
        }

        cell = buildableAreas[slotIndex][0];
        return true;
    }

    public int GetSlotCellCount(int slotIndex)
    {
        EnsureBuildableAreas();
        return slotIndex >= 0 && slotIndex < buildableAreas.Count ? buildableAreas[slotIndex].Count : 0;
    }

    public Vector3 GetSlotWorldPosition(int slotIndex)
    {
        EnsureBuildableAreas();
        if (slotIndex < 0 || slotIndex >= buildableAreas.Count || buildableAreas[slotIndex].Count == 0)
        {
            return transform.position;
        }

        HexTerrainRegionObject source = GetRegionObject();
        if (source == null)
        {
            return transform.position;
        }

        Vector3 sum = Vector3.zero;
        List<Vector2Int> area = buildableAreas[slotIndex];
        for (int i = 0; i < area.Count; i++)
        {
            sum += source.GetCellWorldCenter(area[i], heightOffset);
        }

        return sum / area.Count;
    }

    public void SyncBuildings(IReadOnlyList<BuildingInstance> buildings)
    {
        int slotCount = BuildableSlotCount;
        List<int> staleSlots = new List<int>();

        foreach (KeyValuePair<int, GameObject> pair in spawnedBySlot)
        {
            if (pair.Key >= slotCount || buildings == null || pair.Key >= buildings.Count)
            {
                staleSlots.Add(pair.Key);
            }
        }

        for (int i = 0; i < staleSlots.Count; i++)
        {
            ClearSlot(staleSlots[i]);
        }

        if (buildings == null)
        {
            return;
        }

        int count = Mathf.Min(slotCount, buildings.Count);
        for (int i = 0; i < count; i++)
        {
            PlaceOrClearSlot(i, buildings[i]);
        }
    }

    public bool PlaceOrClearSlot(int slotIndex, BuildingInstance building)
    {
        if (!CanPlaceSlot(slotIndex))
        {
            ClearSlot(slotIndex);
            return false;
        }

        if (building != null && building.IsScavengerLocked())
        {
            return PlaceScavengerSlot(slotIndex);
        }

        if (building == null || building.IsEmptySlot() || (!placeUnderConstructionBuildings && building.IsUnderConstruction()))
        {
            ClearSlot(slotIndex);
            return true;
        }

        BuildingData data = building.data;
        if (data == null)
        {
            ClearSlot(slotIndex);
            return false;
        }

        string buildingId = string.IsNullOrWhiteSpace(data.ID) ? data.name : data.ID.Trim();
        if (TryReuseSpawned(slotIndex, buildingId))
        {
            MoveSpawnedToSlot(slotIndex, data.LoadPrefab());
            return true;
        }

        ClearSlot(slotIndex);

        GameObject prefab = data.LoadPrefab();
        if (prefab == null)
        {
            return false;
        }

        Transform root = GetOrCreateBuildingRoot();
        GameObject spawned = Instantiate(prefab, root, false);
        spawned.name = $"BuildingSlot_{slotIndex}_{buildingId}";
        MoveSpawnedToSlotImmediate(spawned, slotIndex);
        PlaySpawnTween(slotIndex, spawned);

        spawnedBySlot[slotIndex] = spawned;
        spawnedBuildingIdsBySlot[slotIndex] = buildingId;
        return true;
    }

    private bool PlaceScavengerSlot(int slotIndex)
    {
        GameObject prefab = LoadScavengerPrefab();
        if (prefab == null)
        {
            ClearSlot(slotIndex);
            return false;
        }

        if (TryReuseSpawned(slotIndex, ScavengerBuildingId))
        {
            MoveSpawnedToSlot(slotIndex, prefab);
            return true;
        }

        ClearSlot(slotIndex);

        Transform root = GetOrCreateBuildingRoot();
        GameObject spawned = Instantiate(prefab, root, false);
        spawned.name = $"BuildingSlot_{slotIndex}_Scavenger";
        MoveSpawnedToSlotImmediate(spawned, slotIndex);
        PlaySpawnTween(slotIndex, spawned);

        spawnedBySlot[slotIndex] = spawned;
        spawnedBuildingIdsBySlot[slotIndex] = ScavengerBuildingId;
        return true;
    }

    public void ClearSlot(int slotIndex)
    {
        StopSpawnTween(slotIndex);

        if (spawnedBySlot.TryGetValue(slotIndex, out GameObject spawned) && spawned != null)
        {
            DestroySpawned(spawned);
        }

        spawnedBySlot.Remove(slotIndex);
        spawnedBuildingIdsBySlot.Remove(slotIndex);
    }

    public void ClearAllSlots()
    {
        List<int> slots = new List<int>(spawnedBySlot.Keys);
        for (int i = 0; i < slots.Count; i++)
        {
            ClearSlot(slots[i]);
        }
    }

    private bool TryReuseSpawned(int slotIndex, string buildingId)
    {
        return spawnedBySlot.TryGetValue(slotIndex, out GameObject spawned)
            && spawned != null
            && spawnedBuildingIdsBySlot.TryGetValue(slotIndex, out string spawnedBuildingId)
            && spawnedBuildingId == buildingId;
    }

    private void MoveSpawnedToSlot(int slotIndex, GameObject prefab)
    {
        if (!spawnedBySlot.TryGetValue(slotIndex, out GameObject spawned) || spawned == null)
        {
            return;
        }

        StopSpawnTween(slotIndex);
        MoveSpawnedToSlotImmediate(spawned, slotIndex);
        if (prefab != null)
        {
            spawned.transform.localRotation = prefab.transform.localRotation;
            spawned.transform.localScale = prefab.transform.localScale;
        }
    }

    private void MoveSpawnedToSlotImmediate(GameObject spawned, int slotIndex)
    {
        if (spawned == null)
        {
            return;
        }

        spawned.transform.position = GetSlotWorldPosition(slotIndex);
    }

    private static GameObject LoadScavengerPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>(ScavengerPrefabResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning($"[HexBuildingPlacementArea] Scavenger prefab load failed. path: Resources/{ScavengerPrefabResourcePath}");
        }

        return prefab;
    }

    private void PlaySpawnTween(int slotIndex, GameObject spawned)
    {
        StopSpawnTween(slotIndex);

        if (!Application.isPlaying || !animateSpawn || spawned == null || spawnTweenDuration <= 0f)
        {
            return;
        }

        Vector3 targetPosition = GetSlotWorldPosition(slotIndex);
        Vector3 startPosition = targetPosition + Vector3.down * Mathf.Max(0f, spawnTweenDepth);
        spawned.transform.position = startPosition;
        spawnTweensBySlot[slotIndex] = StartCoroutine(TweenSpawnPosition(slotIndex, spawned.transform, startPosition, targetPosition));
    }

    private IEnumerator TweenSpawnPosition(int slotIndex, Transform target, Vector3 startPosition, Vector3 targetPosition)
    {
        float elapsed = 0f;
        while (elapsed < spawnTweenDuration)
        {
            if (target == null)
            {
                spawnTweensBySlot.Remove(slotIndex);
                yield break;
            }

            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / spawnTweenDuration);
            float easedTime = spawnTweenCurve != null ? spawnTweenCurve.Evaluate(normalizedTime) : normalizedTime;
            target.position = Vector3.LerpUnclamped(startPosition, targetPosition, easedTime);
            yield return null;
        }

        if (target != null)
        {
            target.position = targetPosition;
        }

        spawnTweensBySlot.Remove(slotIndex);
    }

    private void StopSpawnTween(int slotIndex)
    {
        if (!spawnTweensBySlot.TryGetValue(slotIndex, out Coroutine tween) || tween == null)
        {
            spawnTweensBySlot.Remove(slotIndex);
            return;
        }

        StopCoroutine(tween);
        spawnTweensBySlot.Remove(slotIndex);
    }

    private HexTerrainRegionObject GetRegionObject()
    {
        if (regionObject == null)
        {
            regionObject = GetComponent<HexTerrainRegionObject>();
        }

        return regionObject;
    }

    private void EnsureBuildableAreas()
    {
        HexTerrainRegionObject source = GetRegionObject();
        int buildableCellsCount = source != null && source.BuildableCells != null ? source.BuildableCells.Count : 0;
        int buildableCellsHash = CalculateBuildableCellsHash(source);

        if (!buildableAreasDirty
            && cachedRegionObject == source
            && cachedBuildableCellsCount == buildableCellsCount
            && cachedBuildableCellsHash == buildableCellsHash)
        {
            return;
        }

        RebuildBuildableAreas(source, buildableCellsCount, buildableCellsHash);
    }

    private void RebuildBuildableAreas(
        HexTerrainRegionObject source,
        int buildableCellsCount,
        int buildableCellsHash)
    {
        buildableAreas.Clear();
        cachedRegionObject = source;
        cachedBuildableCellsCount = buildableCellsCount;
        cachedBuildableCellsHash = buildableCellsHash;
        buildableAreasDirty = false;

        if (source == null || source.BuildableCells == null || buildableCellsCount == 0)
        {
            return;
        }

        HashSet<Vector2Int> buildableSet = new HashSet<Vector2Int>(source.BuildableCells);
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();

        for (int i = 0; i < source.BuildableCells.Count; i++)
        {
            Vector2Int start = source.BuildableCells[i];
            if (!buildableSet.Contains(start) || visited.Contains(start))
            {
                continue;
            }

            List<Vector2Int> area = new List<Vector2Int>();
            visited.Add(start);
            frontier.Enqueue(start);

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                area.Add(current);

                for (int direction = 0; direction < 6; direction++)
                {
                    Vector2Int neighbor = HexTerrainGenerator.GetNeighbor(current.x, current.y, direction);
                    if (!buildableSet.Contains(neighbor) || visited.Contains(neighbor))
                    {
                        continue;
                    }

                    visited.Add(neighbor);
                    frontier.Enqueue(neighbor);
                }
            }

            buildableAreas.Add(area);
        }
    }

    private static int CalculateBuildableCellsHash(HexTerrainRegionObject source)
    {
        if (source == null || source.BuildableCells == null || source.BuildableCells.Count == 0)
        {
            return 0;
        }

        unchecked
        {
            int hash = 17;
            for (int i = 0; i < source.BuildableCells.Count; i++)
            {
                Vector2Int cell = source.BuildableCells[i];
                hash = hash * 31 + cell.x;
                hash = hash * 31 + cell.y;
            }

            return hash;
        }
    }

    private Transform GetOrCreateBuildingRoot()
    {
        if (buildingRoot != null)
        {
            return buildingRoot;
        }

        Transform existing = transform.Find(DefaultRootName);
        if (existing != null)
        {
            buildingRoot = existing;
            return buildingRoot;
        }

        GameObject rootObject = new GameObject(DefaultRootName);
        rootObject.transform.SetParent(transform, false);
        buildingRoot = rootObject.transform;
        return buildingRoot;
    }

    private static void DestroySpawned(GameObject spawned)
    {
        if (spawned == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(spawned);
        }
        else
        {
            DestroyImmediate(spawned);
        }
    }
}
