using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class GrassGenerator : MonoBehaviour
{
    public ComputeShader computeShader;
    public Camera renderCamera;
    public Material grassMat;
    public Material VoronoiMat;
    public int2 voronoiRTSize;
    private RenderTexture voronoiRT;

    ComputeBuffer worldPosBuffer;
    ComputeBuffer triangleBuffer;
    ComputeBuffer uvBuffer;
    ComputeBuffer positionBuffer;
    ComputeBuffer colorBuffer;
    ComputeBuffer argsBuffer;
    Bounds bounds;

    ComputeBuffer debugBuffer;

    int range = 100;
    float spacing = 0.5f;

    readonly int rangeID = Shader.PropertyToID("_Range");
    readonly int spacingID = Shader.PropertyToID("_Spacing");
    readonly int worldPosBufferID = Shader.PropertyToID("worldPosBuffer");
    readonly int vpMatrixID = Shader.PropertyToID("_VP_MATRIX");

    void Start()
    {
        worldPosBuffer = new ComputeBuffer(range * range, sizeof(float) * 3, ComputeBufferType.Append);
        worldPosBuffer.SetCounterValue(0);
        debugBuffer = new ComputeBuffer(range * range, sizeof(float) * 3, ComputeBufferType.Append);

        bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
        argsBuffer = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments);
        
        CreateComputerBufferFromMesh(GrassMesh.Instance.mesh);
        CreateVoronoiRT(voronoiRTSize.x, voronoiRTSize.y);
        /*
        var mf = gameObject.GetComponent<MeshFilter>();
        mf.mesh = GrassMesh.Instance.mesh;
        var mr = gameObject.GetComponent<MeshRenderer>();
        mr.material = mat;
        */
    }

    void CreateVoronoiRT(int width, int height)
    {
        voronoiRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Bilinear,
            autoGenerateMips = false
        };
        voronoiRT.Create();
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

        grassMat.SetBuffer("worldPosBuffer", worldPosBuffer);
    }

    private void Update()
    {
        worldPosBuffer.SetCounterValue(0);

        computeShader.SetInt(rangeID, range);
        computeShader.SetFloat(spacingID, spacing);
        computeShader.SetBuffer(0, worldPosBufferID, worldPosBuffer);

        var pMatrix = GL.GetGPUProjectionMatrix(renderCamera.projectionMatrix, false);
        var vpMatrix = pMatrix * renderCamera.worldToCameraMatrix;
        computeShader.SetMatrix(vpMatrixID, vpMatrix);

        var threadCountX = Mathf.CeilToInt(range / 8f);
        var threadCountZ = Mathf.CeilToInt(range / 8f);
        computeShader.Dispatch(0, threadCountX, threadCountZ, 1);

        Graphics.Blit(null, voronoiRT, VoronoiMat);
    }

    int[] args = new int[4];
    private void LateUpdate()
    {
        args[0] = GrassMesh.Instance.mesh.triangles.Length;
        args[1] = 0;
        args[2] = 0;
        args[3] = 0;
        argsBuffer.SetData(args);
        ComputeBuffer.CopyCount(worldPosBuffer, argsBuffer, sizeof(int)); //must update worldPosbuffer count in every frame!!! 

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
        if (debugBuffer != null) debugBuffer.Release();
    }
}
