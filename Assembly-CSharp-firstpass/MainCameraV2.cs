using UnityEngine;

public sealed class MainCameraV2 : MonoBehaviour, ICompileTimeCheckable
{
    [SerializeField]
    [AssertNotNull]
    private Camera cam;

    [SerializeField]
    private int defaultCullingMask;

    public static MainCameraV2 main { get; private set; }

    private void Start()
    {
        main = this;
    }

    public void OverrideCullingMask(LayerMask mask)
    {
        cam.cullingMask = mask;
    }

    public void RestoreCullingMask()
    {
        cam.cullingMask = defaultCullingMask;
    }

    [ContextMenu("Copy culling mask")]
    private void CopyCullingMask()
    {
        defaultCullingMask = cam.cullingMask;
    }

    public string CompileTimeCheck()
    {
        if (defaultCullingMask != cam.cullingMask)
        {
            return $"Default culling mask ({defaultCullingMask}) must be initialized to camera's culling mask ({cam.cullingMask})";
        }
        return null;
    }
}
