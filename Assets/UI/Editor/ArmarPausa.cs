using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menú: HomeBot > Armar menú de pausa
// No es obligatorio: al dar Play, el botón de pausa y el menú se crean solos (ConstructorPausa).
// Esta opción sirve para dejarlos guardados en la escena y poder verlos/ajustarlos en el editor.
public static class ArmarPausa
{
    [MenuItem("HomeBot/Armar menú de pausa")]
    public static void ArmarDesdeMenu()
    {
        if (!HerramientasHomeBot.FueraDePlay("Menú de pausa")) return;

        // Solo en escenas de juego: nunca en el menú de inicio
        if (Object.FindFirstObjectByType<MenuInicio>() != null || Object.FindFirstObjectByType<VidaJugador>() == null)
        {
            EditorUtility.DisplayDialog("Menú de pausa",
                "Abre la escena del juego (HomeBotEntrega) antes de usar esta opción.\n\nEl menú de pausa no va en el menú de inicio.", "OK");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Menú de pausa", "No hay un Canvas en la escena.", "OK");
            return;
        }
        if (Construir(canvas.transform, Object.FindFirstObjectByType<VidaJugador>()))
        {
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            Debug.Log("Menú de pausa armado. Guarda la escena (Ctrl+S).");
        }
    }

    public static bool Construir(Transform canvas, VidaJugador jugador)
    {
        MenuPausa menu = ConstructorPausa.Construir(canvas, jugador);
        if (menu == null)
        {
            EditorUtility.DisplayDialog("Menú de pausa", "Faltan imágenes en Assets/UI/Resources/HomeBotUI.", "OK");
            return false;
        }
        Undo.RegisterCreatedObjectUndo(menu.gameObject, "Menú de pausa");
        if (menu.botonPausaHUD != null) Undo.RegisterCreatedObjectUndo(menu.botonPausaHUD.gameObject, "Botón de pausa");
        return true;
    }
}
