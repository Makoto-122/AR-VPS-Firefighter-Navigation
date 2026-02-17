using System.Collections.Generic;
using UnityEngine;

public class PathTester : MonoBehaviour
{
    public Node startNode;
    public Node goalNode;

    void Start()
    {
        List<Node> path = Pathfinding.FindPath(startNode, goalNode);

        if (path != null)
        {
            Debug.Log("経路見つかりました：");
            foreach (var node in path)
            {
                Debug.Log(node.name);
            }
        }
        else
        {
            Debug.LogWarning("経路が見つかりませんでした");
        }
    }
}
