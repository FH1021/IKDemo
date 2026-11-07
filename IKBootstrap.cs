using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// IK 场景引导脚本：挂在任意空物体上，Play 后会自动完成：
///   1) 创建相机、灯光、EventSystem
///   2) 创建可拖拽的绿色目标球
///   3) 创建 UI（输入关节数量 + 生成按钮 + 提示文字）
///   4) 每帧调用 FABRIK 求解器，把关节链末端拉向目标球
/// 使用方法：
///   1. 新建一个空场景
///   2. 新建一个空 GameObject，命名为 IKRoot
///   3. 把本脚本拖到 IKRoot 上
///   4. 点 Play
/// </summary>
public class IKBootstrap : MonoBehaviour
{
    private FabrikSolver solver;
    private JointChainBuilder builder;
    private Transform target;
    private InputField jointCountInput;
    private Text statusText;

    private void Start()
    {
        SetupScene();
        SetupUI();
        Build(5); // 默认 5 个关节
    }

    // ---------- 场景基础 ----------
    private void SetupScene()
    {
        // 相机
        if (Camera.main == null)
        {
            Camera camObj = new GameObject("Main Camera").AddComponent<Camera>();
            camObj.tag = "MainCamera";
            camObj.transform.position = new Vector3(2f, 0f, -12f);
            camObj.transform.rotation = Quaternion.identity;
            camObj.clearFlags = CameraClearFlags.SolidColor;
            camObj.backgroundColor = new Color(0.15f, 0.15f, 0.20f);
        }
        else
        {
            Camera.main.transform.position = new Vector3(2f, 0f, -12f);
            Camera.main.transform.rotation = Quaternion.identity;
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0.15f, 0.15f, 0.20f);
        }

        // 灯光
        if (FindObjectOfType<Light>() == null)
        {
            Light l = new GameObject("Directional Light").AddComponent<Light>();
            l.type = LightType.Directional;
            l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            l.intensity = 1.2f;
        }

        // EventSystem（UI 交互必须）
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        // 绿色目标球（被拖拽）
        GameObject targetObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        targetObj.name = "Target";
        targetObj.transform.localScale = Vector3.one * 0.45f;
        targetObj.transform.position = new Vector3(5f, 0f, 0f);
        Renderer tr = targetObj.GetComponent<Renderer>();
        tr.material.color = Color.green;   // 直接改自带材质颜色，避免 Shader.Find 返回 null
        targetObj.AddComponent<TargetDragger>();
        target = targetObj.transform;

        // 本物体上挂 solver 与 builder
        solver = gameObject.AddComponent<FabrikSolver>();
        builder = gameObject.AddComponent<JointChainBuilder>();
    }

    // ---------- UI ----------
    private void SetupUI()
    {
        Font arial = Resources.GetBuiltinResource<Font>("Arial.ttf");

        Canvas canvas = new GameObject("Canvas").AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.gameObject.AddComponent<CanvasScaler>();
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        // 标签
        Text label = NewText("关节数:", arial);
        label.transform.SetParent(canvas.transform, false);
        RectTransform lr = label.GetComponent<RectTransform>();
        lr.anchorMin = new Vector2(0, 1); lr.anchorMax = new Vector2(0, 1);
        lr.anchoredPosition = new Vector2(60f, -30f);
        lr.sizeDelta = new Vector2(80f, 30f);
        label.alignment = TextAnchor.MiddleRight;
        label.color = Color.white;
        label.fontSize = 16;

        // 输入框
        InputField input = NewInputField(arial);
        input.transform.SetParent(canvas.transform, false);
        RectTransform ir = input.GetComponent<RectTransform>();
        ir.anchorMin = new Vector2(0, 1); ir.anchorMax = new Vector2(0, 1);
        ir.anchoredPosition = new Vector2(160f, -30f);
        ir.sizeDelta = new Vector2(100f, 30f);
        input.text = "5";
        jointCountInput = input;

        // 按钮
        Button btn = NewButton("生成关节链", arial);
        btn.transform.SetParent(canvas.transform, false);
        RectTransform br = btn.GetComponent<RectTransform>();
        br.anchorMin = new Vector2(0, 1); br.anchorMax = new Vector2(0, 1);
        br.anchoredPosition = new Vector2(280f, -30f);
        br.sizeDelta = new Vector2(110f, 30f);
        btn.onClick.AddListener(OnBuildClicked);

        // 提示
        statusText = NewText("提示：拖拽绿色球即可带动关节链；红色球为固定根节点。", arial);
        statusText.transform.SetParent(canvas.transform, false);
        RectTransform sr = statusText.GetComponent<RectTransform>();
        sr.anchorMin = new Vector2(0, 1); sr.anchorMax = new Vector2(0, 1);
        sr.anchoredPosition = new Vector2(10f, -75f);
        sr.sizeDelta = new Vector2(600f, 30f);
        statusText.alignment = TextAnchor.UpperLeft;
        statusText.color = Color.white;
        statusText.fontSize = 14;
    }

    private Text NewText(string content, Font font)
    {
        Text t = new GameObject("Text").AddComponent<Text>();
        t.text = content;
        t.font = font;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private InputField NewInputField(Font font)
    {
        GameObject go = new GameObject("InputField");
        Image bg = go.AddComponent<Image>();
        bg.color = Color.white;
        InputField input = go.AddComponent<InputField>();
        input.targetGraphic = bg;

        Text txt = NewText("", font);
        txt.transform.SetParent(go.transform, false);
        txt.color = Color.black;
        RectTransform tr = txt.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(8f, 0f);
        tr.offsetMax = new Vector2(-8f, 0f);
        txt.alignment = TextAnchor.MiddleLeft;
        input.textComponent = txt;
        return input;
    }

    private Button NewButton(string label, Font font)
    {
        GameObject go = new GameObject("Button");
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.30f, 0.65f, 1f);
        Button b = go.AddComponent<Button>();
        b.targetGraphic = bg;

        Text txt = NewText(label, font);
        txt.transform.SetParent(go.transform, false);
        RectTransform tr = txt.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.fontSize = 14;
        return b;
    }

    // ---------- 逻辑 ----------
    private void OnBuildClicked()
    {
        int n;
        if (!int.TryParse(jointCountInput.text, out n)) n = 5;
        n = Mathf.Clamp(n, 2, 20);
        Build(n);
    }

    private void Build(int jointCount)
    {
        // 根节点固定在 (-3, 0, 0)，链向右水平铺开
        builder.BuildChain(jointCount, new Vector3(-3f, 0f, 0f));
    }

    private void Update()
    {
        if (solver != null && solver.joints.Count > 1 && target != null)
        {
            solver.Solve(target.position);
            // 连线由 JointChainBuilder.OnRenderObject 自动画，不需要手动刷新
        }
    }
}
