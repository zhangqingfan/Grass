using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

public class GrassGenerator : MonoBehaviour
{
    public ComputeShader computeShader;
    public Camera renderCamera;
    public Material grassMat;
    [Range(5, 30)]
    public int grassCount;
    public Material VoronoiMat;
    public int2 voronoiRTSize;
    private RenderTexture voronoiRT;

    ComputeBuffer worldPosBuffer;
    ComputeBuffer triangleBuffer;
    ComputeBuffer uvBuffer;
    ComputeBuffer positionBuffer;
    ComputeBuffer colorBuffer;
    ComputeBuffer argsBuffer;
    ComputeBuffer grassConfigBuffer;
    Bounds bounds;

    //ComputeBuffer debugBuffer;
    int range = 100;
    float spacing = 0.5f;

    readonly int rangeID = Shader.PropertyToID("_Range");
    readonly int spacingID = Shader.PropertyToID("_Spacing");
    readonly int worldPosBufferID = Shader.PropertyToID("worldPosBuffer");
    readonly int vpMatrixID = Shader.PropertyToID("_VP_MATRIX");

    public List<GrassConfig> grassConfigList = new List<GrassConfig>();
    readonly int grassConfigBufferCount = Shader.PropertyToID("grassConfigBufferCount");
    readonly int grassConfigBufferID = Shader.PropertyToID("grassConfigBuffer");

    void Start()
    {
        //debugBuffer = new ComputeBuffer(range * range, sizeof(float) * 3, ComputeBufferType.Append);
        bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);

        CreateComputerBuffers();
        SyncShader();
        CreateVoronoiRT(voronoiRTSize.x, voronoiRTSize.y);
    }

    void CreateVoronoiRT(int width, int height)
    {
        RenderTextureDescriptor desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGBHalf, 0)
        {
            autoGenerateMips = false,
            sRGB = false
        };
        voronoiRT = new RenderTexture(desc);
        voronoiRT.filterMode = FilterMode.Point;
        voronoiRT.Create();
        Graphics.Blit(null, voronoiRT, VoronoiMat); 
    }

    void CreateComputerBuffers()
    {
        var mesh = GrassMesh.Instance.mesh;

        triangleBuffer = new ComputeBuffer(mesh.triangles.Length, sizeof(int));
        triangleBuffer.SetData(mesh.triangles);

        uvBuffer = new ComputeBuffer(mesh.uv.Length, sizeof(float) * 2);
        uvBuffer.SetData(mesh.uv);

        colorBuffer = new ComputeBuffer(mesh.colors.Length, sizeof(float) * 4);
        colorBuffer.SetData(mesh.colors);

        positionBuffer = new ComputeBuffer(mesh.vertices.Length, sizeof(float) * 3);
        positionBuffer.SetData(mesh.vertices);

        worldPosBuffer = new ComputeBuffer(range * range, sizeof(float) * 3, ComputeBufferType.Append);
        worldPosBuffer.SetCounterValue(0);

        grassConfigBuffer = new ComputeBuffer(grassConfigList.Count, Marshal.SizeOf<GrassConfig>());
    }

    void SyncShader()
    {
        //Grass shader;
        Shader.SetGlobalBuffer("triangleBuffer", triangleBuffer);
        Shader.SetGlobalBuffer("uvBuffer", uvBuffer);
        Shader.SetGlobalBuffer("colorBuffer", colorBuffer);
        Shader.SetGlobalBuffer("vertexBuffer", positionBuffer);
        Shader.SetGlobalBuffer("worldPosBuffer", worldPosBuffer);
        
        //Voronoi Shader;
        Shader.SetGlobalInt("_grassCount", grassCount);
    }

    void SyncComputeShader()
    {
        computeShader.SetInt(rangeID, range);
        computeShader.SetFloat(spacingID, spacing);
        computeShader.SetBuffer(0, worldPosBufferID, worldPosBuffer);

        var pMatrix = GL.GetGPUProjectionMatrix(renderCamera.projectionMatrix, false);
        var vpMatrix = pMatrix * renderCamera.worldToCameraMatrix;
        computeShader.SetMatrix(vpMatrixID, vpMatrix);

        computeShader.SetInt(grassConfigBufferCount, grassConfigList.Count);
        grassConfigBuffer.SetData(grassConfigList.ToArray());  //todo...¡Ÿ ±¥˙¬Î£°
        computeShader.SetBuffer(0, grassConfigBufferID, grassConfigBuffer);

        computeShader.SetTexture(0, "voronoiRT", voronoiRT);
    }

    private void Update()
    {
        worldPosBuffer.SetCounterValue(0);
        SyncComputeShader();

        var threadCountX = Mathf.CeilToInt(range / 8f);
        var threadCountZ = Mathf.CeilToInt(range / 8f);
        computeShader.Dispatch(0, threadCountX, threadCountZ, 1);
    }

    int[] args = new int[4];
    void InitArgsBuffer()
    {
        argsBuffer = argsBuffer == null ? new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments) : argsBuffer;
        args[0] = GrassMesh.Instance.mesh.triangles.Length;
        args[1] = 0;
        args[2] = 0;
        args[3] = 0;
        argsBuffer.SetData(args);
        ComputeBuffer.CopyCount(worldPosBuffer, argsBuffer, sizeof(int)); //must update worldPosbuffer count in every frame!!! 
    }

    private void LateUpdate()
    {
        InitArgsBuffer();
        Graphics.DrawProceduralIndirect(grassMat, bounds, MeshTopology.Triangles, argsBuffer, 0, null, null, UnityEngine.Rendering.ShadowCastingMode.Off, true);
    }

    private void OnGUI()
    {
        GUI.DrawTexture(new Rect(20, 20, 512, 512), voronoiRT, ScaleMode.ScaleToFit);
    }

    void OnDestroy()
    {
        worldPosBuffer.Release();
        triangleBuffer.Release();
        uvBuffer.Release();
        colorBuffer.Release();
        positionBuffer.Release();
        argsBuffer.Release();
        voronoiRT.Release();
        grassConfigBuffer.Release();
        //if (debugBuffer != null) debugBuffer.Release();
    }
}
