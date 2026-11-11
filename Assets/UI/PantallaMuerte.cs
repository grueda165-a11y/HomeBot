using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Pantalla de muerte: aparece cuando el jugador pierde las 3 vidas.
// Bordes rojos que laten, y dos botones: Reintentar y Salir.
// El juego queda en pausa mientras se muestra.
[RequireComponent(typeof(CanvasGroup))]
public class PantallaMuerte : MonoBehaviour
{
    [Header("Referencias")]
    public VidaJugador jugador;
    public Button botonReintentar;
    public Button botonSalir;

    [Tooltip("Imagen de bordes rojos (viñeta).")]
    public Image vineta;

    [Tooltip("Efectos eléctricos que se lanzan al aparecer la pantalla (opcional).")]
    public EfectoElectrico[] efectosAlAparecer;

    [Header("Animación")]
    public float velocidadAparecer = 2.5f;
    public float velocidadLatido = 2.5f;
    [Range(0f, 1f)] public float latidoMinimo = 0.65f;

    private CanvasGroup grupo;
    private bool visible;
    private Color colorVineta;

    private CursorLockMode cursorAnterior;
    private bool cursorVisibleAnterior;

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        if (vineta != null) colorVineta = vineta.color;

        if (botonReintentar != null) botonReintentar.onClick.AddListener(Reintentar);
        if (botonSalir != null) botonSalir.onClick.AddListener(Salir);

        AplicarVisible(false, inmediato: true);
    }

    void OnEnable()
    {
        VidaJugador.SinVidas += Mostrar;
    }

    void OnDisable()
    {
        VidaJugador.SinVidas -= Mostrar;
    }

    void Start()
    {
        if (jugador == null)
            jugador = FindFirstObjectByType<VidaJugador>();
    }

    void Update()
    {
        // Se usa tiempo sin escala porque el juego está en pausa.
        float objetivo = visible ? 1f : 0f;
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, objetivo, velocidadAparecer * Time.unscaledDeltaTime);

        if (vineta != null && visible)
        {
            float latido = Mathf.Lerp(latidoMinimo, 1f, (Mathf.Sin(Time.unscaledTime * velocidadLatido) + 1f) * 0.5f);
            Color c = colorVineta;
            c.a *= latido;
            vineta.color = c;
        }
    }

    public void Mostrar()
    {
        cursorAnterior = Cursor.lockState;
        cursorVisibleAnterior = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        AplicarVisible(true, inmediato: false);

        if (efectosAlAparecer != null)
            foreach (EfectoElectrico e in efectosAlAparecer)
                if (e != null) e.Disparar(1f);

        if (botonReintentar != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(botonReintentar.gameObject);
    }

    public void Reintentar()
    {
        AplicarVisible(false, inmediato: true);

        Cursor.lockState = cursorAnterior;
        Cursor.visible = cursorVisibleAnterior;

        if (jugador != null)
            jugador.Reintentar();
        else
            Time.timeScale = 1f;
    }

    public void Salir()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void AplicarVisible(bool mostrar, bool inmediato)
    {
        visible = mostrar;
        grupo.interactable = mostrar;
        grupo.blocksRaycasts = mostrar;
        if (inmediato) grupo.alpha = mostrar ? 1f : 0f;
    }
}
