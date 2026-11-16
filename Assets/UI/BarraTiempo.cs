using UnityEngine;
using UnityEngine.UI;

// Pila de juguete del HUD: se va vaciando con el tiempo del jugador (VidaJugador.vidaMaxima, 90 s).
//  - Verde -> amarillo a medida que baja.
//  - Por debajo del 25 %: se pone roja y parpadea (pulsación).
//  - Al recoger una batería: se llena, chispazo eléctrico y rebote ("scale punch").
public class BarraTiempo : MonoBehaviour
{
    [Header("Referencias")]
    public VidaJugador jugador;

    [Tooltip("Imagen de la carga dentro de la pila (tipo Filled, horizontal).")]
    public Image relleno;

    [Tooltip("Lo que rebota al recargar (normalmente la pila completa). Si está vacío usa este objeto.")]
    public RectTransform objetoRebote;

    [Tooltip("Efecto eléctrico que se lanza al recargar con una batería (opcional).")]
    public EfectoElectrico efecto;

    [Header("Colores")]
    public Color colorLlena = new Color(0.28f, 0.85f, 0.35f);
    public Color colorMedia = new Color(0.98f, 0.80f, 0.15f);
    public Color colorAlerta = new Color(0.92f, 0.20f, 0.22f);

    [Header("Alerta de batería baja")]
    [Range(0f, 1f)] public float umbralAlerta = 0.25f;
    [Tooltip("Pulsaciones por segundo del parpadeo.")]
    public float velocidadParpadeo = 3.5f;
    [Range(0f, 1f)] public float brilloMinimo = 0.35f;
    [Tooltip("Cuánto crece la pila con cada pulsación en alerta.")]
    public float pulsoEscala = 0.04f;

    [Header("Rebote al recargar")]
    public float fuerzaRebote = 0.3f;
    public float duracionRebote = 0.45f;

    [Header("Animación")]
    [Tooltip("Qué tan rápido sube la carga al recargar.")]
    public float velocidadRecarga = 4f;

    private float objetivoAnterior = 1f;
    private int vidasAnteriores = -1;
    private float inicioRebote = -10f;
    private Vector3 escalaBase = Vector3.one;

    void Start()
    {
        if (jugador == null)
            jugador = FindFirstObjectByType<VidaJugador>();
        if (objetoRebote == null)
            objetoRebote = (RectTransform)transform;
        escalaBase = objetoRebote.localScale;

        if (relleno != null)
        {
            relleno.type = Image.Type.Filled;
            relleno.fillMethod = Image.FillMethod.Horizontal;
            relleno.fillOrigin = (int)Image.OriginHorizontal.Left;
            relleno.fillAmount = 1f;
        }
    }

    void Update()
    {
        if (jugador == null || relleno == null) return;

        float objetivo = Mathf.Clamp01(jugador.vidaActual / jugador.vidaMaxima);

        // Subió de golpe sin perder vidas = recogió una batería.
        bool subio = objetivo - objetivoAnterior > 0.05f;
        bool murio = vidasAnteriores != -1 && jugador.vidasActuales != vidasAnteriores;
        if (subio && !murio && !jugador.EnPantallaMuerte)
            Recargado();

        objetivoAnterior = objetivo;
        vidasAnteriores = jugador.vidasActuales;

        // Baja de forma continua; al subir lo hace con una animación corta.
        if (objetivo < relleno.fillAmount)
            relleno.fillAmount = objetivo;
        else
            relleno.fillAmount = Mathf.MoveTowards(relleno.fillAmount, objetivo, velocidadRecarga * Time.unscaledDeltaTime);

        ActualizarColorYEscala(relleno.fillAmount);
    }

    void Recargado()
    {
        inicioRebote = Time.unscaledTime;
        if (efecto != null) efecto.Disparar();
    }

    void ActualizarColorYEscala(float carga)
    {
        float escala = 1f;

        if (carga < umbralAlerta)
        {
            // Rojo que parpadea
            float onda = (Mathf.Sin(Time.unscaledTime * velocidadParpadeo * Mathf.PI * 2f) + 1f) * 0.5f;
            Color c = colorAlerta * Mathf.Lerp(brilloMinimo, 1f, onda);
            c.a = 1f;
            relleno.color = c;
            escala += pulsoEscala * onda;
        }
        else
        {
            // De verde (llena) a amarillo (cerca del 25 %)
            float t = Mathf.InverseLerp(1f, umbralAlerta, carga);
            relleno.color = Color.Lerp(colorLlena, colorMedia, t);
        }

        // Rebote al recargar: crece y vuelve con un pequeño resorte
        float tr = (Time.unscaledTime - inicioRebote) / duracionRebote;
        if (tr >= 0f && tr < 1f)
            escala += fuerzaRebote * Mathf.Sin(tr * Mathf.PI * 3f) * (1f - tr);

        objetoRebote.localScale = escalaBase * escala;
    }
}
