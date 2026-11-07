using UnityEngine;

/// <summary>
/// 目标拖拽器：挂在绿色目标球上，鼠标按下后即可在 XY 平面（Z=0）上拖动它。
/// 关节链的末端会被 IK 求解器实时拉向这个目标位置。
/// </summary>
[RequireComponent(typeof(Collider))]
public class TargetDragger : MonoBehaviour
{
    [Tooltip("拖拽所在平面的 Z 坐标")]
    public float planeZ = 0f;

    private bool dragging;
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        if (cam == null) cam = FindObjectOfType<Camera>();
    }

    private void OnMouseDown()
    {
        dragging = true;
    }

    private void OnMouseUp()
    {
        dragging = false;
    }

    private void Update()
    {
        if (!dragging) return;
        if (cam == null) return;

        // 构造一个与相机垂直、Z=planeZ 的平面，把鼠标射线投射到该平面上
        Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, planeZ));
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        float dist;
        if (plane.Raycast(ray, out dist))
        {
            transform.position = ray.GetPoint(dist);
        }
    }
}
