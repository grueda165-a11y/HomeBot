using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú de pausa (Esc o el botón de pausa del HUD):
//  - CONTINUAR: vuelve al juego (también con Esc).
//  - REINICIAR: empieza de nuevo desde el inicio con las 3 vidas.
//  - AJUSTES: Volumen y Efectos (los mismos del menú de inicio).
//  - MENÚ: vuelve al menú de inicio.
// Mientras está abierto, el juego queda congelado y los sonidos del juego en pausa.
[RequireComponent(typeof(CanvasGroup))]
public class MenuPausa : MonoBehaviour
{
    [Header("Referencias")]
    public VidaJugador jugador;
    public string escenaMenu = "MenuInicio";

    [Tooltip("Botón de pausa que se ve en el HUD durante el juego.")]
    public Button botonPausaHUD;

    [Header("Panel de pausa")]
    public CanvasGroup panelPausa;
    public Button botonContinuar;
    public Button botonReiniciar;
    public Button botonAjustes;
    public Button botonMenu;

    [Header("Panel de ajustes")]
    public CanvasGroup panelAjustes;
    public Slider sliderVolumen;
    public Slider sliderEfectos;
    public Button botonVolver;

    [Header("Tecla")]
    [Tooltip("Abre y cierra el menú de pausa.")]
    public KeyCode tecla = KeyCode.Escape;

    [Header("Animación")]
    public float velocidad = 7f;

    public bool Abierto { get; private set; }

    private CanvasGroup grupo;
    private bool enAjustes;
    private CursorLockMode cursorAnterior;
    private bool cursorVisibleAnterior;
    private readonly List<Behaviour> controlesApagados = new List<Behaviour>();

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();

        Mostrar(grupo, false, true);
        Mostrar(panelPausa, true, true);
        Mostrar(panelAjustes, false, true);
    }

    void Start()
    {
        // La pausa solo existe en escenas de juego: si quedó puesta en el menú de inicio
        // (o en una escena sin jugador), se quita sola junto con su botón.
        if (!EsEscenaDeJuego())
        {
            if (botonPausaHUD != null) Destroy(botonPausaHUD.gameObject);
            Destroy(gameObject);
            return;
        }

        Conectar();
    }

    bool EsEscenaDeJuego()
    {
        foreach (GameObject raiz in gameObject.scene.GetRootGameObjects())
        {
            if (raiz.GetComponentInChildren<MenuInicio>(true) != null) return false;
            if (jugador == null) jugador = raiz.GetComponentInChildren<VidaJugador>(true);
        }
        return jugador != null;
    }

    // Se conectan los botones en Start (no en Awake) para que las referencias ya estén puestas.
    private bool conectado;
    void Conectar()
    {
        if (conectado) return;
        conectado = true;

        if (botonPausaHUD != null) botonPausaHUD.onClick.AddListener(() => { if (!Abierto && (jugador == null || !jugador.EnPantallaMuerte)) Abrir(); });
        if (botonContinuar != null) botonContinuar.onClick.AddListener(Continuar);
        if (botonReiniciar != null) botonReiniciar.onClick.AddListener(Reiniciar);
        if (botonAjustes != null) botonAjustes.onClick.AddListener(AbrirAjustes);
        if (botonMenu != null) botonMenu.onClick.AddListener(IrAlMenu);
        if (botonVolver != null) botonVolver.onClick.AddListener(CerrarAjustes);

        if (sliderVolumen != null)
        {
            sliderVolumen.value = AjustesAudio.Musica;   // antes de escuchar cambios: así la barra verde se actualiza
            sliderVolumen.onValueChanged.AddListener(v => AjustesAudio.Musica = v);
        }
        if (sliderEfectos != null)
        {
            sliderEfectos.value = AjustesAudio.Efectos;
            sliderEfectos.onValueChanged.AddListener(v => AjustesAudio.Efectos = v);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(tecla))
        {
            if (!Abierto)
            {
                if (jugador == null || !jugador.EnPantallaMuerte) Abrir();
            }
            else if (enAjustes) CerrarAjustes();
            else Continuar();
        }

        // El botón de pausa del HUD se esconde con el menú abierto o en la pantalla de muerte
        if (botonPausaHUD != null)
        {
            bool mostrar = !Abierto && (jugador == null || !jugador.EnPantallaMuerte);
            if (botonPausaHUD.gameObject.activeSelf != mostrar) botonPausaHUD.gameObject.SetActive(mostrar);
        }

        float dt = Time.unscaledDeltaTime;
        Animar(grupo, Abierto, dt, null);
        Animar(panelPausa, Abierto && !enAjustes, dt, panelPausa != null ? panelPausa.transform : null);
        Animar(panelAjustes, Abierto && enAjustes, dt, panelAjustes != null ? panelAjustes.transform : null);
    }

    // ---------- Acciones ----------

    public void Abrir()
    {
        Abierto = true;
        enAjustes = false;

        Time.timeScale = 0f;
        AudioListener.pause = true;   // los botones del menú suenan igual (ignoran la pausa)

        cursorAnterior = Cursor.lockState;
        cursorVisibleAnterior = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // El robot no se mueve ni salta mientras está en pausa
        controlesApagados.Clear();
        foreach (Movimiento_Player m in FindObjectsByType<Movimiento_Player>(FindObjectsSortMode.None))
            if (m.enabled) { m.enabled = false; controlesApagados.Add(m); }

        Mostrar(grupo, true, false);
        Mostrar(panelPausa, true, false);
        Mostrar(panelAjustes, false, false);
        Seleccionar(botonContinuar);
    }

    public void Continuar()
    {
        if (!Abierto) return;
        Abierto = false;
        enAjustes = false;

        Mostrar(grupo, false, false);
        Reanudar();

        Cursor.lockState = cursorAnterior;
        Cursor.visible = cursorVisibleAnterior;
    }

    public void Reiniciar()
    {
        Continuar();
        if (jugador != null) jugador.Reintentar();
    }

    public void AbrirAjustes()
    {
        enAjustes = true;
        Mostrar(panelPausa, false, false);
        Mostrar(panelAjustes, true, false);
        Seleccionar(sliderVolumen);
    }

    public void CerrarAjustes()
    {
        enAjustes = false;
        Mostrar(panelAjustes, false, false);
        Mostrar(panelPausa, true, false);
        Seleccionar(botonAjustes);
    }

    public void IrAlMenu()
    {
        Reanudar();
        if (Application.CanStreamedLevelBeLoaded(escenaMenu))
            SceneManager.LoadScene(escenaMenu);
        else
            Debug.LogWarning("No encontré la escena '" + escenaMenu + "' en la lista de escenas del build.");
    }

    // ---------- Ayudas ----------

    void Reanudar()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        foreach (Behaviour b in controlesApagados) if (b != null) b.enabled = true;
        controlesApagados.Clear();
    }

    void OnDestroy()
    {
        if (Abierto) Reanudar();
    }

    void Animar(CanvasGroup g, bool visible, float dt, Transform panel)
    {
        if (g == null) return;
        g.alpha = Mathf.MoveTowards(g.alpha, visible ? 1f : 0f, velocidad * dt);
        if (panel != null) panel.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, g.alpha);
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
