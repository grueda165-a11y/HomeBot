using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Reaparición")]
    public Vector3 offsetRespawn = new Vector3(0f, 1f, 0f); 

    [Header("Aspecto (opcional)")]
    public Color colorInactivo = Color.red;
    public Color colorActivo = Color.green;

    private Renderer _renderer;
    private bool activado = false;

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
    }

    void OnEnable()
    {
        VidaJugador.ReinicioCompleto += Desactivar;
    }

    void OnDisable()
    {
        VidaJugador.ReinicioCompleto -= Desactivar;
    }

    void Start()
    {
        AplicarColor();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activado) return;

        if (other.CompareTag("Player"))
        {
            VidaJugador vida = other.GetComponent<VidaJugador>();
            if (vida != null)
            {
                vida.GuardarCheckpoint(transform.position + offsetRespawn);
                activado = true;
                AplicarColor();
            }
        }
    }

    void Desactivar()
    {
        activado = false;
        AplicarColor();
    }

    void AplicarColor()
    {
        if (_renderer != null)
            _renderer.material.color = activado ? colorActivo : colorInactivo;
    }
}