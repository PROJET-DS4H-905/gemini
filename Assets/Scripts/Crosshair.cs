using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

/// <summary>
/// Simple screen-center crosshair dot so the player can see what they are aiming at.
/// </summary>
public class Crosshair : MonoBehaviour
{
    [Tooltip("Dot color (alpha controls transparency).")]
    [SerializeField] private Color dotColor = new Color(1f, 1f, 1f, 0.2f);

    [Tooltip("Dot size in screen pixels.")]
    [SerializeField] private float dotSize = 6f;

    private void Start()
    {
        if (Application.isMobilePlatform || XRSettings.isDeviceActive)
            return; // no cursor/crosshair on mobile or in VR
        Build();
    }

    public void Build()
    {
        var canvasGO = new GameObject("Crosshair", typeof(RectTransform), typeof(Canvas));
        canvasGO.transform.SetParent(transform, false);
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var dotGO = new GameObject("Dot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dotGO.transform.SetParent(canvasGO.transform, false);

        var dot = dotGO.GetComponent<Image>();
        dot.sprite = MakeCircleSprite(64);
        dot.color = dotColor;
        dot.raycastTarget = false;

        var rt = dotGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(dotSize, dotSize);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
    }

    private static Sprite MakeCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = new Vector2(size / 2f - 0.5f, size / 2f - 0.5f);
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - dist + 0.5f); // soft anti-aliased edge
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
