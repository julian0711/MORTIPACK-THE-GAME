using UnityEngine;
using UnityEditor;

public class SoundSetupTool : EditorWindow
{
    [MenuItem("Tools/Fix Sound Manager")]
    public static void FixSoundManager()
    {
        GameObject go = GameObject.Find("GlobalSoundManager");
        if (go == null)
        {
            go = new GameObject("GlobalSoundManager");
            Undo.RegisterCreatedObjectUndo(go, "Create GlobalSoundManager");
            Debug.Log("[SoundSetup] Created 'GlobalSoundManager' object.");
        }

        GlobalSoundManager manager = go.GetComponent<GlobalSoundManager>();
        if (manager == null)
        {
            manager = Undo.AddComponent<GlobalSoundManager>(go);
            Debug.Log("[SoundSetup] Added 'GlobalSoundManager' script.");
        }
        
        Debug.Log("GlobalSoundManager check confirmed! You can now verify the Inspector for assigned clips.");
        Selection.activeGameObject = go;
    }
}
