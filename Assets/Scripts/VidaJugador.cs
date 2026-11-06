using UnityEngine;
using TMPro;

public class VidaJugador : MonoBehaviour
{
    [Header("Configuración de Vida (tiempo)")]
    public float vidaMaxima = 120f;
    public float vidaActual;
    public float perdidaPorSegundo = 1f;

    [Header("Configuración de Vidas")]
    public int vidasMaximas = 3;
    public int vidasActuales;

    [Header("Aparición inicial")]
    public Vector3 coordenadasIniciales = new Vector3(13.27f, 22.71f, -9.2f);

    // Avisos para otros objetos
    public static event System.Action JugadorMurio;      
    public static event System.Action ReinicioCompleto;  

    [Header("Interfaz")]
    public TMP_Text textoVida;    
    public TMP_Text textoVidas;   

    private CharacterController _characterController;
    private Rigidbody _rigidbody;

    private Vector3 posicionInicial;   
    private Vector3 posicionRespawn;   

    void Start()
    {
        vidaActual = vidaMaxima;
        vidasActuales = vidasMaximas;

        _characterController = GetComponent<CharacterController>();
        _rigidbody = GetComponent<Rigidbody>();

        posicionInicial = coordenadasIniciales;
        posicionRespawn = posicionInicial;

        Teletransportar(posicionInicial);

        ActualizarTextos();
    }

    void Update()
    {
        vidaActual -= perdidaPorSegundo * Time.deltaTime;

        if (vidaActual <= 0f)
        {
            Morir();
        }

        ActualizarTextos();
    }

    public void TomarDano(float cantidadDano)
    {
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

    
    public void GuardarCheckpoint(Vector3 nuevaPosicion)
    {
        posicionRespawn = nuevaPosicion;
    }

    void ActualizarTextos()
    {
        if (textoVida != null)
            textoVida.text = "Vida: " + Mathf.CeilToInt(Mathf.Max(vidaActual, 0f));

        if (textoVidas != null)
            textoVidas.text = "Vidas: " + vidasActuales;
    }

    void Morir()
    {
        vidasActuales--;

        bool sinVidas = vidasActuales <= 0;

        if (sinVidas)
        {
            
            vidasActuales = vidasMaximas;
            posicionRespawn = posicionInicial;
            Debug.Log("Sin vidas. Volviendo al checkpoint inicial.");
        }
        else
        {
            Debug.Log("El jugador ha muerto. Vidas restantes: " + vidasActuales);
        }

        Teletransportar(posicionRespawn);

        vidaActual = vidaMaxima;

        JugadorMurio?.Invoke();                    
        if (sinVidas) ReinicioCompleto?.Invoke();  

        ActualizarTextos();
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