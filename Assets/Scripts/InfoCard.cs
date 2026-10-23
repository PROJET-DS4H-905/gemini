using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a simple world-space info card beside this object when it is clicked or
/// tapped (mouse/touch/controller/hand via the injected ISelectInput). Built in code.
/// </summary>
public class InfoCard : MonoBehaviour
{
    [Tooltip("How far (world units) a click can reach to select this object.")]
    [SerializeField] private float clickRange = 100f;

    [Tooltip("Local offset from this object where the card is placed.")]
    [SerializeField] private Vector3 cardOffset = new Vector3(0f, 1.2f, 0f);

    [Tooltip("Body text shown under the object's name.")]
    [SerializeField] private string cardText = "Hello World";

    private RectTransform cardRect;
    private ISelectInput select;

    private void Start()
    {
        BuildCard();
        select = SelectInputFactory.Create();
    }

    public void BuildCard()
    {
        var canvasGO = new GameObject("InfoCard", typeof(RectTransform), typeof(Canvas));
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        cardRect = canvasGO.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(240f, 120f);
        cardRect.localScale = new Vector3(0.01f, 0.01f, 0.01f); // 240px -> 2.4m wide
        cardRect.localPosition = cardOffset;

        var panelGO = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelGO.transform.SetParent(canvasGO.transform, false);
        panelGO.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.16f, 0.85f);
        Stretch(panelGO.GetComponent<RectTransform>());

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGO.transform.SetParent(panelGO.transform, false);
        var text = textGO.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = gameObject.name + "\n" + cardText;
        text.fontSize = 26;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        Stretch(textGO.GetComponent<RectTransform>());

        cardRect.gameObject.SetActive(false); // start hidden, shown on click
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        if (cardRect == null || select == null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        // Keep the card facing the camera while shown.
        if (cardRect.gameObject.activeSelf)
            cardRect.rotation = cam.transform.rotation;

        // Toggle the card when this object is clicked/tapped/triggered.
        if (select.WasPressedThisFrame
            && Physics.Raycast(select.GetPointerRay(), out RaycastHit hit, clickRange)
            && hit.collider.gameObject == gameObject)
            cardRect.gameObject.SetActive(!cardRect.gameObject.activeSelf);
    }
}
