using UnityEngine;
using TMPro;

public class VidaJugador : MonoBehaviour
{
    [Header("Configuración de Vida")]
    public float vidaMaxima = 120f;
    public float vidaActual;
    public float perdidaPorSegundo = 1f;

    // Aviso que reciben los objetos cuando el jugador muere
    public static event System.Action JugadorMurio;

    [Header("Interfaz")]
    public TMP_Text textoVida; // arrastra aquí el texto del Canvas

    [Header("Configuración de Respawn")]
    public Vector3 posicionRespawn = new Vector3(0f, 0f, 0f);

    private CharacterController _characterController;
    private Rigidbody _rigidbody;

    void Start()
    {
        vidaActual = vidaMaxima;
        _characterController = GetComponent<CharacterController>();
        _rigidbody = GetComponent<Rigidbody>();
        ActualizarTexto();
    }

    void Update()
    {
        vidaActual -= perdidaPorSegundo * Time.deltaTime;

        if (vidaActual <= 0f)
        {
            Morir();
        }

        ActualizarTexto();
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

    void ActualizarTexto()
    {
        if (textoVida != null)
        {
            textoVida.text = "Vida: " + Mathf.CeilToInt(vidaActual);
        }
    }

    void Morir()
    {
        Debug.Log("El jugador ha muerto. Reapareciendo...");

        if (_characterController != null) _characterController.enabled = false;
        if (_rigidbody != null) _rigidbody.isKinematic = true;

        transform.position = posicionRespawn;

        if (_characterController != null) _characterController.enabled = true;
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = Vector3.zero;
        }

        vidaActual = vidaMaxima;

        JugadorMurio?.Invoke();
    }
}