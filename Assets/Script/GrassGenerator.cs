using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassGenerator : MonoBehaviour
{
    public ComputeShader computeShader;
    ComputeBuffer buffer;

    int range = 100;
    float spacing = 0.1f;

    readonly int rangeID = Shader.PropertyToID("_Range");
    readonly int spacingID = Shader.PropertyToID("_Spacing");
    readonly int bufferID = Shader.PropertyToID("GrassPosition");

    void Start()
    {
        buffer = new ComputeBuffer(range * range, sizeof(float) * 3, ComputeBufferType.Append);
        buffer.SetCounterValue(0);
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

    void OnDestroy()
    {
        buffer.Release();
    }
}
