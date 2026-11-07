using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 关节链生成器：根据关节数量生成一串带颜色的关节球，
/// 关节之间用 GL.LINES 画线（不依赖 LineRenderer，避免 shader 被剥离导致粉色）。
/// 根节点 = 第 0 个关节（红色，位置固定）
/// 末端 = 最后一个关节（绿色）
/// 中间关节 = 青色
/// </summary>
public class JointChainBuilder : MonoBehaviour
{
    [Header("外观参数")]
    public float jointRadius = 0.18f;
    public float segmentLength = 1.0f;
    public Color middleColor = Color.cyan;
    public Color lineColor = Color.yellow;

    private readonly List<GameObject> jointObjs = new List<GameObject>();
    private Material lineMaterial; // GL 画线用的材质（Hidden/Internal-Colored，不会被剥离）

    /// <summary>
    /// 重建整条关节链
    /// </summary>
    public void BuildChain(int jointCount, Vector3 startPos)
    {
        ClearChain();

        FabrikSolver solver = GetComponent<FabrikSolver>();
        if (solver == null) solver = gameObject.AddComponent<FabrikSolver>();
        solver.Clear();

        for (int i = 0; i < jointCount; i++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = $"Joint_{i:00}";

            float r = (i == 0 || i == jointCount - 1) ? jointRadius * 1.35f : jointRadius;
            sphere.transform.localScale = Vector3.one * (r * 2f);
            sphere.transform.position = startPos + Vector3.right * (i * segmentLength);

            Renderer sr = sphere.GetComponent<Renderer>();
            if (i == 0) sr.material.color = Color.red;
            else if (i == jointCount - 1) sr.material.color = Color.green;
            else sr.material.color = middleColor;

            // 关节球只做可视化，删掉 Collider，避免挡住绿球的鼠标拖拽
            Collider col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            jointObjs.Add(sphere);
            solver.joints.Add(sphere.transform);
            if (i > 0) solver.lengths.Add(segmentLength);
        }

        solver.root = jointObjs[0].transform;
    }

    /// <summary>
    /// 用 GL immediate mode 画关节之间的连线。
    /// OnRenderObject 在相机渲染时调用，此时 GL 操作会直接进入渲染管线。
    /// </summary>
    private void OnRenderObject()
    {
        var solver = GetComponent<FabrikSolver>();
        if (solver == null || solver.joints == null || solver.joints.Count < 2) return;

        // 懒加载 lineMaterial（Hidden/Internal-Colored 是 Unity 引擎内部 shader，不会被打包剥离）
        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;
            lineMaterial.SetInt("_ZWrite", 0);
            lineMaterial.SetInt("_Cull", 0);
            lineMaterial.SetInt("_ZTest", 8); // Always
        }

        lineMaterial.SetPass(0);
        GL.PushMatrix();
        GL.Begin(GL.LINES);
        GL.Color(lineColor);
        for (int i = 0; i < solver.joints.Count - 1; i++)
        {
            GL.Vertex(solver.joints[i].position);
            GL.Vertex(solver.joints[i + 1].position);
        }
        GL.End();
        GL.PopMatrix();
    }

    private void ClearChain()
    {
        foreach (GameObject g in jointObjs)
        {
            if (g != null) Destroy(g);
        }
        jointObjs.Clear();

        FabrikSolver solver = GetComponent<FabrikSolver>();
        if (solver != null) solver.Clear();
    }
}
