using UnityEngine;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader RayTracingShader;
    public Texture2D SkyboxTexture;
    public Light DirectionalLight;
    public Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
    public uint SphereMax = 100;
    public float SpherePlacementRadius = 100.0f;

    private RenderTexture _target;
    private Camera _camera;
    private int kernel;
    private uint _currentSample = 0;
    private Material _addMaterial;
    private ComputeBuffer _sphereBuffer;

    // MonoBehaviour.OnRenderImage: called after camera finishes rendering, allows modification of camera's final image
    // Documentation: https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnRenderImage.html
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Render(destination);
    }

    private void SetShaderParameters()
    {
        RayTracingShader.SetMatrix(name: "_CameraToWorld", val: _camera.cameraToWorldMatrix);
        RayTracingShader.SetMatrix(name: "_CameraInverseProjection", val: _camera.projectionMatrix.inverse);
        RayTracingShader.SetTexture(kernelIndex: 0, name: "_SkyboxTexture", texture: SkyboxTexture);
        RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));
        Vector3 lightDirection = DirectionalLight.transform.forward;
        RayTracingShader.SetVector("_DirectionalLight", 
            new Vector4(lightDirection.x, 
                lightDirection.y, 
                lightDirection.z, 
                DirectionalLight.intensity));
        RayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>(); 
        kernel = RayTracingShader.FindKernel("CSMain");
        
    }

    void Update()
    {
        if(transform.hasChanged || DirectionalLight.transform.hasChanged)
        {
            _currentSample = 0;
            transform.hasChanged = false;
            DirectionalLight.transform.hasChanged = false;
        }

        SetShaderParameters();
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
        _addMaterial.SetFloat("_Sample", _currentSample);
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

    private void OnEnable()
    {
        _currentSample = 0;
        SetUpScene();
    }

    private void OnDisable()
    {
        if (_sphereBuffer != null) _sphereBuffer.Release();
    }

    private void SetUpScene()
    {
        List<Sphere> spheres = new List<Sphere>();

        for (int i = 0; i < SphereMax; i++)
        {
            var randomPos = Random.insideUnitCircle * SpherePlacementRadius;
            Sphere sphere = new Sphere{
                Radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x)
            };
            sphere.Position = new Vector3 (randomPos.x, sphere.Radius, randomPos.y);

            foreach (Sphere other in spheres)
            {
                float minDist = sphere.Radius + other.Radius;
                // Reject spheres that would intersect with other spheres.
                if (Vector3.SqrMagnitude(sphere.Position - other.Position) < minDist * minDist) goto SkipSphere;
            }

            Color color = Random.ColorHSV();
            var metal = Random.value < 0.5f;
            sphere.Albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
            // 4% specularity.
            sphere.Specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;
            
            spheres.Add(sphere);

            SkipSphere: continue;
        }

        _sphereBuffer = new ComputeBuffer(spheres.Count,  40);
        _sphereBuffer.SetData(spheres);
    }
}
