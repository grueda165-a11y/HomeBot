using UnityEngine;

// Animaciones y sonidos del robot cuando recibe daño o muere.
//  - Muerte: el juego se detiene un momento (lo controla VidaJugador.pausaAlMorir), se ve la
//    animación de muerte y luego reaparece en el checkpoint.
//  - Daño sin morir: se ve la animación de descarga eléctrica.
[RequireComponent(typeof(VidaJugador))]
public class FeedbackRobot : MonoBehaviour
{
    public Animator animator;               // opcional: si lo dejas vacío lo busca en los hijos

    [Header("Sonidos (opcional)")]
    public AudioClip sonidoMuerte;          // por ejemplo Assets/Sonidos/SonidoDaño
    public AudioClip sonidoDano;
    [Range(0f, 1f)] public float volumen = 1f;

    private VidaJugador vida;
    private AudioSource fuente;

    void Awake()
    {
        vida = GetComponent<VidaJugador>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
    }

    void OnEnable()
    {
        VidaJugador.Muriendo += AlMorir;
        VidaJugador.RecibioDano += AlRecibirDano;
        VidaJugador.JugadorMurio += AlReaparecer;
    }

    void OnDisable()
    {
        VidaJugador.Muriendo -= AlMorir;
        VidaJugador.RecibioDano -= AlRecibirDano;
        VidaJugador.JugadorMurio -= AlReaparecer;
    }

    void AlMorir()
    {
        if (animator != null)
        {
            // Mientras el juego está en pausa (timeScale = 0) la animación debe seguir moviéndose
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            Disparar("muere");
        }
        Sonar(sonidoMuerte);
    }

    void AlRecibirDano()
    {
        Disparar("electro");
        Sonar(sonidoDano);
    }

    void AlReaparecer()
    {
        if (animator == null) return;

        // Vuelve a la animación de reposo (la de muerte no tiene salida)
        animator.updateMode = AnimatorUpdateMode.Normal;
        animator.Rebind();
        animator.Update(0f);
    }

    // Durante la pausa de la muerte, evita que un salto interrumpa la animación
    void LateUpdate()
    {
        if (animator != null && vida != null && vida.EnSecuenciaMuerte)
        {
            Quitar("salta");
            Quitar("electro");
        }
    }

    void Disparar(string nombre)
    {
        if (animator == null || !Existe(nombre)) return;
        animator.SetTrigger(nombre);
    }

    void Quitar(string nombre)
    {
        if (Existe(nombre)) animator.ResetTrigger(nombre);
    }

    bool Existe(string nombre)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == nombre) return true;
        return false;
    }

    void Sonar(AudioClip clip)
    {
        if (clip != null && fuente != null) fuente.PlayOneShot(clip, volumen);
    }
}