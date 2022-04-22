using UnityEngine;

public static class EditorModifications
{
    public const bool matchFixedTimeToDeltaTime = false;

    public static Resolution desktopResolution => Screen.systemResolution;

    public static void SetFadeAmount(this Renderer renderer, float fadeAmount)
    {
        renderer.fadeAmount = fadeAmount;
    }

    public static GameObject Instantiate(GameObject prefab, Transform parent, Vector3 localPos, Quaternion localRot, bool awake)
    {
        return Object.Instantiate(prefab, parent, localPos, localRot, awake);
    }

    public static GameObject Instantiate(GameObject prefab, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale, bool awake)
    {
        return Object.Instantiate(prefab, parent, localPos, localRot, localScale, awake);
    }
}
