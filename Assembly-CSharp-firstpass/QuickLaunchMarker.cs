using UnityEngine;

[ExecuteInEditMode]
public class QuickLaunchMarker : MonoBehaviour
{
    private bool inited;

    public string originalScene = string.Empty;

    public bool forceNewGame;

    private void Awake()
    {
        if (Application.isPlaying && !inited)
        {
            Object.DontDestroyOnLoad(base.gameObject);
            QuickLaunchHelper.SetupEditorReturnAfterPlayMode(this);
            inited = true;
        }
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            QuickLaunchHelper.TryReturnToOriginalScene();
        }
    }
}
