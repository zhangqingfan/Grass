using System;
using UnityEngine;

[Serializable]
public struct GrassConfig
{
    [Range(0, 1)]
    public float pullToCenter;
    
    [Range(0, 0.5f)]
    public float heightOffset;

    [Range(0, 0.3f)]
    public float topOffset;

    [Range(0, 0.1f)]
    public float bend;

    [Range(0, 2)]
    public float worldPosOffset;
}
