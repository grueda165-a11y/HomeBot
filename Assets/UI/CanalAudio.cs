using UnityEngine;

// Opcional: ponlo en un AudioSource para decir si es Música o Efectos.
// Sin este componente: los sonidos que se repiten (loop) cuentan como música y el resto como efectos.
[RequireComponent(typeof(AudioSource))]
public class CanalAudio : MonoBehaviour
{
    public enum Tipo { Musica, Efectos }
    public Tipo canal = Tipo.Efectos;
}
