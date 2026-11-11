using UnityEngine;

public class Orbitar : MonoBehaviour
{
    public Transform objetivo;      // objeto alrededor del cual orbita
    public float velocidad = 90f;   // grados por segundo
    public Vector3 eje = Vector3.up;

    void Update()
    {
        transform.RotateAround(objetivo.position, eje, velocidad * Time.deltaTime);
    }
}