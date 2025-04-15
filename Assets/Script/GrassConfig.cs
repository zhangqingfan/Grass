using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct GrassConfig
{
    [Range(0, 80)]
    public float pullToCenter;
    [Range(0, 100)]
    public float sameDir;
}
