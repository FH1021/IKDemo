# IKDemo
# 逆向运动学（IK）关节链演示

基于 Unity 的 FABRIK 逆向运动学演示项目：根节点固定，鼠标拖拽末端目标点，整条关节链自动跟随。

---

## 一、如何打开项目

1. 打开 **Unity Hub**
2. 点击 **添加 / Open** → 选择本文件夹 `C:\Users\GZD13\OneDrive\Desktop\逆向运动`
3. 用 **Unity 2022.3 LTS**（或更高版本）打开
4. 首次打开会自动导入资源，等待右下角进度条走完

## 二、如何运行

1. 打开 Unity 后，菜单 **文件 → 新建场景**（或按 `Ctrl + N`），保存为 `Assets/Scenes/Main.unity`
2. 在层级面板（Hierarchy）右键 → **创建空对象**，命名为 `IKRoot`
3. 在 Project 窗口打开 `Assets/Scripts`，把 `IKBootstrap.cs` 拖到 `IKRoot` 上
4. 点击顶部 **▶ 播放按钮**

运行后场景会自动生成：
- 一个红色球 = **根节点**（位置固定不动）
- 一串青色球 = **中间关节**（自动计算角度）
- 一个绿色球 = **目标点**（用鼠标拖拽它）
- 黄色线 = 关节之间的连线
- 左上角 UI：输入关节数量、点"生成关节链"按钮即可重建

## 三、操作说明

| 操作 | 效果 |
|------|------|
| 鼠标按住绿色球拖动 | 带动整条关节链弯曲 |
| 左上角输入框输入数字（2~20） | 改变关节数量 |
| 点击"生成关节链"按钮 | 按新数量重建链 |
| 把目标拖到链够不到的远处 | 整条链会拉成一条直线指向目标 |

## 四、文件结构

```
逆向运动/
├── Assets/
│   ├── Scripts/
│   │   ├── IKBootstrap.cs      # 入口：自动搭建场景、UI，每帧调用求解
│   │   ├── JointChainBuilder.cs# 按关节数量生成球、连线、填充数据
│   │   ├── FabrikSolver.cs     # FABRIK 算法核心（前向推+后向拉迭代）
│   │   └── TargetDragger.cs    # 绿色目标球的鼠标拖拽
│   └── Scenes/                 # 场景自己新建一个 Main 放这
├── Packages/manifest.json
└── ProjectSettings/ProjectVersion.txt
```

## 五、算法原理（FABRIK）

FABRIK = **Forward And Backward Reaching Inverse Kinematics**，是一种迭代式 IK 算法，不需要求导，收敛快。

每一帧做两步：

1. **前向阶段（从末端推向根）**
   - 先把末端关节位置直接设到目标点
   - 从倒数第二个关节开始往前，每个关节都保持"与下一个关节距离 = 原段长"，沿方向挪到新位置

2. **后向阶段（从根拉回）**
   - 把根关节位置强制设回固定锚点
   - 从根往后，每个关节都保持"与前一个关节距离 = 原段长"，沿方向挪到新位置

3. 重复 1、2，直到末端离目标足够近（小于容差）或达到最大迭代次数（默认 20 次）。

> 与解析法（两连杆 IK）相比，FABRIK 对任意关节数都通用；与 CCD（循环坐标下降）相比，FABRIK 收敛更平滑、抖动更小。

## 六、可调参数

选中挂了 `IKBootstrap` 的物体，在 Inspector 里展开 `Fabrik Solver`：

- **Max Iterations**：最大迭代次数（默认 20，越大越精确）
- **Tolerance**：收敛阈值（默认 0.001 米）

展开 `Joint Chain Builder`：

- **Joint Radius**：关节球半径
- **Segment Length**：每段骨头长度
- **Middle Color**：中间关节颜色
- **Line Color**：连线颜色
