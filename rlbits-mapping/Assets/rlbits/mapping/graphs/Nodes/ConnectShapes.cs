using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;
namespace RLBits.Mapping.Graphs
{
    [CreateNodeMenu("Shapes/ConnectShapes")]
    [NodeTint(0.35f, 0.05f, 0.6f)]

    public class ConnectShapes : PCGNode
    {
        protected Vector2Int m_NoiseParentSize;

        [Input] public GridMap m_Input;

        [Output] public List<GridShape> m_Shapes;
        [Output] public float[] m_result;
        
        [Header("单个节点最大链接数量")]
        public int maxConnections = 6;
        
        [Header("单个节点最小链接数量")]
        public int minConnections = 2;
        
        [Header("连接半径")]
        public int radius = 15;
        
        public class EdgeData
        {
            public GridShape a;
            public GridShape b;
        }

        // Return the correct value of an output port when requested
        public override object GetValue(NodePort port)
        {
            if (port.fieldName == "m_result")
            {
                if (m_result != null)
                {
                    if (m_result.Length != noiseGraph.TotalCells)
                    {
                        UpdateData();
                    }
                    return m_result;
                }
            }
            else if (port.fieldName == "m_Shapes")
            {
                if (m_Shapes != null)
                {
                    return m_Shapes;
                }
            }
            return null;
        }

        public override void UpdateData(bool withOutputs = true)
        {
            m_NoiseParentSize = noiseGraph.Size;
            m_Input = GetPort("m_Input").GetInputValue<GridMap>();

            m_result = new float[m_NoiseParentSize.x * m_NoiseParentSize.y];

            Random.InitState(noiseGraph.Seed);

            if (m_Input == null)
            {
                return;
            }

            //construct mst
            List<EdgeData> allEdges = new List<EdgeData>();
            var keys = m_Input.gridShapes.Keys.ToList();
            
            for (int starts = 0; starts < keys.Count; starts++)
            {
                for (int ends = 0; ends < starts; ends++)
                {
                    if (starts == ends)
                        continue;
                    var startNode = m_Input.gridShapes[keys[starts]];
                    var endNode = m_Input.gridShapes[keys[ends]];
                    if (Vector2Int.Distance(startNode.position, endNode.position) > radius)
                    {
                        continue;
                    }
                    startNode.neighbours.Add(endNode.index);
                    endNode.neighbours.Add(startNode.index);
                    allEdges.Add(new EdgeData()
                    {
                        a = startNode,
                        b = endNode
                    });
                }
            }
            
            //处理孤岛, 小于minConnections的节点, 就找到最近的节点连接到满足minConnections
            foreach (var p in m_Input.gridShapes)
            {
                var shape = p.Value;
                while (shape.neighbours.Count < minConnections)
                {
                    GridShape closestShape = null;
                    float closestDistance = float.MaxValue;
            
                    foreach (var other in m_Input.gridShapes)
                    {
                        if (other.Key == p.Key || shape.neighbours.Contains(other.Value.index))
                            continue;
            
                        float distance = Vector2Int.Distance(shape.position, other.Value.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestShape = other.Value;
                        }
                    }
            
                    if (closestShape != null)
                    {
                        shape.neighbours.Add(closestShape.index);
                        closestShape.neighbours.Add(shape.index);
                        allEdges.Add(new EdgeData()
                        {
                            a = shape,
                            b = closestShape
                        });
                    }
                    else
                    {
                        break;
                    }
                }
            }

            //处理多余的连接, 大于maxConnections的节点, 就找到最远的节点断开连接
            foreach (var p in m_Input.gridShapes)
            {
                var shape = p.Value;
                while (shape.neighbours.Count > maxConnections)
                {
                    GridShape farthestShape = null;
                    float farthestDistance = float.MinValue;

                    foreach (var neighbourIndex in shape.neighbours)
                    {
                        var neighbour = m_Input.gridShapes.Values.First(s => s.index == neighbourIndex);
                        float distance = Vector2Int.Distance(shape.position, neighbour.position);
                        if (distance > farthestDistance)
                        {
                            farthestDistance = distance;
                            farthestShape = neighbour;
                        }
                    }

                    if (farthestShape != null)
                    {
                        shape.neighbours.Remove(farthestShape.index);
                        farthestShape.neighbours.Remove(shape.index);
                        allEdges.RemoveAll(e => (e.a == shape && e.b == farthestShape) || (e.b == shape && e.a == farthestShape));
                    }
                    else
                    {
                        break;
                    }
                }
            }

            foreach (EdgeData edgeData in allEdges)
            {
                var ps = GetPointsOnLine(edgeData.a.position, edgeData.b.position);
                foreach (Vector2Int vector2Int in ps)
                {
                    m_result[vector2Int.x + vector2Int.y * m_NoiseParentSize.x] = 1;
                }
            }
            
            base.UpdateData(withOutputs);
            
        }

        public static List<Vector2Int> GetPointsOnLine(Vector2Int p1, Vector2Int p2)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            int x0 = p1.x;
            int y0 = p1.y;
            int x1 = p2.x;
            int y1 = p2.y;

            bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);
            if (steep)
            {
                int t;
                t = x0; // swap x0 and y0
                x0 = y0;
                y0 = t;
                t = x1; // swap x1 and y1
                x1 = y1;
                y1 = t;
            }
            if (x0 > x1)
            {
                int t;
                t = x0; // swap x0 and x1
                x0 = x1;
                x1 = t;
                t = y0; // swap y0 and y1
                y0 = y1;
                y1 = t;
            }
            int dx = x1 - x0;
            int dy = Mathf.Abs(y1 - y0);
            int error = dx / 2;
            int ystep = (y0 < y1) ? 1 : -1;
            int y = y0;
            for (int x = x0; x <= x1; x++)
            {
                result.Add(new Vector2Int((steep ? y : x), (steep ? x : y)));
                error = error - dy;
                if (error < 0)
                {
                    y += ystep;
                    error += dx;
                }
            }

            return result;
        }
    }
}