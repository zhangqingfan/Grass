using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct GrassConfig
{
    [Range(0, 1)]
    public float pullToCenter;
    [Range(0, 1)]
    public float sameDir;
}
