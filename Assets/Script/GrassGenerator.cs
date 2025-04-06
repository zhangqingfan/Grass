using Unity.Mathematics;
using UnityEngine;

struct GrassPosition
{
    public float3 pos;
};

public class GrassGenerator : MonoBehaviour
{
    public ComputeShader computeShader;
    public Camera renderCamera;
    public Material mat;
    
    ComputeBuffer worldPosBuffer;
    ComputeBuffer triangleBuffer;
    ComputeBuffer uvBuffer;
    ComputeBuffer positionBuffer;
    ComputeBuffer colorBuffer;
    ComputeBuffer argsBuffer;
    Bounds bounds;
    Mesh grassMesh;

    ComputeBuffer debugBuffer;

    int range = 5;
    float spacing = 1f;

    readonly int rangeID = Shader.PropertyToID("_Range");
    readonly int spacingID = Shader.PropertyToID("_Spacing");
    readonly int worldPosBufferID = Shader.PropertyToID("worldPosBuffer");

    void Start()
    {
        worldPosBuffer = new ComputeBuffer(range * range, sizeof(float) * 3, ComputeBufferType.Append);
        worldPosBuffer.SetCounterValue(0);
        
        bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
        argsBuffer = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments);

        var go = GameObject.Find("Grass");
        grassMesh = go.GetComponent<MeshFilter>().mesh;
        CreateComputerBufferFromMesh(GrassMesh.staticMesh); //todo...bug!!
    }

    private void Update()
    {
        worldPosBuffer.SetCounterValue(0);

        computeShader.SetInt(rangeID, range);
        computeShader.SetFloat(spacingID, spacing);
        computeShader.SetBuffer(0, worldPosBufferID, worldPosBuffer);

        var threadCountX = Mathf.CeilToInt(range / 8f);
        var threadCountZ = Mathf.CeilToInt(range / 8f);
        computeShader.Dispatch(0, threadCountX, threadCountZ, 1);
    }

    private void LateUpdate()
    {
        ComputeBuffer.CopyCount(worldPosBuffer, argsBuffer, sizeof(int));
        //Graphics.DrawProceduralIndirect(mat, bounds, MeshTopology.Triangles, argsBuffer, 0, null, null, UnityEngine.Rendering.ShadowCastingMode.Off, true);

        //var results = new GrassPosition[1000];
        //debugBuffer.GetData(results);
        //Debug.Log(results[0]);
    }

    void CreateComputerBufferFromMesh(Mesh mesh)
    {
        triangleBuffer = new ComputeBuffer(mesh.triangles.Length, sizeof(int));
        triangleBuffer.SetData(mesh.triangles);
        Shader.SetGlobalBuffer("triangleBuffer", triangleBuffer);

        uvBuffer = new ComputeBuffer(mesh.uv.Length, sizeof(float) * 2);
        uvBuffer.SetData(mesh.uv);
        Shader.SetGlobalBuffer("uvBuffer", uvBuffer);

        colorBuffer = new ComputeBuffer(mesh.colors.Length, sizeof(float) * 4);
        colorBuffer.SetData(mesh.colors);
        Shader.SetGlobalBuffer("colorBuffer", colorBuffer);

        positionBuffer = new ComputeBuffer(mesh.vertices.Length, sizeof(float) * 3);
        positionBuffer.SetData(mesh.vertices);
        Shader.SetGlobalBuffer("vertexBuffer", positionBuffer);

        Shader.SetGlobalBuffer("worldPosBuffer", worldPosBuffer);

        debugBuffer = new ComputeBuffer(mesh.triangles.Length, sizeof(int));
        Shader.SetGlobalBuffer("debugBuffer", debugBuffer);
    }
        
    void OnDestroy()
    {
        worldPosBuffer.Release();
        triangleBuffer.Release();
        uvBuffer.Release();
        colorBuffer.Release();
        positionBuffer.Release();
        argsBuffer.Release();
        debugBuffer.Release();
    }
}
