using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Efecto de "cambio de cámara de vigilancia": un golpe de estática y un cartel
// "● REC  CAM 2 · PASILLO" que se ve un momento arriba al centro de la pantalla.
// Lo crea CamaraPTZ sola; no hay que ponerlo en la escena.
public class EfectoCambioCamara : MonoBehaviour
{
    public float duracionEstatica = 0.35f;
    public float duracionCartel = 2.5f;

    private RawImage estatica;
    private CanvasGroup cartel;
    private TextMeshProUGUI texto;
    private TextMeshProUGUI rec;
    private Texture2D ruido;
    private float inicio = -100f;

    public static EfectoCambioCamara Crear()
    {
        GameObject go = new GameObject("EfectoCambioCamara", typeof(Canvas), typeof(CanvasScaler));
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 50;   // encima del HUD, debajo de la pausa
        CanvasScaler cs = go.GetComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920f, 1080f);
        cs.matchWidthOrHeight = 0.5f;
        return go.AddComponent<EfectoCambioCamara>();
    }

    void Awake()
    {
        // Estática a pantalla completa
        ruido = new Texture2D(128, 128, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
        var px = new Color32[128 * 128];
        for (int i = 0; i < px.Length; i++) { byte v = (byte)Random.Range(0, 256); px[i] = new Color32(v, v, v, 255); }
        ruido.SetPixels32(px);
        ruido.Apply();

        estatica = Nuevo<RawImage>("Estatica", transform);
        Estirar(estatica.rectTransform);
        estatica.texture = ruido;
        estatica.raycastTarget = false;
        estatica.color = new Color(1f, 1f, 1f, 0f);

        // Cartel arriba al centro
        GameObject g = new GameObject("Cartel", typeof(RectTransform), typeof(CanvasGroup));
        g.transform.SetParent(transform, false);
        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -40f);
        rt.sizeDelta = new Vector2(900f, 60f);
        cartel = g.GetComponent<CanvasGroup>();
        cartel.alpha = 0f;
        cartel.blocksRaycasts = false;

        Image fondo = Nuevo<Image>("Fondo", g.transform);
        Estirar(fondo.rectTransform);
        fondo.color = new Color(0f, 0f, 0f, 0.45f);
        fondo.raycastTarget = false;

        rec = Nuevo<TextMeshProUGUI>("Rec", g.transform);
        RectTransform rr = rec.rectTransform;
        rr.anchorMin = new Vector2(0f, 0f); rr.anchorMax = new Vector2(0f, 1f); rr.pivot = new Vector2(0f, 0.5f);
        rr.anchoredPosition = new Vector2(20f, 0f); rr.sizeDelta = new Vector2(140f, 0f);
        rec.text = "● REC";
        rec.fontSize = 34; rec.fontStyle = FontStyles.Bold;
        rec.color = new Color(1f, 0.25f, 0.25f);
        rec.alignment = TextAlignmentOptions.MidlineLeft;
        rec.raycastTarget = false;

        texto = Nuevo<TextMeshProUGUI>("Texto", g.transform);
        RectTransform tr = texto.rectTransform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(160f, 0f); tr.offsetMax = new Vector2(-20f, 0f);
        texto.fontSize = 34; texto.fontStyle = FontStyles.Bold;
        texto.color = new Color(0.85f, 0.97f, 1f);
        texto.alignment = TextAlignmentOptions.MidlineLeft;
        texto.characterSpacing = 4f;
        texto.raycastTarget = false;
    }

    public void Mostrar(int numero, string nombre)
    {
        texto.text = "CAM " + numero + "  ·  " + nombre;
        inicio = Time.unscaledTime;
    }

    void Update()
    {
        float t = Time.unscaledTime - inicio;

        // Estática: golpe fuerte que se apaga rápido, con parpadeo
        float e = t < duracionEstatica ? (1f - t / duracionEstatica) : 0f;
        if (e > 0f)
        {
            estatica.uvRect = new Rect(Random.value, Random.value, 6f, 3.5f);
            estatica.color = new Color(1f, 1f, 1f, e * (Random.value < 0.2f ? 0.5f : 0.85f));
        }
        else if (estatica.color.a != 0f) estatica.color = new Color(1f, 1f, 1f, 0f);

        // Cartel: aparece, se queda y se desvanece
        float a = 0f;
        if (t < duracionCartel) a = t < 0.15f ? t / 0.15f : (t > duracionCartel - 0.6f ? (duracionCartel - t) / 0.6f : 1f);
        cartel.alpha = Mathf.Clamp01(a);

        // El punto rojo de REC parpadea
        if (a > 0f) rec.alpha = Mathf.Repeat(Time.unscaledTime, 1f) < 0.6f ? 1f : 0.25f;
    }

    void OnDestroy()
    {
        if (ruido != null) Destroy(ruido);
    }

    static T Nuevo<T>(string nombre, Transform padre) where T : Component
    {
        GameObject g = new GameObject(nombre, typeof(RectTransform));
        g.transform.SetParent(padre, false);
        return g.AddComponent<T>();
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
