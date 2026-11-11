using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarroPerseguidor : MonoBehaviour
{
    [Header("Persecución")]
    public Transform jugador;          
    public float velocidad = 6f;
    public float velocidadGiro = 4f;

    [Header("Daño")]
    public float danoAlTocar = 9999f;

    private Rigidbody _rb;
    private bool activo = false;
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;

        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;
    }

    void Start()
    {
        if (jugador == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null) jugador = obj.transform;
        }
    }

    void OnEnable()
    {
        VidaJugador.JugadorMurio += Reiniciar;
    }

    void OnDisable()
    {
        VidaJugador.JugadorMurio -= Reiniciar;
    }

    public void Activar()
    {
        activo = true;
    }

    void Reiniciar()
    {
        activo = false;
        _rb.position = posicionInicial;
        _rb.rotation = rotacionInicial;
    }

    void FixedUpdate()
    {
        if (!activo || jugador == null) return;

        Vector3 direccion = jugador.position - transform.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude > 0.01f)
        {
            Quaternion objetivo = Quaternion.LookRotation(direccion.normalized, Vector3.up);
            Quaternion nuevaRot = Quaternion.Slerp(_rb.rotation, objetivo, velocidadGiro * Time.fixedDeltaTime);
            _rb.MoveRotation(nuevaRot);
        }

        _rb.MovePosition(_rb.position + transform.forward * velocidad * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!activo) return;

        if (other.CompareTag("Player"))
        {
            VidaJugador vida = other.GetComponent<VidaJugador>();
            if (vida != null)
            {
                vida.TomarDano(danoAlTocar);
            }
        }
    }
}