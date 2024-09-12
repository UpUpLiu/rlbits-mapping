using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace RLBits.Mapping.Graphs
{
    [CreateNodeMenu("Shapes/DistributeShapes")]
    [NodeTint(0.35f, 0.05f, 0.6f)]
    public class DistributeShapes : PCGNode
    {
        [Output] public GridMap map;
        [Output] public float[] m_result;

        [Header("最大形状数量")]
        public int m_MaxShapeCount;
        
        [Header("最小相隔半径")]
        public int m_minRadius;

        private int[,] m_Area;

        protected Vector2Int m_NoiseParentSize;
        

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
            else if (port.fieldName == "map")
            {
                if (map != null)
                {
                    return map;
                }
            }
            return null;
        }


        public override void UpdateData(bool withOutputs = true)
        {

            m_NoiseParentSize = noiseGraph.Size;

            if (m_result == null || m_result.Length != m_NoiseParentSize.x * m_NoiseParentSize.y)
            {
                m_result = new float[m_NoiseParentSize.x * m_NoiseParentSize.y];
            }

            for (int y = 0; y < m_NoiseParentSize.y; y++)
            {
                for (int x = 0; x < m_NoiseParentSize.x; x++)
                {
                    m_result[x + (y * m_NoiseParentSize.x)] = 0.0f;
                }
            }

            SetShapeData();

            
            foreach (var mapGridShape in map.gridShapes)
            {
                m_result[GridToArray(mapGridShape.Value.position)] = 1.0f;
            }
            

            base.UpdateData(withOutputs);
        }

        private void SetShapeData()
        {
            Random.InitState(noiseGraph.m_MasterNode.Seed);
            //may not need this
            m_Area = new int[m_NoiseParentSize.x, m_NoiseParentSize.y];
            map = new GridMap();
            int count = m_MaxShapeCount;
            m_minRadius = Mathf.Max(1, m_minRadius);
            int tryCount = 10000000;
            while (count > 0)
            {
                tryCount--;
                if (tryCount <= 0)
                {
                    break;
                }

                var x = Mathf.FloorToInt(Mathf.Lerp(m_Area.GetLength(0)*0.125f, m_Area.GetLength(0) * 0.875f, Random.Range(0.0f, 1.0f)));
                var y = Mathf.FloorToInt(Mathf.Lerp(m_Area.GetLength(1)*0.125f, m_Area.GetLength(1) * 0.875f, Random.Range(0.0f, 1.0f)));
                GridShape rd = new(map.GetKey(x, y));
                rd.position.x = x;
                rd.position.y = y;
                bool add = true;
                if (map.gridShapes.ContainsKey(map.GetKey(rd.position)))
                {
                    continue;
                }
                foreach (KeyValuePair<int,GridShape> mapGridShape in map.gridShapes)
                {
                    if(Vector2Int.Distance(mapGridShape.Value.position, rd.position) < m_minRadius)
                    {
                        count++;
                        add = false;
                        break;
                    }
                }
                if (add)
                {
                    map.AddShape(rd);
                }
                count--;
            }
        }
    }
}