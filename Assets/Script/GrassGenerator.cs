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
    public Texture2D windRT;
    public Vector3 positionOffset;
    [Range(0, 2f)]
    public float windForce;
    [Range(0f, 2f)]
    public float windSpeed;

    ComputeBuffer grassInfoBuffer;
    ComputeBuffer triangleBuffer;
    ComputeBuffer uvBuffer;
    ComputeBuffer positionBuffer;
    ComputeBuffer colorBuffer;
    ComputeBuffer argsBuffer;
    ComputeBuffer grassConfigBuffer;
    Bounds bounds;

    float spacing = 0.1f;
    public List<GrassConfig> grassConfigList = new List<GrassConfig>();

    void Start()
    {
        bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
        CreateVoronoiRT(voronoiRTSize.x, voronoiRTSize.y);

        CreateComputerBuffers();
        SyncStatic();
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

        triangleBuffer = new ComputeBuffer(mesh.triangles.Length, Marshal.SizeOf<int>());
        triangleBuffer.SetData(mesh.triangles);

        uvBuffer = new ComputeBuffer(mesh.uv.Length, Marshal.SizeOf<Vector2>());
        uvBuffer.SetData(mesh.uv);

        colorBuffer = new ComputeBuffer(mesh.colors.Length, Marshal.SizeOf<Color>());
        colorBuffer.SetData(mesh.colors);

        positionBuffer = new ComputeBuffer(mesh.vertices.Length, Marshal.SizeOf<Vector3>());
        positionBuffer.SetData(mesh.vertices);

        grassInfoBuffer = new ComputeBuffer(1000000, sizeof(float) * 3 + sizeof(float) * 6, ComputeBufferType.Append);
        grassInfoBuffer.SetCounterValue(0);

        grassConfigBuffer = new ComputeBuffer(grassConfigList.Count, Marshal.SizeOf<GrassConfig>());
    }

    void SyncStatic()
    {
        //Grass shader;
        Shader.SetGlobalBuffer("triangleBuffer", triangleBuffer);
        Shader.SetGlobalBuffer("uvBuffer", uvBuffer);
        Shader.SetGlobalBuffer("colorBuffer", colorBuffer);
        Shader.SetGlobalBuffer("vertexBuffer", positionBuffer);
        Shader.SetGlobalBuffer("grassInfoBuffer", grassInfoBuffer);
        
        //Voronoi Shader;
        Shader.SetGlobalInt("_grassCount", grassCount);
        computeShader.SetTexture(0, "voronoiRT", voronoiRT);
        computeShader.SetTexture(0, "windRT", windRT);
    }

    void SyncDynamic()
    {
        computeShader.SetVector("_CameraPos", renderCamera.transform.position);
        computeShader.SetBuffer(0, "grassInfoBuffer", grassInfoBuffer);
        computeShader.SetFloat("_WindSpeed", windSpeed);
        computeShader.SetFloat("_Time", Time.time);
        
        var pMatrix = GL.GetGPUProjectionMatrix(renderCamera.projectionMatrix, false);
        var vpMatrix = pMatrix * renderCamera.worldToCameraMatrix;
        computeShader.SetMatrix("_VP_MATRIX", vpMatrix);

        computeShader.SetInt("grassConfigBufferCount", grassConfigList.Count);
        grassConfigBuffer.SetData(grassConfigList.ToArray());
        computeShader.SetBuffer(0, "grassConfigBuffer", grassConfigBuffer);

        Shader.SetGlobalVector("_PositionOffset", positionOffset);
        Shader.SetGlobalFloat("_WindForce", windForce);
    }

    private void Update()
    {
        grassInfoBuffer.SetCounterValue(0);
        SyncDynamic();
        
        var grassTiles = TileManager.Instance.FrustumCulling(renderCamera);
        for (int i = 0; i < grassTiles.Count; i++)
        {
            var tile = grassTiles[i];

            var grassNumber = tile.size / spacing;
            computeShader.SetVector("_GrassNumber", grassNumber);
            computeShader.SetFloat("_Spacing", spacing);
            computeShader.SetVector("_TileLeftBottomPosition", tile.centerPoint - new Vector3(tile.size.x * 0.5f, 0, tile.size.y * 0.5f));

            var threadCountX = Mathf.CeilToInt(grassNumber.x / 8f);
            var threadCountZ = Mathf.CeilToInt(grassNumber.y / 8f);
            computeShader.Dispatch(0, threadCountX, threadCountZ, 1);
        }
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
        ComputeBuffer.CopyCount(grassInfoBuffer, argsBuffer, sizeof(int)); //must update worldPosbuffer count in every frame!!! 
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
        grassInfoBuffer.Release();
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
