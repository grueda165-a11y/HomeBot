using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menú: HomeBot > Cámara PTZ > ...
// Cambia la cámara que seguía por detrás (CameraFollow) por la cámara PTZ fija.
public static class ConfigurarCamaraPTZ
{
    const string NombrePuntos = "PuntosCamaraPTZ";
    const string NombreReferencia = "ReferenciaControles_PTZ";

    [MenuItem("HomeBot/Cámara PTZ/Configurar")]
    public static void Configurar()
    {
        if (!HerramientasHomeBot.FueraDePlay("Cámara PTZ")) return;
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
        if (cam == null)
        {
            EditorUtility.DisplayDialog("Cámara PTZ", "No hay ninguna cámara en la escena.", "OK");
            return;
        }

        GameObject jugadorGO = GameObject.FindGameObjectWithTag("Player");
        if (jugadorGO == null)
        {
            EditorUtility.DisplayDialog("Cámara PTZ", "No encontré un objeto con el tag 'Player'.", "OK");
            return;
        }
        Transform jugador = jugadorGO.transform;

        Undo.SetCurrentGroupName("Configurar cámara PTZ");
        int grupo = Undo.GetCurrentGroup();

        // 1. Apagar la cámara que seguía por detrás
        foreach (CameraFollow viejo in Object.FindObjectsByType<CameraFollow>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(viejo, "Apagar CameraFollow");
            viejo.enabled = false;
        }

        // 2. Puntos de montaje
        GameObject puntosGO = GameObject.Find(NombrePuntos);
        if (puntosGO == null)
        {
            puntosGO = new GameObject(NombrePuntos);
            Undo.RegisterCreatedObjectUndo(puntosGO, NombrePuntos);
        }
        if (puntosGO.transform.childCount == 0)
        {
            // Primer punto: arriba y a un lado del inicio del jugador (no detrás)
            CrearPunto(puntosGO.transform, jugador.position + new Vector3(7f, 6f, -7f));
        }

        // 3. Referencia de controles
        GameObject refGO = GameObject.Find(NombreReferencia);
        if (refGO == null)
        {
            refGO = new GameObject(NombreReferencia);
            Undo.RegisterCreatedObjectUndo(refGO, NombreReferencia);
        }

        // 4. Componente PTZ
        CamaraPTZ ptz = cam.GetComponent<CamaraPTZ>();
        if (ptz == null) ptz = Undo.AddComponent<CamaraPTZ>(cam.gameObject);
        Undo.RecordObject(ptz, "Cámara PTZ");
        ptz.jugador = jugador;
        ptz.contenedorPuntos = puntosGO.transform;
        ptz.referenciaControles = refGO.transform;

        // 5. El movimiento del jugador usa la referencia estable en vez de la cámara directa
        Movimiento_Player mov = jugadorGO.GetComponent<Movimiento_Player>();
        if (mov != null)
        {
            Undo.RecordObject(mov, "Referencia de controles");
            mov.cameraTransform = refGO.transform;
        }

        Undo.CollapseUndoOperations(grupo);
        EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
        Selection.activeGameObject = puntosGO.transform.GetChild(0).gameObject;
        Debug.Log("Cámara PTZ lista. Mueve los puntos de 'PuntosCamaraPTZ' (o agrega más) y guarda la escena (Ctrl+S).");
    }

    [MenuItem("HomeBot/Cámara PTZ/Agregar punto donde estoy mirando")]
    public static void AgregarPuntoDesdeVista()
    {
        if (!HerramientasHomeBot.FueraDePlay("Cámara PTZ")) return;
        SceneView vista = SceneView.lastActiveSceneView;
        if (vista == null || vista.camera == null)
        {
            EditorUtility.DisplayDialog("Cámara PTZ", "Abre la ventana Scene para elegir dónde poner el punto.", "OK");
            return;
        }

        GameObject puntosGO = GameObject.Find(NombrePuntos);
        if (puntosGO == null)
        {
            EditorUtility.DisplayDialog("Cámara PTZ", "Primero usa HomeBot > Cámara PTZ > Configurar.", "OK");
            return;
        }

        GameObject punto = CrearPunto(puntosGO.transform, vista.camera.transform.position);
        Selection.activeGameObject = punto;
        EditorSceneManager.MarkSceneDirty(punto.scene);
    }

    static GameObject CrearPunto(Transform padre, Vector3 posicion)
    {
        GameObject p = new GameObject("PuntoCamara_" + (padre.childCount + 1));
        Undo.RegisterCreatedObjectUndo(p, "Punto de cámara");
        p.transform.SetParent(padre, false);
        p.transform.position = posicion;
        return p;
    }
}
