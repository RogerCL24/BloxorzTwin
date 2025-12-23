using UnityEngine;
using UnityEngine.UI; // Necesario para RawImage

[RequireComponent(typeof(RawImage))]
public class ScrollBackground : MonoBehaviour
{
    private RawImage rawImage;

    public float speedX = 0.02f;
    public float speedY = 0.01f;

    void Start()
    {
        rawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        Rect uvRect = rawImage.uvRect;

        float newX = uvRect.x + speedX * Time.deltaTime;
        float newY = uvRect.y + speedY * Time.deltaTime;

        uvRect.position = new Vector2(Mathf.Repeat(newX, 1), Mathf.Repeat(newY, 1));

        rawImage.uvRect = uvRect;
    }
}