using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    public Texture SkyboxTexture;

    private RenderTexture _target;
    private Camera _camera;

    // MonoBehaviour.OnRenderImage: clled after camera finishes rendering, allows modification of final image
    // Documentation: https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnRenderImage.html
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Render (destination);
    }

    private void SetShaderParameters()
    {
        RayTracingShader.SetMatrix(name: "_CameraToWorld", val: _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix(name: "_CameraInverseProjection", val: _camera.projectionMatrix.inverse);
        RayTracingShader.SetTexture(kernelIndex: 0, name: "_SkyboxTexture", texture: SkyboxTexture);
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>(); 
    }

    private void Render(RenderTexture destination)
    {
        InitRenderTexture();

        RayTracingShader.SetTexture(kernelIndex: 0, name: "Result", _target);
        var threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        var threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        // Execute ComputeShader, spawn one thread per pixel of the render target
        // The default thread group size as defined in the Unity compute shader template (RayTracingShader.compute) is 
         // [numthreads(8,8,1)], so we’ll stick to that and spawn one thread group per 8×8 pixels
        RayTracingShader.Dispatch(kernelIndex: 0, threadGroupsX, threadGroupsY, threadGroupsZ: 1);

        SetShaderParameters();

        // Draw result to screen
        Graphics.Blit(_target, destination);
    }

    // Create target with appropriate dimensions
    private void InitRenderTexture()
    {
        // TODO: Using TendetTexture.IsCreated
        if (_target != null) _target.Release();

        // TODO: Try different depth & format values
        _target =
            new RenderTexture(Screen.width,
                Screen.height,
                depth: 0,
                format: RenderTextureFormat.ARGBFloat,
                readWrite: RenderTextureReadWrite.Linear);
        // Output textures need random write flag enabled
        _target.enableRandomWrite = true;
        _target.Create();
    }
}
