using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Pantalla de victoria: "¡Has vuelto a la habitación de tu dueño! Felicidades".
// Aparece al recoger la última batería (la decide ControlVictoria), pausa el juego
// y el botón MENÚ vuelve al menú de inicio.
[RequireComponent(typeof(CanvasGroup))]
public class PantallaVictoria : MonoBehaviour
{
    public Button botonMenu;
    public string escenaMenu = "MenuInicio";
    public float duracionAparecer = 0.8f;

    public bool Mostrando { get; private set; }

    private CanvasGroup grupo;
    private RectTransform contenido;
    private AudioSource fuente;

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = grupo.blocksRaycasts = false;
    }

    void Start()
    {
        // Botón conectado en Start (las referencias ya están puestas)
        if (botonMenu != null) botonMenu.onClick.AddListener(IrAlMenu);
        contenido = transform.childCount > 0 ? (RectTransform)transform.GetChild(0) : null;
    }

    public void Mostrar()
    {
        if (Mostrando) return;
        Mostrando = true;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // El menú de pausa no se puede abrir encima de la victoria
        foreach (MenuPausa p in FindObjectsByType<MenuPausa>(FindObjectsSortMode.None))
        {
            if (p.botonPausaHUD != null) p.botonPausaHUD.gameObject.SetActive(false);
            p.enabled = false;
        }

        grupo.interactable = grupo.blocksRaycasts = true;

        fuente = gameObject.AddComponent<AudioSource>();
        fuente.ignoreListenerPause = true;
        fuente.playOnAwake = false;
        fuente.volume = AjustesAudio.Efectos;
        fuente.PlayOneShot(SonidosRobot.Victoria());

        if (botonMenu != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(botonMenu.gameObject);
    }

    void Update()
    {
        if (!Mostrando) return;
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, 1f, Time.unscaledDeltaTime / duracionAparecer);
        // Pequeño "zoom" al aparecer
        if (contenido != null) contenido.localScale = Vector3.one * Mathf.Lerp(1.06f, 1f, grupo.alpha);
    }

    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (Application.CanStreamedLevelBeLoaded(escenaMenu))
            SceneManager.LoadScene(escenaMenu);
        else
            Debug.LogWarning("No encontré la escena '" + escenaMenu + "' en la lista de escenas del build.");
    }

    void OnDestroy()
    {
        if (Mostrando) Time.timeScale = 1f;
    }
}
