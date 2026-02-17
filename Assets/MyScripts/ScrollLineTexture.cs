using UnityEngine;

public class ScrollLineTexture : MonoBehaviour
{
    public float scrollSpeed = 2.0f;
    private Material lineMat;
    private float offset = 0f;

    void Start()
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        if (lr != null)
        {
            lineMat = lr.material;
        }
    }

    void Update()
    {
        if (lineMat != null)
        {
            offset -= Time.deltaTime * scrollSpeed;
            lineMat.mainTextureOffset = new Vector2(offset, 0);
        }
    }
}