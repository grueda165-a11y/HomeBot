using UnityEngine;
using UnityEngine.EventSystems;

// Hace que un botón del menú se sienta vivo:
// crece al pasar el mouse (o al seleccionarlo con teclado), se hunde al presionarlo
// y suena un "clic" de juguete. Funciona aunque el juego esté en pausa.
public class BotonMenu : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler, ISubmitHandler, IPointerClickHandler
{
    public float escalaHover = 1.08f;
    public float escalaPresionado = 0.95f;
    public float velocidad = 14f;

    [Tooltip("Respira suavemente cuando está seleccionado (para el botón principal).")]
    public bool latirSeleccionado = true;

    [Header("Sonidos (vacío = se generan por código)")]
    public AudioClip sonidoHover;
    public AudioClip sonidoClic;

    private Vector3 escalaBase;
    private bool encima, presionado, seleccionado;
    private static AudioSource fuente;

    void Awake()
    {
        escalaBase = transform.localScale;
        if (sonidoHover == null) sonidoHover = SonidosRobot.Tic();
        if (sonidoClic == null) sonidoClic = SonidosRobot.Clic();
    }

    void OnDisable()
    {
        encima = presionado = seleccionado = false;
        transform.localScale = escalaBase;
    }

    void Update()
    {
        float objetivo = 1f;
        if (encima || seleccionado) objetivo = escalaHover;
        if (presionado) objetivo = escalaPresionado;
        if (seleccionado && !encima && !presionado && latirSeleccionado)
            objetivo += Mathf.Sin(Time.unscaledTime * 4f) * 0.015f;

        float actual = transform.localScale.x / Mathf.Max(0.0001f, escalaBase.x);
        actual = Mathf.Lerp(actual, objetivo, 1f - Mathf.Exp(-velocidad * Time.unscaledDeltaTime));
        transform.localScale = escalaBase * actual;
    }

    public void OnPointerEnter(PointerEventData e) { encima = true; Sonar(sonidoHover, 0.5f); }
    public void OnPointerExit(PointerEventData e) { encima = false; presionado = false; }
    public void OnPointerDown(PointerEventData e) { presionado = true; }
    public void OnPointerUp(PointerEventData e) { presionado = false; }
    public void OnPointerClick(PointerEventData e) { Sonar(sonidoClic, 1f); }
    public void OnSelect(BaseEventData e) { seleccionado = true; }
    public void OnDeselect(BaseEventData e) { seleccionado = false; }
    public void OnSubmit(BaseEventData e) { Sonar(sonidoClic, 1f); }

    void Sonar(AudioClip clip, float vol)
    {
        if (clip == null) return;
        if (fuente == null)
        {
            GameObject go = new GameObject("SonidosBotones");
            DontDestroyOnLoad(go);
            fuente = go.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
            fuente.ignoreListenerPause = true;
            AjustesAudio.Aplicar();   // le pone el volumen de "Efectos"
        }
        fuente.PlayOneShot(clip, vol);
    }
}
