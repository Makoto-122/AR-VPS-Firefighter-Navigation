using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Immersal.AR;
using UnityEngine.Networking;

/// <summary>
/// VPSで自己位置更新。
/// 自己位置をグラフに射影（辺上の最近点）し、その射影点から
/// ・直行（射影→goal）
/// ・水利経由（射影→water→goal）
/// を算出し、設定に応じて描画。
///
/// ★ 火元は HTTP（curl / API）必須
/// ★ WiFi（HTTP）が使えない場合は何もしない
/// ★ 手動指定・バックアップは一切行わない
/// </summary>
public class TestClosestPath : MonoBehaviour
{
    // =========================
    // 位置
    // =========================
    [Header("Positions")]
    public Transform currentLocation;

    // =========================
    // HTTP 火元取得（必須）
    // =========================
    [Header("Fire Source API (Required)")]
    [Tooltip("例: http://<local-server>:5050/fire-source")]
    public string fireSourceUrl = "http://<local-server>:5050/fire-source";

    // HTTPで受信した最新の火元ノード名
    private string latestFireNodeName = null;

    // =========================
    // 更新設定
    // =========================
    [Header("Update Interval")]
    public float updateInterval = 1f;

    // =========================
    // マーカー
    // =========================
    [Header("Markers (Optional)")]
    public GameObject waterMarkerPrefab;
    public GameObject goalMarkerPrefab;
    public float markerYOffset = 1.5f;

    // =========================
    // オプション
    // =========================
    [Header("Options")]
    [Tooltip("true: 必ず水利経由 / false: 直行と水利経由の短い方")]
    public bool forceViaWater = false;

    // =========================
    // 内部
    // =========================
    private PathVisualizer visualizer;
    private WaitForSeconds wait;

    private GameObject lastWaterMarker;
    private GameObject lastGoalMarker;

    // =========================
    // JSON
    // =========================
    [System.Serializable]
    class FireSourceResponse
    {
        public string node;
    }

    // =========================
    // Unity Lifecycle
    // =========================
    void Start()
    {
        visualizer = GameObject.Find("PathDrawer")?.GetComponent<PathVisualizer>();
        if (visualizer == null)
        {
            Debug.LogError("[INIT ERROR] PathDrawer / PathVisualizer が見つかりません。");
            return;
        }

        wait = new WaitForSeconds(updateInterval);
        StartCoroutine(UpdateLoop());
    }

    void OnEnable()
    {
        if (ARLocalizer.Instance != null)
            ARLocalizer.Instance.OnPoseFound += OnVpsPoseUpdated;
    }

    void OnDisable()
    {
        if (ARLocalizer.Instance != null)
            ARLocalizer.Instance.OnPoseFound -= OnVpsPoseUpdated;
    }

    private void OnVpsPoseUpdated(LocalizerPose pose)
    {
        if (currentLocation != null)
        {
            currentLocation.position = pose.lastUpdatedPose.position;
            currentLocation.rotation = pose.lastUpdatedPose.rotation;
        }
    }

    // =========================
    // メインループ
    // =========================
    IEnumerator UpdateLoop()
    {
        while (true)
        {
            // HTTPが使えなければ何もしない
            yield return FetchFireSource();

            // 火元が取得できていなければ描画しない
            if (!string.IsNullOrEmpty(latestFireNodeName))
                yield return ComputeAndDrawPath();

            yield return wait;
        }
    }

    // =========================
    // HTTP 火元取得（必須）
    // =========================
    IEnumerator FetchFireSource()
    {
        using (UnityWebRequest req = UnityWebRequest.Get(fireSourceUrl))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                // WiFiなし／サーバ停止時：何もしない
                Debug.LogWarning("[HTTP] Fire source not available (WiFi unavailable).");
                yield break;
            }

            var json = req.downloadHandler.text;
            var data = JsonUtility.FromJson<FireSourceResponse>(json);

            if (!string.IsNullOrEmpty(data.node))
            {
                latestFireNodeName = data.node;
                Debug.Log("[HTTP] Fire source updated: " + latestFireNodeName);
            }
        }
    }

    // =========================
    // 経路計算＋描画
    // =========================
    IEnumerator ComputeAndDrawPath()
    {
        // ---------- ゴール決定（HTTP必須） ----------
        var goalObj = GameObject.Find(latestFireNodeName);
        if (goalObj == null)
        {
            Debug.LogError("[GOAL ERROR] Node not found: " + latestFireNodeName);
            yield break;
        }

        Node goalNode = goalObj.GetComponent<Node>();
        if (goalNode == null)
        {
            Debug.LogError("[GOAL ERROR] GameObject has no Node component: " + latestFireNodeName);
            yield break;
        }

        // ---------- 射影点 ----------
        Vector3 camPos = currentLocation != null ? currentLocation.position : Vector3.zero;
        var proj = Pathfinding.FindClosestProjectionOnGraph(camPos);

        if (proj.a == null || proj.b == null)
        {
            Debug.LogError("[PROJ ERROR] 射影点算出失敗。Node.neighbors を確認してください。");
            yield break;
        }

        // ---------- 水利 ----------
        Node waterNode = Pathfinding.FindClosestWaterNodeByPath(goalNode);

        // ---------- 経路候補 ----------
        var direct = Pathfinding.FindPathFromProjection(proj, goalNode);

        List<Node> via = null;
        if (waterNode != null)
        {
            var toWater = Pathfinding.FindPathFromProjection(proj, waterNode);
            var toGoal = Pathfinding.FindPath(waterNode, goalNode);
            if (toWater != null && toGoal != null)
                via = Pathfinding.CombinePaths(toWater, toGoal);
        }

        // ---------- 採用 ----------
        List<Node> finalPath;
        if (forceViaWater)
        {
            finalPath = (via != null && via.Count > 0) ? via : direct;
        }
        else
        {
            float lenDirect = Pathfinding.ComputePathLength(direct);
            float lenVia = Pathfinding.ComputePathLength(via);
            finalPath = (lenVia < lenDirect) ? via : direct;
        }

        if (finalPath == null || finalPath.Count == 0)
        {
            Debug.LogWarning("[PATH WARNING] 経路が見つかりません。");
            yield break;
        }

        // ---------- 描画 ----------
        visualizer.DrawPathFromProjection(proj.point, finalPath);

        // ---------- マーカー ----------
        PlaceOrReplaceMarker(ref lastGoalMarker, goalMarkerPrefab,
            goalNode.transform.position + Vector3.up * markerYOffset, "[GOAL]");

        if (waterNode != null && (forceViaWater || finalPath.Contains(waterNode)))
            PlaceOrReplaceMarker(ref lastWaterMarker, waterMarkerPrefab,
                waterNode.transform.position + Vector3.up * markerYOffset, "[WATER]");
        else
        {
            if (lastWaterMarker != null) Destroy(lastWaterMarker);
            lastWaterMarker = null;
        }

        yield return null;
    }

    // =========================
    // マーカー
    // =========================
    private void PlaceOrReplaceMarker(ref GameObject holder, GameObject prefab, Vector3 pos, string fallbackName)
    {
        if (holder != null) Destroy(holder);

        if (prefab != null)
            holder = Instantiate(prefab, pos, Quaternion.identity);
        else
        {
            holder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            holder.transform.position = pos;
            holder.transform.localScale = Vector3.one * 0.3f;
        }
        holder.name = fallbackName;
    }
}
