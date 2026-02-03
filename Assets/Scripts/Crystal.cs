using UnityEngine;

public class Crystal : MonoBehaviour
{
    [Header("Status")]
    [SerializeField] protected bool isIlluminated;
    public bool IsIlluminated => isIlluminated;

    [Header("Settings")]
    [Tooltip("检测光照的半径")]
    [SerializeField] private float detectionRadius = 0.5f;
    [Tooltip("光照层级（通常是 Light Layer 1）")]
    [SerializeField] private LayerMask lightLayer;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Door targetDoor;
    [SerializeField] private bool stayOpenAfterIlluminated;

    [Tooltip("是否只能被镜子反射的光照亮")]
    [SerializeField] private bool onlyMirrorLight;

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

    protected virtual void UpdateVisuals()
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

    protected virtual void CheckIllumination()
    {
        // 增加主动检测逻辑，防止触发器状态丢失
        // 检测当前是否仍有有效光源在接触
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        Collider2D[] results = new Collider2D[10];
        int count = GetComponent<Collider2D>().Overlap(filter, results);
        
        bool foundLight = false;
        for (int i = 0; i < count; i++)
        {
            if (IsCorrectLightSource(results[i]))
            {
                foundLight = true;
                break;
            }
        }
        isIlluminated = foundLight;
    }

    // 推荐方案：使用触发器检测
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        if (IsCorrectLightSource(other))
        {
            isIlluminated = true;
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (IsCorrectLightSource(other))
        {
            isIlluminated = false;
        }
    }

    protected bool IsCorrectLightSource(Collider2D other)
    {
        if (onlyMirrorLight)
        {
            // 仅接受标签为 MirrorBeam 的触发器
            return other.CompareTag("MirrorBeam");
        }
        else
        {
            // 接受普通手电筒或镜子光束
            return other.CompareTag("FlashLightBeam") || other.CompareTag("MirrorBeam");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isIlluminated ? Color.yellow : Color.white;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
