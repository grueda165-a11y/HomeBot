using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SonidoScript : MonoBehaviour
{
    [Header("Sonido")]
    public AudioClip sonido;                 
    [Range(0f, 1f)] public float volumen = 1f;
    public float retraso = 0f;              
    public bool repetirEnBucle = false;      
    public bool sonido3D = false;            

    [Header("Opciones")]
    public bool soloUnaVez = true;           
    public bool detenerAlSalir = false;      
    public bool reiniciarAlMorir = true;     

    private AudioSource fuente;
    private bool yaActivada = false;

    void Awake()
    {
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = repetirEnBucle;
        fuente.spatialBlend = sonido3D ? 1f : 0f;
        fuente.clip = sonido;
        fuente.volume = volumen;
    }

    void OnEnable()
    {
        VidaJugador.JugadorMurio += Reiniciar;
    }

    void OnDisable()
    {
        VidaJugador.JugadorMurio -= Reiniciar;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (sonido == null) return;
        if (soloUnaVez && yaActivada) return;

        yaActivada = true;

        fuente.clip = sonido;
        fuente.volume = volumen;
        fuente.loop = repetirEnBucle;
        fuente.PlayDelayed(retraso);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (detenerAlSalir)
            fuente.Stop();
    }

    void Reiniciar()
    {
        fuente.Stop();

        if (reiniciarAlMorir)
            yaActivada = false;
    }
}