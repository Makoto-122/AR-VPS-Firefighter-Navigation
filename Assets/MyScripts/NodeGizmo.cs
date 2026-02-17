// NodeGizmo.cs
using UnityEngine;

[ExecuteAlways]
public class NodeGizmo : MonoBehaviour
{
    private Node node;

    void OnDrawGizmos()
    {
        if (node == null) node = GetComponent<Node>();
        if (node == null) return;

        Gizmos.color = Color.cyan;
        foreach (var neighbor in node.neighbors)
        {
            if (neighbor != null)
            {
                Gizmos.DrawLine(transform.position, neighbor.transform.position);
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.1f);
    }
}
