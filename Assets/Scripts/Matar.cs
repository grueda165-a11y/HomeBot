using UnityEngine;

public class Matar : MonoBehaviour
{
    public float cantidadDano = 200f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            VidaJugador vida = other.GetComponent<VidaJugador>();
            if (vida != null)
            {
                vida.TomarDano(cantidadDano);
            }
        }
    }
}
