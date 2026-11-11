using UnityEngine;
using UnityEngine.UI;

// Barra del Canvas que se va vaciando con el tiempo del jugador.
// Cuando el jugador toca una batería, el tiempo se reinicia y la barra vuelve a llenarse.
public class BarraTiempo : MonoBehaviour
{
    [Header("Referencias")]
    public VidaJugador jugador;

    [Tooltip("Imagen 'fill' de la barra (tipo Filled, horizontal).")]
    public Image relleno;

    [Tooltip("Efecto eléctrico que se lanza al recargar con una batería (opcional).")]
    public EfectoElectrico efecto;

    private float objetivoAnterior = 1f;

    [Header("Animación")]
    [Tooltip("Qué tan rápido sube la barra al recargar con una batería.")]
    public float velocidadRecarga = 4f;

    void Start()
    {
        if (jugador == null)
            jugador = FindFirstObjectByType<VidaJugador>();

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

        // Subió de golpe: batería recogida (o reaparición) -> chispazo.
        if (objetivo - objetivoAnterior > 0.05f && efecto != null)
            efecto.Disparar();
        objetivoAnterior = objetivo;

        // Baja de forma continua; cuando sube (batería o reaparición) lo hace con una animación corta.
        if (objetivo < relleno.fillAmount)
            relleno.fillAmount = objetivo;
        else
            relleno.fillAmount = Mathf.MoveTowards(relleno.fillAmount, objetivo, velocidadRecarga * Time.deltaTime);
    }
}
