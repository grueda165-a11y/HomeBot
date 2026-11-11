using UnityEngine;

// Efecto tipo eléctrico para elementos de la UI:
// rayos que chisporrotean alrededor, parpadeo y un pequeño temblor.
// Llama a Disparar() para lanzarlo, o activa 'continuo' para que chispee solo de vez en cuando.
// Funciona aunque el juego esté en pausa (Time.timeScale = 0).
[RequireComponent(typeof(RectTransform))]
public class EfectoElectrico : MonoBehaviour
{
    [Header("Disparo")]
    public float duracion = 0.6f;
    public float temblor = 6f;
    [Tooltip("Cada cuánto cambian de forma los rayos (segundos).")]
    public float intervaloRayos = 0.045f;

    [Header("Chispas automáticas")]
    public bool continuo = false;
    public Vector2 pausaEntreChispas = new Vector2(0.8f, 2.2f);
    public float duracionChispa = 0.25f;

    [Header("Referencias (se crean solas si están vacías)")]
    public RayoUI rayos;
    public CanvasGroup grupo;

    private RectTransform rt;
    private Vector2 desplazamientoAplicado;
    private float fin;
    private float duracionActual = 1f;
    private float proximoCambio;
    private float proximaChispa;

    void Awake()
    {
        rt = (RectTransform)transform;
        if (grupo == null) grupo = GetComponent<CanvasGroup>();
        if (rayos == null) rayos = GetComponentInChildren<RayoUI>(true);
        if (rayos != null) rayos.enabled = false;
        proximaChispa = Time.unscaledTime + Random.Range(pausaEntreChispas.x, pausaEntreChispas.y);
    }

    void OnDisable()
    {
        QuitarTemblor();
        if (grupo != null) grupo.alpha = 1f;
        if (rayos != null) rayos.enabled = false;
    }

    public void Disparar()
    {
        Disparar(duracion);
    }

    public void Disparar(float segundos)
    {
        duracionActual = Mathf.Max(0.01f, segundos);
        fin = Mathf.Max(fin, Time.unscaledTime + duracionActual);
    }

    void LateUpdate()
    {
        float ahora = Time.unscaledTime;

        if (continuo && ahora >= proximaChispa)
        {
            Disparar(duracionChispa);
            proximaChispa = ahora + duracionChispa + Random.Range(pausaEntreChispas.x, pausaEntreChispas.y);
        }

        QuitarTemblor();

        bool activo = ahora < fin;
        if (!activo)
        {
            if (grupo != null) grupo.alpha = 1f;
            if (rayos != null && rayos.enabled) rayos.enabled = false;
            return;
        }

        float fuerza = Mathf.Clamp01((fin - ahora) / duracionActual);

        // Temblor
        desplazamientoAplicado = Random.insideUnitCircle * temblor * fuerza;
        rt.anchoredPosition += desplazamientoAplicado;

        // Parpadeo
        if (grupo != null)
            grupo.alpha = Random.value < 0.25f ? Random.Range(0.35f, 0.7f) : 1f;

        // Rayos
        if (rayos != null && ahora >= proximoCambio)
        {
            rayos.enabled = Random.value < 0.85f;
            rayos.Regenerar();
            proximoCambio = ahora + intervaloRayos;
        }
    }

    void QuitarTemblor()
    {
        if (rt != null && desplazamientoAplicado != Vector2.zero)
        {
            rt.anchoredPosition -= desplazamientoAplicado;
            desplazamientoAplicado = Vector2.zero;
        }
    }
}
