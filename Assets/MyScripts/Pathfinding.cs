using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A* 探索＋グラフ上への射影（辺上の最近点）ユーティリティ
/// </summary>
public class Pathfinding : MonoBehaviour
{
    private static Node[] cachedNodes;

    private static readonly List<Node> openSet = new List<Node>();
    private static readonly Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();
    private static readonly Dictionary<Node, float> gScore = new Dictionary<Node, float>();
    private static readonly Dictionary<Node, float> fScore = new Dictionary<Node, float>();

    // ========== キャッシュ ==========
    public static void RefreshCache()
    {
        cachedNodes = GameObject.FindObjectsOfType<Node>();
    }

    private static void EnsureCache()
    {
        if (cachedNodes == null) RefreshCache();
    }

    // ========== グラフ上の最近点（辺への射影） ==========
    public struct GraphProjection
    {
        public Node a;         // 辺の片端
        public Node b;         // 辺の片端
        public Vector3 point;  // 射影点（辺上）
        public float da;       // 射影点→a の距離
        public float db;       // 射影点→b の距離
        public float dist;     // 自己位置→射影点 の距離（参考）
    }

    /// <summary>
    /// 自己位置から、グラフ（ノード間の辺）上の最も近い点を探す。
    /// 辺は neighbors から重複なく列挙。
    /// </summary>
    public static GraphProjection FindClosestProjectionOnGraph(Vector3 worldPos)
    {
        EnsureCache();

        // 重複辺を避ける（InstanceID の小さい→大きい順でキー化）
        var seen = new HashSet<ulong>();
        GraphProjection best = new GraphProjection { a = null, b = null, point = Vector3.zero, da = 0, db = 0, dist = float.PositiveInfinity };

        foreach (var n in cachedNodes)
        {
            if (n == null || n.neighbors == null) continue;

            int idN = n.GetInstanceID();
            Vector3 pN = n.transform.position;

            foreach (var m in n.neighbors)
            {
                if (m == null) continue;
                int idM = m.GetInstanceID();
                if (idN == idM) continue;

                // 無向辺の重複排除
                ulong key = idN < idM
                    ? ((ulong)(uint)idN << 32) | (uint)idM
                    : ((ulong)(uint)idM << 32) | (uint)idN;
                if (seen.Contains(key)) continue;
                seen.Add(key);

                Vector3 pM = m.transform.position;

                // worldPos を セグメント pN-pM に射影（クランプ）
                Vector3 ab = pM - pN;
                float abLen2 = Vector3.SqrMagnitude(ab);
                if (abLen2 < 1e-8f) continue; // 同一点はスキップ

                float t = Vector3.Dot(worldPos - pN, ab) / abLen2;
                t = Mathf.Clamp01(t);
                Vector3 proj = pN + t * ab;

                float d2 = (worldPos - proj).sqrMagnitude;
                if (d2 < best.dist * best.dist)
                {
                    best.a = n;
                    best.b = m;
                    best.point = proj;
                    best.da = Vector3.Distance(proj, pN);
                    best.db = Vector3.Distance(proj, pM);
                    best.dist = Mathf.Sqrt(d2);
                }
            }
        }

        return best;
    }

    // ========== A*（標準：ノード→ノード） ==========
    public static List<Node> FindPath(Node startNode, Node goalNode)
    {
        EnsureCache();

        openSet.Clear();
        cameFrom.Clear();
        gScore.Clear();
        fScore.Clear();

        if (startNode == null || goalNode == null) return null;

        openSet.Add(startNode);

        foreach (var node in cachedNodes)
        {
            if (node == null) continue;
            gScore[node] = float.PositiveInfinity;
            fScore[node] = float.PositiveInfinity;
        }

        gScore[startNode] = 0f;
        fScore[startNode] = Heuristic(startNode, goalNode);

        while (openSet.Count > 0)
        {
            Node current = openSet[0];
            float minF = fScore[current];
            for (int i = 1; i < openSet.Count; i++)
            {
                var node = openSet[i];
                if (fScore[node] < minF)
                {
                    current = node;
                    minF = fScore[node];
                }
            }

            if (current == goalNode)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);

            foreach (var neighbor in current.neighbors)
            {
                if (neighbor == null) continue;
                float tentativeG = gScore[current] + Vector3.Distance(current.transform.position, neighbor.transform.position);

                if (tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goalNode);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    // ========== A*（射影点スタート：ノードにせず2端点を初期候補として与える） ==========
    /// <summary>
    /// 射影点 'proj' から goalNode への最短経路（最初は proj に隣接する a/b を初期フロンティアとして探索）。
    /// 戻り値は「到達ノード列」（描画時は proj.point を先頭に挿入して使う）。
    /// </summary>
    public static List<Node> FindPathFromProjection(GraphProjection proj, Node goalNode)
    {
        EnsureCache();

        openSet.Clear();
        cameFrom.Clear();
        gScore.Clear();
        fScore.Clear();

        if (proj.a == null || proj.b == null || goalNode == null) return null;

        foreach (var node in cachedNodes)
        {
            if (node == null) continue;
            gScore[node] = float.PositiveInfinity;
            fScore[node] = float.PositiveInfinity;
        }

        // 射影点から a / b までの距離を初期 g としてセット
        // どちらから始めても良いので両方を openSet に入れる
        gScore[proj.a] = proj.da;
        fScore[proj.a] = proj.da + Heuristic(proj.a, goalNode);
        openSet.Add(proj.a);

        gScore[proj.b] = proj.db;
        fScore[proj.b] = proj.db + Heuristic(proj.b, goalNode);
        openSet.Add(proj.b);

        while (openSet.Count > 0)
        {
            Node current = openSet[0];
            float minF = fScore[current];
            for (int i = 1; i < openSet.Count; i++)
            {
                var node = openSet[i];
                if (fScore[node] < minF)
                {
                    current = node;
                    minF = fScore[node];
                }
            }

            if (current == goalNode)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);

            foreach (var neighbor in current.neighbors)
            {
                if (neighbor == null) continue;
                float tentativeG = gScore[current] + Vector3.Distance(current.transform.position, neighbor.transform.position);

                if (tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goalNode);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    private static float Heuristic(Node a, Node b)
    {
        return Vector3.Distance(a.transform.position, b.transform.position);
    }

    private static List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node current)
    {
        Stack<Node> stack = new Stack<Node>();
        stack.Push(current);
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            stack.Push(current);
        }
        return new List<Node>(stack);
    }

    // ========== ユーティリティ ==========
    public static float ComputePathLength(List<Node> path)
    {
        if (path == null || path.Count < 2) return float.PositiveInfinity;

        float sum = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            sum += Vector3.Distance(path[i - 1].transform.position, path[i].transform.position);
        }
        return sum;
    }

    public static List<Node> CombinePaths(List<Node> a, List<Node> b)
    {
        if (a == null || a.Count == 0) return b;
        if (b == null || b.Count == 0) return a;

        var result = new List<Node>(a);
        int start = (a[a.Count - 1] == b[0]) ? 1 : 0;
        for (int i = start; i < b.Count; i++)
            result.Add(b[i]);

        return result;
    }

    // 水利ノード
    public static List<Node> GetAllWaterNodes()
    {
        EnsureCache();
        var list = new List<Node>();
        foreach (var n in cachedNodes)
        {
            if (n != null && n.name.StartsWith("W"))
                list.Add(n);
        }
        return list;
    }

    public static Node FindClosestWaterNodeByPath(Node goalNode)
    {
        var waters = GetAllWaterNodes();
        if (waters.Count == 0) return null;

        Node bestWater = null;
        float bestLen = float.PositiveInfinity;

        foreach (var w in waters)
        {
            var path = FindPath(goalNode, w);
            if (path == null || path.Count == 0) continue;

            float len = ComputePathLength(path);
            if (len < bestLen)
            {
                bestLen = len;
                bestWater = w;
            }
        }
        return bestWater;
    }
}
