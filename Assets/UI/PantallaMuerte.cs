using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Pantalla de "Game Over" del robot de juguete que se queda sin batería.
// Secuencia (funciona con el juego en pausa):
//   1. Cortocircuito: estática y franjas de interferencia (sin rayos).
//   2. Apagado tipo tele vieja: la imagen se cierra en una línea y luego en un punto.
//   3. Silencio en negro con sonido de piezas metálicas cayendo.
//   4. Aparece la carita del robot apagada (ojos en X), el título y los botones Reintentar / Menú.
[RequireComponent(typeof(CanvasGroup))]
public class PantallaMuerte : MonoBehaviour
{
    [Header("Referencias")]
    public VidaJugador jugador;
    public Button botonReintentar;
    [Tooltip("Botón que lleva al menú de inicio.")]
    public Button botonSalir;
    public string escenaMenu = "MenuInicio";
    public string textoBotonMenu = "MENÚ";

    [Header("Fase 1: cortocircuito")]
    public RawImage estatica;
    public Image[] franjasGlitch;
    public float duracionCortocircuito = 0.9f;

    [Header("Fase 2: apagado tipo tele")]
    public RectTransform cortinaArriba;
    public RectTransform cortinaAbajo;
    public RectTransform lineaApagado;
    public float duracionCierre = 0.35f;
    public float duracionPunto = 0.3f;

    [Header("Fase 4: robot apagado")]
    public CanvasGroup contenido;
    public Image pantallaCarita;
    public TMP_Text titulo;
    public Color colorPantallaApagada = new Color(0.10f, 0.16f, 0.17f);
    public Color colorPantallaChispazo = new Color(0.35f, 0.75f, 0.8f);
    public float pausaEnNegro = 0.6f;
    public float duracionAparecer = 0.8f;

    [Header("Sonidos (vacío = se generan por código)")]
    public AudioSource audioSource;
    public AudioClip sonidoChispazo;
    public AudioClip sonidoApagado;
    public AudioClip sonidoPiezas;
    [Range(0f, 1f)] public float volumen = 0.8f;

    private CanvasGroup grupo;
    private Coroutine secuencia;
    private Texture2D texturaEstatica;

    private CursorLockMode cursorAnterior;
    private bool cursorVisibleAnterior;

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();

        if (botonReintentar != null) botonReintentar.onClick.AddListener(Reintentar);
        if (botonSalir != null)
        {
            botonSalir.onClick.AddListener(Salir);
            // El botón ahora lleva al menú: se cambia su texto aunque la escena se armara antes
            TMP_Text etiqueta = botonSalir.GetComponentInChildren<TMP_Text>(true);
            if (etiqueta != null && !string.IsNullOrEmpty(textoBotonMenu)) etiqueta.text = textoBotonMenu;
        }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.ignoreListenerPause = true;

        if (sonidoChispazo == null) sonidoChispazo = SonidosRobot.Chispazo();
        if (sonidoApagado == null) sonidoApagado = SonidosRobot.Apagado();
        if (sonidoPiezas == null) sonidoPiezas = SonidosRobot.PiezasCayendo();

        CrearTexturaEstatica();
        Ocultar();
    }

    void OnEnable()  { VidaJugador.SinVidas += Mostrar; }
    void OnDisable() { VidaJugador.SinVidas -= Mostrar; }

    void Start()
    {
        if (jugador == null)
            jugador = FindFirstObjectByType<VidaJugador>();
    }

    void OnDestroy()
    {
        if (texturaEstatica != null) Destroy(texturaEstatica);
    }

    // ---------------- Secuencia ----------------

    public void Mostrar()
    {
        cursorAnterior = Cursor.lockState;
        cursorVisibleAnterior = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (secuencia != null) StopCoroutine(secuencia);
        secuencia = StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        Ocultar();
        grupo.alpha = 1f;
        grupo.blocksRaycasts = true;   // bloquea clics mientras dura la animación

        // ---- 1. Cortocircuito ----
        Sonar(sonidoChispazo);
        if (estatica != null) estatica.gameObject.SetActive(true);

        for (float t = 0f; t < duracionCortocircuito; t += Time.unscaledDeltaTime)
        {
            float p = t / duracionCortocircuito;
            MoverEstatica(Mathf.Lerp(0.25f, 0.85f, p) * (Random.value < 0.15f ? 0.3f : 1f));
            MoverFranjas(p);
            yield return null;
        }
        ApagarFranjas();

        // ---- 2. Apagado tipo tele: se cierra en una línea ----
        Sonar(sonidoApagado);
        if (lineaApagado != null) lineaApagado.gameObject.SetActive(true);
        SetLinea(1f, 1f);

        for (float t = 0f; t < duracionCierre; t += Time.unscaledDeltaTime)
        {
            float p = Suave(t / duracionCierre);
            SetCortinas(p);
            MoverEstatica(Mathf.Lerp(0.85f, 0.4f, p));
            yield return null;
        }
        SetCortinas(1f, sinHueco: true);   // negro total; la línea queda dibujada encima
        if (estatica != null) estatica.gameObject.SetActive(false);

        // ... y luego en un punto que se desvanece
        for (float t = 0f; t < duracionPunto; t += Time.unscaledDeltaTime)
        {
            float p = t / duracionPunto;
            SetLinea(Mathf.Lerp(1f, 0.004f, Suave(Mathf.Min(1f, p * 1.6f))), 1f - Mathf.Max(0f, p - 0.6f) / 0.4f);
            yield return null;
        }
        if (lineaApagado != null) lineaApagado.gameObject.SetActive(false);

        // ---- 3. Negro + piezas cayendo ----
        Sonar(sonidoPiezas);
        yield return new WaitForSecondsRealtime(pausaEnNegro);

        // ---- 4. Robot apagado ----
        if (contenido != null)
        {
            contenido.gameObject.SetActive(true);
            for (float t = 0f; t < duracionAparecer; t += Time.unscaledDeltaTime)
            {
                float p = t / duracionAparecer;
                // Parpadea como si intentara encender y no pudiera
                bool chispazo = (p > 0.25f && p < 0.32f) || (p > 0.5f && p < 0.55f);
                contenido.alpha = chispazo ? 1f : p;
                if (pantallaCarita != null)
                    pantallaCarita.color = chispazo ? colorPantallaChispazo : colorPantallaApagada;
                if (titulo != null)
                    titulo.alpha = Random.value < 0.2f ? 0.3f : 1f;
                yield return null;
            }
            contenido.alpha = 1f;
            contenido.interactable = true;
            if (pantallaCarita != null) pantallaCarita.color = colorPantallaApagada;
            if (titulo != null) titulo.alpha = 1f;
        }

        grupo.interactable = true;
        if (botonReintentar != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(botonReintentar.gameObject);

        secuencia = null;
    }

    // ---------------- Botones ----------------

    public void Reintentar()
    {
        if (secuencia != null) { StopCoroutine(secuencia); secuencia = null; }
        Ocultar();

        Cursor.lockState = cursorAnterior;
        Cursor.visible = cursorVisibleAnterior;

        if (jugador != null) jugador.Reintentar();
        else Time.timeScale = 1f;
    }

    // Botón "Menú": vuelve al menú de inicio (si no existe en el build, cierra el juego).
    public void Salir()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (Application.CanStreamedLevelBeLoaded(escenaMenu))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(escenaMenu);
            return;
        }

        Debug.LogWarning("No encontré la escena '" + escenaMenu + "' en la lista de escenas del build. Cerrando el juego.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------- Ayudas ----------------

    void Ocultar()
    {
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        if (estatica != null) estatica.gameObject.SetActive(false);
        if (lineaApagado != null) lineaApagado.gameObject.SetActive(false);
        ApagarFranjas();
        SetCortinas(0f);

        if (contenido != null)
        {
            contenido.alpha = 0f;
            contenido.interactable = false;
            contenido.gameObject.SetActive(false);
        }
    }

    void Sonar(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, volumen);
    }

    void CrearTexturaEstatica()
    {
        const int tam = 128;
        texturaEstatica = new Texture2D(tam, tam, TextureFormat.RGBA32, false);
        texturaEstatica.filterMode = FilterMode.Point;
        texturaEstatica.wrapMode = TextureWrapMode.Repeat;

        Color32[] px = new Color32[tam * tam];
        for (int i = 0; i < px.Length; i++)
        {
            byte v = (byte)Random.Range(0, 256);
            px[i] = new Color32(v, v, v, 255);
        }
        texturaEstatica.SetPixels32(px);
        texturaEstatica.Apply();

        if (estatica != null) estatica.texture = texturaEstatica;
    }

    void MoverEstatica(float alfa)
    {
        if (estatica == null) return;
        estatica.uvRect = new Rect(Random.value, Random.value, 7f, 4f);
        Color c = estatica.color;
        c.a = alfa;
        estatica.color = c;
    }

    void MoverFranjas(float progreso)
    {
        if (franjasGlitch == null) return;
        foreach (Image f in franjasGlitch)
        {
            if (f == null) continue;
            bool visible = Random.value < 0.35f + progreso * 0.4f;
            f.gameObject.SetActive(visible);
            if (!visible) continue;

            RectTransform rt = f.rectTransform;
            float y = Random.value;
            float alto = Random.Range(0.005f, 0.05f);
            rt.anchorMin = new Vector2(0f, y);
            rt.anchorMax = new Vector2(1f, Mathf.Min(1f, y + alto));
            rt.offsetMin = new Vector2(Random.Range(-80f, 80f), 0f);
            rt.offsetMax = new Vector2(rt.offsetMin.x, 0f);

            float gris = Random.value;
            f.color = new Color(gris * 0.6f, gris, gris, Random.Range(0.25f, 0.7f));
        }
    }

    void ApagarFranjas()
    {
        if (franjasGlitch == null) return;
        foreach (Image f in franjasGlitch)
            if (f != null) f.gameObject.SetActive(false);
    }

    // 0 = abiertas, 1 = cerradas dejando solo una línea en el centro
    void SetCortinas(float p, bool sinHueco = false)
    {
        float hueco = sinHueco ? 0f : 0.002f;
        float centro = Mathf.Lerp(0f, 0.5f - hueco, p);

        if (cortinaAbajo != null)
        {
            cortinaAbajo.gameObject.SetActive(p > 0f);
            cortinaAbajo.anchorMin = Vector2.zero;
            cortinaAbajo.anchorMax = new Vector2(1f, centro);
            cortinaAbajo.offsetMin = cortinaAbajo.offsetMax = Vector2.zero;
        }
        if (cortinaArriba != null)
        {
            cortinaArriba.gameObject.SetActive(p > 0f);
            cortinaArriba.anchorMin = new Vector2(0f, 1f - centro);
            cortinaArriba.anchorMax = Vector2.one;
            cortinaArriba.offsetMin = cortinaArriba.offsetMax = Vector2.zero;
        }
    }

    // ancho: 1 = toda la pantalla, ~0 = punto
    void SetLinea(float ancho, float alfa)
    {
        if (lineaApagado == null) return;
        lineaApagado.anchorMin = new Vector2(0.5f - ancho * 0.5f, 0.5f);
        lineaApagado.anchorMax = new Vector2(0.5f + ancho * 0.5f, 0.5f);
        lineaApagado.sizeDelta = new Vector2(Mathf.Lerp(8f, 0f, ancho), 6f);   // el punto no desaparece del todo
        Image img = lineaApagado.GetComponent<Image>();
        if (img != null) { Color c = img.color; c.a = alfa; img.color = c; }
    }

    static float Suave(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
