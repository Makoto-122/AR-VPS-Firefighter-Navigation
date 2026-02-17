using UnityEngine;
using System.Collections.Generic;

public class NodeManager : MonoBehaviour
{
    public static NodeManager Instance { get; private set; }

    private List<Node> allNodes = new List<Node>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        UpdateNodeList();
    }

    public void UpdateNodeList()
    {
        allNodes.Clear();
        allNodes.AddRange(FindObjectsOfType<Node>());
    }

    public List<Node> GetAllNodes()
    {
        return allNodes;
    }

    public Node FindClosestNode(Vector3 position)
    {
        Node closest = null;
        float minDist = float.PositiveInfinity;

        foreach (var node in allNodes)
        {
            float dist = Vector3.Distance(position, node.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = node;
            }
        }

        return closest;
    }
}
