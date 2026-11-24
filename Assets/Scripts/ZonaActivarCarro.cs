using UnityEngine;

public class ZonaActivarCarro : MonoBehaviour
{
    public CarroPerseguidor carro;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && carro != null)
        {
            carro.Activar();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && carro != null)
        {
            carro.Desactivar();
        }
    }
}