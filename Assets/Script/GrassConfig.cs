using System;
using UnityEngine;

[Serializable]
public struct GrassConfig
{
    [Range(0, 1)]
    public float pullToCenter;
    
    [Range(0, 1)]
    public float heightOffset;
    
    [Range(0, 2)]
    public float offset;

}
