using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class ZonaReproducirAnimacion : MonoBehaviour
{
    [Header("Animación")]
    public Animator animator;      
    public AnimationClip clip;       

    [Header("Opciones")]
    public bool soloUnaVez = true;          
    public bool reiniciarAlMorir = true;     

    private PlayableGraph graph;
    private AnimationClipPlayable clipPlayable;
    private bool yaActivada = false;

    void Awake()
    {
        if (animator == null || clip == null)
        {
            Debug.LogWarning("Falta asignar el Animator o el Clip en " + name);
            return;
        }

        graph = PlayableGraph.Create("Animacion_" + name);
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        AnimationPlayableOutput salida = AnimationPlayableOutput.Create(graph, "Salida", animator);
        clipPlayable = AnimationClipPlayable.Create(graph, clip);
        salida.SetSourcePlayable(clipPlayable);

        clipPlayable.SetTime(0);
        clipPlayable.Pause();
        graph.Play();
        graph.Evaluate(0f);
    }

    void OnEnable()
    {
        VidaJugador.JugadorMurio += Reiniciar;
    }

    void OnDisable()
    {
        VidaJugador.JugadorMurio -= Reiniciar;
    }

    void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!graph.IsValid()) return;
        if (soloUnaVez && yaActivada) return;

        yaActivada = true;

        clipPlayable.SetTime(0);
        clipPlayable.Play();
    }

    void Reiniciar()
    {
        if (!reiniciarAlMorir || !graph.IsValid()) return;

        yaActivada = false;

        clipPlayable.Pause();
        clipPlayable.SetTime(0);
        graph.Evaluate(0f);
    }
}