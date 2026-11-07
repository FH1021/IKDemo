using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// FABRIK 逆向运动学求解器
/// （Forward And Backward Reaching Inverse Kinematics）
/// 原理：
///   1. 前向阶段：从末端关节开始，把末端拉到目标点，然后依次让每个关节保持"段长不变"地推向根。
///   2. 后向阶段：把根节点拉回固定位置，再依次让每个关节保持"段长不变"地拉向末端。
///   3. 重复上述两步直到末端足够接近目标，或达到最大迭代次数。
/// 优点：无需求导、收敛快、对任意关节数都稳定。
/// </summary>
public class FabrikSolver : MonoBehaviour
{
    [Header("求解参数")]
    [Tooltip("最大迭代次数，越大越精确但越耗性能")]
    public int maxIterations = 20;

    [Tooltip("收敛阈值（米），末端距目标小于该值即停止")]
    public float tolerance = 0.001f;

    [Header("运行时数据（由关节链生成器填充）")]
    public List<Transform> joints = new List<Transform>();
    public List<float> lengths = new List<float>();
    public Transform root; // 根节点（位置固定）

    /// <summary>
    /// 对整条关节链求解一次，让末端尽量靠近 targetPos
    /// </summary>
    public void Solve(Vector3 targetPos)
    {
        if (joints == null || joints.Count < 2) return;
        if (root == null) root = joints[0];

        // 1) 先算整条链的总长度，判断目标是否可达
        float totalLength = 0f;
        for (int i = 0; i < lengths.Count; i++) totalLength += lengths[i];
        float rootToTarget = Vector3.Distance(root.position, targetPos);

        // 2) 不可达：直接把链拉成一条指向目标的直线
        if (rootToTarget > totalLength)
        {
            Vector3 dir = (targetPos - root.position).normalized;
            joints[0].position = root.position;
            for (int i = 1; i < joints.Count; i++)
            {
                joints[i].position = joints[i - 1].position + dir * lengths[i - 1];
            }
            return;
        }

        // 3) 可达：FABRIK 迭代
        List<Vector3> pos = new List<Vector3>(joints.Count);
        for (int i = 0; i < joints.Count; i++) pos.Add(joints[i].position);
        Vector3 basePos = root.position; // 根节点固定锚点

        for (int iter = 0; iter < maxIterations; iter++)
        {
            // ---- 前向：末端推向目标 ----
            pos[pos.Count - 1] = targetPos;
            for (int i = pos.Count - 2; i >= 0; i--)
            {
                float segLen = lengths[i];
                Vector3 dir = (pos[i] - pos[i + 1]).normalized;
                pos[i] = pos[i + 1] + dir * segLen;
            }

            // ---- 后向：根拉回固定点 ----
            pos[0] = basePos;
            for (int i = 0; i < pos.Count - 1; i++)
            {
                float segLen = lengths[i];
                Vector3 dir = (pos[i + 1] - pos[i]).normalized;
                pos[i + 1] = pos[i] + dir * segLen;
            }

            // 收敛判断：末端已经足够接近目标就提前退出
            if (Vector3.Distance(pos[pos.Count - 1], targetPos) < tolerance)
                break;
        }

        // 4) 把计算结果写回 Transform
        for (int i = 0; i < joints.Count; i++)
        {
            joints[i].position = pos[i];
        }
    }

    public void Clear()
    {
        if (joints != null) joints.Clear();
        if (lengths != null) lengths.Clear();
        root = null;
    }
}
