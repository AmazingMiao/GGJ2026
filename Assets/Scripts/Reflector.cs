using UnityEngine;

public class Reflector : Mirror
{
    [Header("Reflector Settings")]
    [Tooltip("反射光束的起始点（通常是反射镜面的中心）")]
    [SerializeField] private Transform reflectionOrigin;

    private Vector3 _incomingDirection = Vector3.zero;

    protected override void CheckIllumination()
    {
        // 增加主动检测逻辑，并获取入射光的方向
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;
        Collider2D[] results = new Collider2D[10];
        int count = GetComponent<Collider2D>().Overlap(filter, results);
        
        bool foundLight = false;
        _incomingDirection = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            if (IsCorrectLightSource(results[i]))
            {
                foundLight = true;
                // 获取入射光束的方向（假设光束物体的 transform.up 是其发射方向）
                _incomingDirection = results[i].transform.up;
                break;
            }
        }
        isIlluminated = foundLight;
    }

    protected override void UpdateVisuals()
    {
        base.UpdateVisuals();

        if (isIlluminated && _incomingDirection != Vector3.zero)
        {
            UpdateReflectionAngle();
        }
    }

    private void UpdateReflectionAngle()
    {
        // 法线默认为水平线（即物体的右向量 transform.right）
        Vector3 normal = transform.right;

        // 计算反射向量: R = I - 2 * (I · N) * N
        Vector3 reflectionDir = Vector3.Reflect(_incomingDirection, normal);

        // 更新反射光束物体的朝向
        // 假设 beamController 挂载在子物体上，我们调整该子物体的旋转
        if (beamController != null)
        {
            // 将反射方向转换为旋转角度
            float angle = Mathf.Atan2(reflectionDir.y, reflectionDir.x) * Mathf.Rad2Deg - 90f;
            beamController.transform.rotation = Quaternion.Euler(0, 0, angle);
            
            if (reflectionOrigin != null)
            {
                beamController.transform.position = reflectionOrigin.position;
            }
        }
    }
}
