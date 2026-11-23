using UnityEngine;

// Ojos del robot del menú principal: miran hacia el mouse y parpadean de vez en cuando.
// Va en un contenedor (girado igual que la cabeza del robot) con los dos ojos como hijos.
[RequireComponent(typeof(RectTransform))]
public class OjosRobot : MonoBehaviour
{
    public RectTransform[] ojos;

    [Header("Mirar al mouse")]
    [Tooltip("Cuánto se pueden mover los ojos (proporción del ancho de la cara).")]
    [Range(0f, 0.3f)] public float desplazamientoMaximo = 0.08f;
    [Tooltip("A qué distancia del robot (en píxeles de referencia) los ojos ya miran del todo hacia el mouse.")]
    public float distanciaCompleta = 450f;
    public float suavidad = 10f;

    [Header("Parpadeo")]
    public Vector2 pausaEntreParpadeos = new Vector2(2.5f, 6f);
    public float duracionParpadeo = 0.15f;

    private RectTransform rt;
    private Canvas canvas;
    private Vector2[] posicionBase;
    private Vector2 actual;
    private float proximoParpadeo;
    private float inicioParpadeo = -10f;

    void Awake()
    {
        rt = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();

        posicionBase = new Vector2[ojos.Length];
        for (int i = 0; i < ojos.Length; i++)
            if (ojos[i] != null) posicionBase[i] = ojos[i].anchoredPosition;

        proximoParpadeo = Time.unscaledTime + Random.Range(pausaEntreParpadeos.x, pausaEntreParpadeos.y);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // ---- Mirar al mouse ----
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 centro = RectTransformUtility.WorldToScreenPoint(cam, rt.position);
        Vector2 hacia = (Vector2)Input.mousePosition - centro;

        // Se pasa a la orientación de la cabeza (que está inclinada)
        Vector2 local = Quaternion.Inverse(rt.rotation) * hacia;
        float escalaCanvas = canvas != null ? canvas.scaleFactor : 1f;
        float fuerza = Mathf.Clamp01(local.magnitude / (distanciaCompleta * escalaCanvas));
        Vector2 objetivo = local.sqrMagnitude > 1f ? local.normalized * fuerza * desplazamientoMaximo * rt.rect.width : Vector2.zero;

        actual = Vector2.Lerp(actual, objetivo, 1f - Mathf.Exp(-suavidad * dt));

        // ---- Parpadeo ----
        float ahora = Time.unscaledTime;
        if (ahora >= proximoParpadeo)
        {
            inicioParpadeo = ahora;
            proximoParpadeo = ahora + Random.Range(pausaEntreParpadeos.x, pausaEntreParpadeos.y);
        }
        float t = (ahora - inicioParpadeo) / duracionParpadeo;
        float alto = t >= 0f && t < 1f ? Mathf.Lerp(0.1f, 1f, Mathf.Abs(t * 2f - 1f)) : 1f;

        for (int i = 0; i < ojos.Length; i++)
        {
            if (ojos[i] == null) continue;
            ojos[i].anchoredPosition = posicionBase[i] + actual;
            ojos[i].localScale = new Vector3(1f, alto, 1f);
        }
    }
}
