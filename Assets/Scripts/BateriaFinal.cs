using UnityEngine;

// Marca una batería como "batería final": al recogerla se gana el juego (sale la pantalla de victoria).
// Ponla en la batería que quieras (junto a ObjetoSumarVida). Si hay varias marcadas, se gana al recogerlas todas.
// Si no hay ninguna marcada, se gana al recoger la última batería que hay dentro del cuarto del hermano.
[DisallowMultipleComponent]
public class BateriaFinal : MonoBehaviour
{
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, 1.2f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.6f, "BATERÍA FINAL");
#endif
    }
}
