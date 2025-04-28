using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    public Camera renderCamera;
    public static TileManager Instance;
    public struct Tile
    {
        public Vector3 centerPoint;
        public Vector2 size;
        public Bounds bound;
    }

    [Range(1, 20)]
    public int tileNumber;
    List<Tile> tileList = new List<Tile>();

    private void Awake()
    {
        if(Instance != null)
        {
            Debug.Log("Fail!!!!!!");
        }
        Instance = this;
    }

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

        var grassTiles = FrustumCulling(renderCamera);
        for (int i = 0; i < grassTiles.Count; i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(grassTiles[i].bound.center, grassTiles[i].bound.size);
        }
    }

    public List<Tile> grassTiles = new List<Tile>();
    public List<Tile> FrustumCulling(Camera camera)
    {
        grassTiles.Clear();
        var planes = GeometryUtility.CalculateFrustumPlanes(camera);

        for (int i = 0; i < tileList.Count; i++)
        {
            if (GeometryUtility.TestPlanesAABB(planes, tileList[i].bound) == true)
            {
                grassTiles.Add(tileList[i]);
            }
        }
        return grassTiles;
    }
}
