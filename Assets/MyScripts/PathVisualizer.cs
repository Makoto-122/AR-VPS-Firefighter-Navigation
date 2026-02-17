using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PathVisualizer : MonoBehaviour
{
    [Header("Line Settings")]
    public float width = 0.3f;

    [Tooltip("LineRenderer に適用するマテリアル。'path drawer' を割り当て推奨。")]
    public Material pathMaterial;

    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.widthMultiplier = width;
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.useWorldSpace = true;

        if (pathMaterial != null)
            lineRenderer.material = pathMaterial;
        else
        {
            // 予備：Assets/Resources/path drawer.mat がある場合のみ
            var mat = Resources.Load<Material>("path drawer");
            if (mat != null) lineRenderer.material = mat;
        }
    }

    /// <summary>射影点（Vector3）→ ノード列 という折れ線を1本で描く</summary>
    public void DrawPathFromProjection(Vector3 projectionPoint, List<Node> path)
    {
        if (path == null || path.Count == 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        int count = path.Count + 1; // 先頭に射影点を追加
        if (lineRenderer.positionCount != count)
            lineRenderer.positionCount = count;

        lineRenderer.SetPosition(0, projectionPoint);
        for (int i = 0; i < path.Count; i++)
            lineRenderer.SetPosition(i + 1, path[i].transform.position);
    }

    public void ClearPath()
    {
        lineRenderer.positionCount = 0;
    }
}
