using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    public Texture2D SkyboxTexture;

    private RenderTexture _target;
    private Camera _camera;
    private int kernel;

    private uint _currentSample = 0;
    private Material _addMaterial;

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
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>(); 
        kernel = RayTracingShader.FindKernel("CSMain");
        SetShaderParameters();
    }

    void Update()
    {
        if(transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
        }
        
        if (_addMaterial == null)
        {
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        }
        // Inform shader about current sample.
        _addMaterial.SetFloat("_Sample", (float)_currentSample);
    }

    private void Render(RenderTexture destination)
    {
        InitRenderTexture();

        RayTracingShader.SetTexture(kernel, name: "Result", _target);
        var threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        var threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        // Execute ComputeShader, spawn one thread per pixel of the render target
        // The default thread group size as defined in the Unity compute shader template (RayTracingShader.compute) is 
         // [numthreads(8,8,1)], so we’ll stick to that and spawn one thread group per 8×8 pixels
        RayTracingShader.Dispatch(kernel, threadGroupsX, threadGroupsY, threadGroupsZ: 1);

        if (_addMaterial == null)
        {
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        }
        // Inform shader about current sample.
        _addMaterial.SetFloat("_Sample", (float)_currentSample);
        // Draw result to screen
        Graphics.Blit(_target, destination, _addMaterial);
        _currentSample++;
    }

    // Create target with appropriate dimensions
    private void InitRenderTexture()
    {
        // TODO: Using TendetTexture.IsCreated.
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
