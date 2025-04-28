using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    public Camera renderCamera;
    
    struct Tile
    {
        public Vector3 centerPoint;
        public Vector2 size;
        public Bounds bound;
    }

    [Range(1, 20)]
    public int tileNumber;
    List<Tile> tileList = new List<Tile>();
    
    void Start()
    {
        Vector3 planeSize = GetComponent<Renderer>().bounds.size;
        var tileSize = new Vector2(planeSize.x / tileNumber, planeSize.z / tileNumber);
        var startPoint = new Vector3(transform.position.x - planeSize.x * 0.5f, 0, transform.position.z - planeSize.z * 0.5f);

        for(int i = 0; i < tileNumber; i++)
        {
            for(int j = 0; j < tileNumber; j++)
            {
                var centerPoint = startPoint + new Vector3(tileSize.x * i + tileSize.x * 0.5f, 0, tileSize.y * j + tileSize.y * 0.5f);
                var tile = new Tile
                {
                    size = tileSize,
                    centerPoint = centerPoint,
                    bound = new Bounds(centerPoint, new Vector3(tileSize.x, 1, tileSize.y))
                };
                tileList.Add(tile);
            }
        }
    }

    private void OnDrawGizmos()
    {
        for(int i = 0; i < tileList.Count; i++)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(tileList[i].bound.center, tileList[i].bound.size);
        }

        var bounds = FrustumCulling();
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

    public Bounds FrustumCulling()
    {
        var bound = new Bounds(Vector3.zero, Vector3.zero);
        var planes = GeometryUtility.CalculateFrustumPlanes(renderCamera);

        for (int i = 0; i < tileList.Count; i++)
        {
            if (GeometryUtility.TestPlanesAABB(planes, tileList[i].bound) == false)
                continue;
            
            if(bound.size == Vector3.zero)
            {
                bound = tileList[i].bound;
                continue;
            }
                    
            bound.Encapsulate(tileList[i].bound);
        }
        return bound;
    }
}
