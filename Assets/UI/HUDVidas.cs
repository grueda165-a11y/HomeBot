using UnityEngine;
using UnityEngine.UI;

// Muestra las vidas del jugador con caritas del robot.
// Todas las caritas visibles comparten color:
// 3 vidas = verde, 2 vidas = amarillo, 1 vida = rojo.
public class HUDVidas : MonoBehaviour
{
    [Header("Referencias")]
    public VidaJugador jugador;

    [Tooltip("Imagen de la pantalla (capa que se tiñe) de cada carita, en orden.")]
    public Image[] pantallas;

    [Tooltip("Objeto raíz de cada carita, en el mismo orden. Se oculta al perder esa vida.")]
    public GameObject[] caritas;

    [Tooltip("Efecto eléctrico que se lanza al perder una vida (opcional).")]
    public EfectoElectrico efecto;

    [Header("Colores")]
    public Color colorTresVidas = new Color(0.24f, 0.86f, 0.35f);
    public Color colorDosVidas = new Color(0.96f, 0.82f, 0.16f);
    public Color colorUnaVida = new Color(0.90f, 0.22f, 0.23f);

    private int vidasMostradas = -1;

    void Start()
    {
        if (jugador == null)
            jugador = FindFirstObjectByType<VidaJugador>();
    }

    void Update()
    {
        if (jugador == null) return;

        if (jugador.vidasActuales != vidasMostradas)
            Actualizar(jugador.vidasActuales);
    }

    void Actualizar(int vidas)
    {
        if (vidasMostradas != -1 && vidas < vidasMostradas && efecto != null)
            efecto.Disparar();

        vidasMostradas = vidas;
        Color color = ColorPara(vidas);

        for (int i = 0; i < caritas.Length; i++)
        {
            if (caritas[i] != null)
                caritas[i].SetActive(i < vidas);
        }

        foreach (Image pantalla in pantallas)
        {
            if (pantalla != null)
                pantalla.color = color;
        }
    }

    Color ColorPara(int vidas)
    {
        if (vidas >= 3) return colorTresVidas;
        if (vidas == 2) return colorDosVidas;
        return colorUnaVida;
    }
}
