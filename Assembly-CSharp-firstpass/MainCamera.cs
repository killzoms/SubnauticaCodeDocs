using System;
using Gendarme;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MainCamera : MonoBehaviour
{
    private static Camera _camera;

    private static Plane[] _cameraFrustumPlanes;

    [SuppressMessage("Subnautica.Rules", "AvoidCameraMain")]
    public static Camera camera
    {
        get
        {
            if ((bool)_camera)
            {
                return _camera;
            }
            return Camera.main;
        }
    }

    [SuppressMessage("Gendarme.Rules.Exceptions", "DoNotThrowReservedExceptionRule")]
    public static Plane[] GetCameraFrustumPlanes()
    {
        if (_cameraFrustumPlanes == null)
        {
            Debug.LogException(new NullReferenceException("MainCamera.cs: _cameraFrustumPlanes was null"));
        }
        return _cameraFrustumPlanes;
    }

    [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
    public void Update()
    {
        _cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
    }

    [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
    public void OnEnable()
    {
        _camera = GetComponent<Camera>();
        _cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
    }

    [SuppressMessage("Gendarme.Rules.Concurrency", "WriteStaticFieldFromInstanceMethodRule")]
    public void OnDisable()
    {
        _camera = null;
    }
}
