using UnityEditor;
using UnityEngine;

// Menú: HomeBot > Marcar batería final
// Selecciona una batería en la Hierarchy (un objeto con ObjetoSumarVida) y usa esta opción:
// al recogerla en el juego aparecerá la pantalla de victoria.
public static class MarcarBateriaFinal
{
    [MenuItem("HomeBot/Marcar batería final")]
    static void Marcar()
    {
        if (!HerramientasHomeBot.FueraDePlay("Batería final")) return;

        int marcadas = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go.GetComponent<ObjetoSumarVida>() == null)
            {
                Debug.LogWarning("'" + go.name + "' no es una batería (no tiene ObjetoSumarVida).");
                continue;
            }
            if (go.GetComponent<BateriaFinal>() == null) Undo.AddComponent<BateriaFinal>(go);
            marcadas++;
        }

        if (marcadas == 0)
            EditorUtility.DisplayDialog("Batería final", "Selecciona una batería (un objeto con ObjetoSumarVida) en la Hierarchy.", "OK");
        else
            Debug.Log("Batería final marcada. Guarda la escena (Ctrl+S).");
    }

    [MenuItem("HomeBot/Marcar batería final", true)]
    static bool Validar() => !EditorApplication.isPlaying;
}
