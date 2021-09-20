using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;

    private RenderTexture _target;

    // MonoBehaviour.OnRenderImage: clled after camera finishes rendering, allows modification of final image
    // Documentation: https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnRenderImage.html
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Render (destination);
    }

    private void Render(RenderTexture destination)
    {
        InitRenderTexture();
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
        _target.enableRandomWrite = true;
        _target.Create();
    }
}
