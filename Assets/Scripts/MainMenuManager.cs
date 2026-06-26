using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attached to a GameObject in the MainMenu scene.
/// Wire the Play and Quit buttons to these methods in the Inspector.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    /// <summary>Called by the Play button — loads the AR game scene.</summary>
    public void PlayGame()
    {
        SceneManager.LoadScene("Game");
    }

    /// <summary>Called by the Quit button.</summary>
    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
