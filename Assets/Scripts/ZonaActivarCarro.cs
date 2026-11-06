using UnityEngine;

public class ZonaActivarCarro : MonoBehaviour
{
    public CarroPerseguidor carro; // arrastra aquí el carro

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && carro != null)
        {
            carro.Activar();
        }
    }
}