using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator))]
public class AnimacionClipBucle : MonoBehaviour
{
    [Tooltip("Arrastra aquí la animación que ya hiciste.")]
    public AnimationClip animacion;

    [Tooltip("1 = velocidad normal.")]
    public float velocidad = 1f;

    private PlayableGraph _graph;
    private AnimationClipPlayable _clipPlayable;

    void OnEnable()
    {
        if (animacion == null)
        {
            Debug.LogWarning("Falta asignar la animación en " + name);
            return;
        }

        _graph = PlayableGraph.Create("AnimBucle_" + name);
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        AnimationPlayableOutput salida =
            AnimationPlayableOutput.Create(_graph, "Anim", GetComponent<Animator>());

        _clipPlayable = AnimationClipPlayable.Create(_graph, animacion);
        _clipPlayable.SetSpeed(velocidad);

        salida.SetSourcePlayable(_clipPlayable);
        _graph.Play();
    }

    void Update()
    {
        // Asegura el bucle aunque el clip no tenga "Loop Time" activado
        if (_graph.IsValid() && _clipPlayable.GetTime() >= animacion.length)
            _clipPlayable.SetTime(0);
    }

    void OnDisable()
    {
        if (_graph.IsValid())
            _graph.Destroy();
    }
}