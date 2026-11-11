using UnityEngine;

public class VidaJugador : MonoBehaviour
{
    [Header("Configuración de Vida (tiempo)")]
    public float vidaMaxima = 120f;
    public float vidaActual;
    public float perdidaPorSegundo = 1f;

    [Header("Configuración de Vidas")]
    public int vidasMaximas = 3;
    public int vidasActuales;

    [Header("Intentos")]
    [Tooltip("Al pasar de este número, el contador de intentos vuelve a 1.")]
    public int intentosMaximos = 3;

    [Header("Aparición inicial")]
    public Vector3 coordenadasIniciales = new Vector3(13.27f, 22.71f, -9.2f);

    // Avisos para otros objetos
    public static event System.Action JugadorMurio;
    public static event System.Action ReinicioCompleto;
    public static event System.Action SinVidas;          // se perdieron las 3 vidas -> pantalla de muerte

    // Número de intento actual (1, 2, 3 y vuelve a 1).
    public static int Intentos { get; private set; }

    // true mientras se muestra la pantalla de muerte (el juego queda en pausa).
    public bool EnPantallaMuerte { get; private set; }

    private CharacterController _characterController;
    private Rigidbody _rigidbody;

    private Vector3 posicionInicial;
    private Vector3 posicionRespawn;

    // Pone el contador en 0 cada vez que se le da Play en el editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarContadorIntentos()
    {
        Intentos = 0;
    }

    void Start()
    {
        Time.timeScale = 1f;

        vidaActual = vidaMaxima;
        vidasActuales = vidasMaximas;
        Intentos = 1;

        _characterController = GetComponent<CharacterController>();
        _rigidbody = GetComponent<Rigidbody>();

        posicionInicial = coordenadasIniciales;
        posicionRespawn = posicionInicial;

        Teletransportar(posicionInicial);
    }

    void Update()
    {
        if (EnPantallaMuerte) return;

        vidaActual -= perdidaPorSegundo * Time.deltaTime;

        if (vidaActual <= 0f)
        {
            Morir();
        }
    }

    public void TomarDano(float cantidadDano)
    {
        if (EnPantallaMuerte) return;

        vidaActual -= cantidadDano;

        if (vidaActual <= 0f)
        {
            Morir();
        }
    }

    public void SumarVida(float segundos)
    {
        vidaActual = Mathf.Min(vidaActual + segundos, vidaMaxima);
    }

    // Llena por completo la barra de tiempo (lo usan las baterías).
    public void RecargarTiempo()
    {
        vidaActual = vidaMaxima;
    }

    public void GuardarCheckpoint(Vector3 nuevaPosicion)
    {
        posicionRespawn = nuevaPosicion;
    }

    void Morir()
    {
        vidasActuales--;

        if (vidasActuales <= 0)
        {
            vidasActuales = 0;
            vidaActual = 0f;

            // Sin pantalla de muerte en la escena: se reinicia directo como antes.
            if (SinVidas == null)
            {
                Reintentar();
                return;
            }

            EnPantallaMuerte = true;
            Time.timeScale = 0f;
            Debug.Log("Sin vidas. Mostrando pantalla de muerte.");
            SinVidas.Invoke();
            return;
        }

        Intentos = Intentos >= intentosMaximos ? 1 : Intentos + 1;
        Debug.Log("El jugador ha muerto. Vidas restantes: " + vidasActuales);

        Teletransportar(posicionRespawn);
        vidaActual = vidaMaxima;

        JugadorMurio?.Invoke();
    }

    // Botón "Reintentar" de la pantalla de muerte: vuelve al inicio con las 3 vidas.
    public void Reintentar()
    {
        EnPantallaMuerte = false;
        Time.timeScale = 1f;

        vidasActuales = vidasMaximas;
        vidaActual = vidaMaxima;
        Intentos = 1;
        posicionRespawn = posicionInicial;

        Teletransportar(posicionRespawn);
        Debug.Log("Reintentando desde el checkpoint inicial.");

        JugadorMurio?.Invoke();
        ReinicioCompleto?.Invoke();
    }

    void OnDestroy()
    {
        if (EnPantallaMuerte) Time.timeScale = 1f;
    }

    void Teletransportar(Vector3 destino)
    {

        if (_characterController != null) _characterController.enabled = false;
        if (_rigidbody != null) _rigidbody.isKinematic = true;


        transform.position = destino;


        if (_characterController != null) _characterController.enabled = true;
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = Vector3.zero;
        }
    }
}
