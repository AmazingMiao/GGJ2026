using UnityEngine;

public class Crystal : MonoBehaviour
{
    [Header("Status")]
    [SerializeField] private bool isIlluminated = false;
    public bool IsIlluminated => isIlluminated;

    [Header("Settings")]
    [Tooltip("检测光照的半径")]
    [SerializeField] private float detectionRadius = 0.5f;
    [Tooltip("光照层级（通常是 Light Layer 1）")]
    [SerializeField] private LayerMask lightLayer;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Door targetDoor;
    [SerializeField] private bool stayOpenAfterIlluminated = false;

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        CheckIllumination();
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isIlluminated ? Color.yellow : Color.white;
        }

        // 更新门的状态
        if (targetDoor != null)
        {
            if (isIlluminated)
            {
                targetDoor.SetOpen(true);
            }
            else if (!stayOpenAfterIlluminated)
            {
                targetDoor.SetOpen(false);
            }
        }
    }

    private void CheckIllumination()
    {
        // 在 2D URP 中，光照通常不直接通过物理碰撞检测
        // 这里使用 OverlapPoint 或 OverlapCircle 检测是否有 Light2D 的包围盒或特定触发器
        // 但最通用的做法是检测手电筒（或其他光源）的距离和角度，或者使用触发器
        
        // 方案：检测是否有带有 Light2D 组件的物体发出的光照射到此处
        // 注意：Unity 2D Light 并没有内置的 "IsPointIlluminated" API
        // 常用替代方案：在手电筒上加一个带有 Trigger 的子物体代表光束区域
    }

    // 推荐方案：使用触发器检测
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("FlashLightBeam"))
        {
            isIlluminated = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("FlashLightBeam"))
        {
            isIlluminated = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isIlluminated ? Color.yellow : Color.white;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
