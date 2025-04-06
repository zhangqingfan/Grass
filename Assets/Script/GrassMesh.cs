using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.HableCurve;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GrassMesh : MonoBehaviour
{
    [Range(1f, 2f)]
    public float height = 1;
    
    [Range(0.2f, 0.6f)]
    public float width = 0.3f;
    
    [Range(1, 5)]
    public int segment = 1; //not include top point.

    public static Mesh staticMesh;
    public Material mat;

    void Awake()
    {
        var verticesNum = (segment + 1) * 2 + 1;
        var vertices = new Vector3[verticesNum];
        var k = 0;
        var segmentHeight = height / (segment + 1);

        for(int i = 0; i <= segment; i++)
        {
            vertices[k] = new Vector3(-width / 2, i * segmentHeight, 0);
            vertices[k + 1] = new Vector3(width / 2, i * segmentHeight, 0);
            k += 2;
        }
        vertices[verticesNum - 1] = new Vector3(0, height, 0);

        var triangleCount = (2 * segment + 1) * 3;
        var triangles = new int[triangleCount];
        k = 0;
        for (int i = 0; i < segment; i++)
        {
            triangles[k] = i * 2 + 1;
            triangles[k + 1] = i * 2;
            triangles[k + 2] = i * 2 + 2;

            triangles[k + 3] = i * 2 + 1;
            triangles[k + 4] = i * 2 + 2;
            triangles[k + 5] = i * 2 + 3;

            k += 6;
        }
        triangles[k] = verticesNum - 2;
        triangles[k + 1] = verticesNum - 3;
        triangles[k + 2] = verticesNum - 1;

        var colors = new Color[verticesNum];
        for (int i = 0; i < verticesNum - 1; i++)
        {
            var r = (i % 2 == 0) ? 0 : 1; //r: 0:left, 1:right, 0.5:middle
            var g = (float)((int)(i / 2) / (float)(segment + 1)); //g: height proportion
            colors[i] = new Color(r, g, 0, 0);
            //Debug.Log(colors[i]);
        }
        colors[verticesNum - 1] = new Color(0.5f, 1, 0, 0);

        var uvs = new Vector2[verticesNum];
        for (int i = 0; i < verticesNum; i++)
        {
            var u = (colors[i].r == 0) ? 0.45f : 0.55f;
            u += ((0.5f - u) * colors[i].g);
            uvs[i] = new Vector2(u, colors[i].g); 
        }
        uvs[verticesNum - 1] = new Vector2(0.5f, 0.95f);

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.colors = colors;

        staticMesh = new Mesh();
        staticMesh.vertices = vertices;
        staticMesh.triangles = triangles;
        staticMesh.uv = uvs;
        staticMesh.colors = colors;

        var mf = gameObject.GetComponent<MeshFilter>();
        mf.mesh = mesh;
        var mr = gameObject.GetComponent<MeshRenderer>();
        mr.material = mat;
    }
}
