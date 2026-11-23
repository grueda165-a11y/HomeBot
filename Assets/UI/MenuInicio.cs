using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú de inicio de HomeBot Run!
//  - JUGAR: fundido a negro y carga la escena del juego.
//  - AJUSTES: abre el panel con Volumen (música) y Efectos, sobre el cuarto oscurecido.
//  - SALIR: cierra el juego.
public class MenuInicio : MonoBehaviour
{
    [Header("Escena del juego")]
    public string escenaJuego = "HomeBotEntrega";

    [Header("Pantalla principal")]
    public CanvasGroup principal;
    public Button botonJugar;
    public Button botonAjustes;
    public Button botonSalir;

    [Header("Ajustes")]
    public CanvasGroup ajustes;
    public Slider sliderVolumen;
    public Slider sliderEfectos;
    public Button botonVolver;

    [Header("Fundido")]
    public CanvasGroup fundido;
    public float duracionFundido = 0.6f;
    public float velocidadPanel = 6f;

    private bool ajustesAbiertos;
    private bool cargando;

    void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (botonJugar != null) botonJugar.onClick.AddListener(Jugar);
        if (botonAjustes != null) botonAjustes.onClick.AddListener(AbrirAjustes);
        if (botonSalir != null) botonSalir.onClick.AddListener(Salir);
        if (botonVolver != null) botonVolver.onClick.AddListener(CerrarAjustes);

        if (sliderVolumen != null)
        {
            sliderVolumen.SetValueWithoutNotify(AjustesAudio.Musica);
            sliderVolumen.onValueChanged.AddListener(v => AjustesAudio.Musica = v);
        }
        if (sliderEfectos != null)
        {
            sliderEfectos.SetValueWithoutNotify(AjustesAudio.Efectos);
            sliderEfectos.onValueChanged.AddListener(v => AjustesAudio.Efectos = v);
        }

        Mostrar(ajustes, false, true);
        Mostrar(principal, true, true);
        if (fundido != null) { fundido.alpha = 1f; fundido.blocksRaycasts = false; }
    }

    void Start()
    {
        Seleccionar(botonJugar);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // Aparece desde negro al abrir el menú
        if (fundido != null && !cargando)
            fundido.alpha = Mathf.MoveTowards(fundido.alpha, 0f, dt / duracionFundido);

        Animar(ajustes, ajustesAbiertos, dt);

        if (ajustesAbiertos && Input.GetKeyDown(KeyCode.Escape))
            CerrarAjustes();
    }

    // ---------- Botones ----------

    public void Jugar()
    {
        if (cargando) return;
        StartCoroutine(CargarJuego());
    }

    public void AbrirAjustes()
    {
        ajustesAbiertos = true;
        Mostrar(principal, false, false);
        Mostrar(ajustes, true, false);
        Seleccionar(sliderVolumen);
    }

    public void CerrarAjustes()
    {
        ajustesAbiertos = false;
        Mostrar(ajustes, false, false);
        Mostrar(principal, true, false);
        Seleccionar(botonAjustes);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Ayudas ----------

    IEnumerator CargarJuego()
    {
        cargando = true;
        Mostrar(principal, false, false);
        if (fundido != null) fundido.blocksRaycasts = true;

        yield return new WaitForSecondsRealtime(0.12f);   // deja sonar el clic

        AsyncOperation carga = null;
#if UNITY_EDITOR
        // En el editor: JUGAR abre la escena que estabas editando al darle Play (por ejemplo HomeBotTesting)
        string editada = UnityEditor.SessionState.GetString("HomeBot_EscenaParaJugar", "");
        if (!string.IsNullOrEmpty(editada))
            carga = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                editada, new LoadSceneParameters(LoadSceneMode.Single));
#endif
        if (carga == null) carga = SceneManager.LoadSceneAsync(escenaJuego);
        if (carga == null)
        {
            Debug.LogError("No se pudo cargar la escena '" + escenaJuego + "'. ¿Está en File > Build Profiles (Scene List)?");
            cargando = false;
            Mostrar(principal, true, false);
            yield break;
        }
        carga.allowSceneActivation = false;

        while (fundido != null && fundido.alpha < 1f)
        {
            fundido.alpha = Mathf.MoveTowards(fundido.alpha, 1f, Time.unscaledDeltaTime / duracionFundido);
            yield return null;
        }

        carga.allowSceneActivation = true;
    }

    void Animar(CanvasGroup g, bool visible, float dt)
    {
        if (g == null) return;
        g.alpha = Mathf.MoveTowards(g.alpha, visible ? 1f : 0f, velocidadPanel * dt);

        // El panel de ajustes "salta" un poquito al abrirse
        float escala = Mathf.Lerp(0.92f, 1f, g.alpha);
        Transform panel = g.transform.childCount > 1 ? g.transform.GetChild(1) : null;
        if (panel != null) panel.localScale = Vector3.one * escala;
    }

    static void Mostrar(CanvasGroup g, bool visible, bool inmediato)
    {
        if (g == null) return;
        g.interactable = visible;
        g.blocksRaycasts = visible;
        if (inmediato) g.alpha = visible ? 1f : 0f;
    }

    static void Seleccionar(Selectable s)
    {
        if (s != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(s.gameObject);
    }
}
