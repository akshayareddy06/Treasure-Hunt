using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TreasureHunt.Editor
{
    [InitializeOnLoad]
    public static class AutoOpenMainMenu
    {
        static AutoOpenMainMenu()
        {
            EditorApplication.delayCall += CheckAndOpenScene;
        }

        private static void CheckAndOpenScene()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path) || activeScene.name == "Untitled")
            {
                string targetPath = "Assets/Scenes/MainMenu.unity";
                if (System.IO.File.Exists(targetPath))
                {
                    Debug.Log("Auto-loading MainMenu scene for user...");
                    EditorSceneManager.OpenScene(targetPath);
                }
            }
        }
    }
}