using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassGenerator : MonoBehaviour
{
    public ComputeShader computeShader;
    public Camera renderCamera;
    public Material mat;
    
    ComputeBuffer buffer;
    ComputeBuffer triangleBuffer;
    ComputeBuffer uvBuffer;
    ComputeBuffer positionBuffer;
    ComputeBuffer colorBuffer;
    ComputeBuffer argsBuffer;
    Bounds bounds;
    Mesh grassMesh;


    int range = 100;
    float spacing = 0.1f;

    readonly int rangeID = Shader.PropertyToID("_Range");
    readonly int spacingID = Shader.PropertyToID("_Spacing");
    readonly int bufferID = Shader.PropertyToID("GrassPosition");

    void Start()
    {
        buffer = new ComputeBuffer(range * range, sizeof(float) * 3, ComputeBufferType.Append);
        buffer.SetCounterValue(0);
        
        bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);
        argsBuffer = new ComputeBuffer(1, sizeof(int) * 4, ComputeBufferType.IndirectArguments);

        var go = GameObject.Find("Grass");
        grassMesh = go.GetComponent<MeshFilter>().mesh;
        CreateComputerBufferFromMesh(grassMesh);
    }

    private void Update()
    {
        buffer.SetCounterValue(0);

        computeShader.SetInt(rangeID, range);
        computeShader.SetFloat(spacingID, spacing);
        computeShader.SetBuffer(0, bufferID, buffer);

        var threadCountX = Mathf.CeilToInt(range / 8f);
        var threadCountZ = Mathf.CeilToInt(range / 8f);
        computeShader.Dispatch(0, threadCountX, threadCountZ, 1);
    }

    private void LateUpdate()
    {
        ComputeBuffer.CopyCount(buffer, argsBuffer, sizeof(int));
        Graphics.DrawProceduralIndirect(mat, bounds, MeshTopology.Triangles, argsBuffer, 0, null, null, UnityEngine.Rendering.ShadowCastingMode.Off, true);
    }

    void CreateComputerBufferFromMesh(Mesh mesh)
    {
        triangleBuffer = new ComputeBuffer(mesh.triangles.Length, sizeof(int));
        triangleBuffer.SetData(mesh.triangles);
        
        uvBuffer = new ComputeBuffer(mesh.uv.Length, sizeof(float) * 2);
        uvBuffer.SetData(mesh.uv);

        colorBuffer = new ComputeBuffer(mesh.colors.Length, sizeof(float) * 4);
        colorBuffer.SetData(mesh.colors);

        positionBuffer = new ComputeBuffer(mesh.vertices.Length, sizeof(float) * 3);
        positionBuffer.SetData(mesh.vertices);
    }
        
    void OnDestroy()
    {
        buffer.Release();
        triangleBuffer.Release();
        uvBuffer.Release();
        colorBuffer.Release();
        positionBuffer.Release();
    }
}
