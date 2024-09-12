using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GridMap
{
    public int width;
    public int height;
    
    public Dictionary<int, GridShape> gridShapes = new Dictionary<int, GridShape>();
    
    public int GetKey(int x, int y)
    {
        return x + (y * width);
    }
    
    public int GetKey(Vector2Int pos)
    {
        return pos.x + (pos.y * width);
    }
    
    public void AddShape(GridShape gs)
    {
        gridShapes.Add(GetKey(gs.position.x, gs.position.y), gs);
    }
    
    
}

[System.Serializable]
public class GridShape
{
    public int index;
    public List<int> neighbours = new List<int>();
    public Vector2Int position;

    public GridShape(int index)
    {
        this.index = index;
    }

    public void SetPosition(Vector3Int pos)
    {
        position = new Vector2Int(pos.x, pos.y);
    }

    public override int GetHashCode()
    {
        return position.GetHashCode();
    }
}
