using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

[RequireComponent(typeof(Light2D))]
[RequireComponent(typeof(PolygonCollider2D))]
public class MirrorBeamController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float maxBeamLength = 20f;
    [SerializeField] private float beamWidth = 0.5f;
    [SerializeField] private float wallIgnoreRadius = 0.5f;
    [SerializeField] private LayerMask wallLayers;
    [SerializeField] private LayerMask crystalLayers;

    private Light2D _light;
    private PolygonCollider2D _collider;
    private Crystal _parentCrystal;

    void Awake()
    {
        _light = GetComponent<Light2D>();
        _collider = GetComponent<PolygonCollider2D>();
        _parentCrystal = GetComponentInParent<Crystal>();
        
        // 确保 Light2D 类型是 Freeform，这样我们才能动态修改形状
        _light.lightType = Light2D.LightType.Freeform;
    }

    void Update()
    {
        if (_light.enabled)
        {
            UpdateBeamShape();
        }
    }

    private void UpdateBeamShape()
    {
        Vector2 origin = transform.position;
        Vector2 direction = transform.up;

        // 【核心修正】：射线从圆周出发，而不是圆心
        // 起点偏移 = 圆心 + 方向 * 忽略半径
        Vector2 rayOrigin = origin + direction * wallIgnoreRadius;
        float rayLength = maxBeamLength - wallIgnoreRadius;

        LayerMask combinedLayers = wallLayers | crystalLayers;
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayOrigin, direction, rayLength, combinedLayers);
        
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        
        float currentLength = maxBeamLength;

        foreach (var hit in hits)
        {
            // 排除自己的父对象
            if (_parentCrystal != null && (hit.collider.gameObject == _parentCrystal.gameObject || hit.transform.IsChildOf(_parentCrystal.transform)))
            {
                continue;
            }

            bool isCrystal = ((1 << hit.collider.gameObject.layer) & crystalLayers) != 0 && hit.collider.CompareTag("Crystal");
            bool isWall = ((1 << hit.collider.gameObject.layer) & wallLayers) != 0;

            if (isCrystal)
            {
                // 截断到几何中心：距离 = 几何中心到圆心的距离
                currentLength = Vector2.Distance(origin, hit.collider.transform.position);
                break;
            }
            else if (isWall)
            {
                // 截断到碰撞点：距离 = 碰撞点到圆心的距离 (即 hit.distance + 忽略半径)
                currentLength = hit.distance + wallIgnoreRadius;
                break;
            }
        }

        // 更新 Light2D 的形状 (Freeform Light)
        // 我们创建一个长方形的路径
        Vector3[] path = new Vector3[4];
        float halfWidth = beamWidth * 0.5f;
        
        path[0] = new Vector3(-halfWidth, 0, 0);
        path[1] = new Vector3(halfWidth, 0, 0);
        path[2] = new Vector3(halfWidth, currentLength, 0);
        path[3] = new Vector3(-halfWidth, currentLength, 0);

        _light.SetShapePath(path);

        // 更新 PolygonCollider2D 的形状
        Vector2[] colliderPath = new Vector2[4];
        colliderPath[0] = new Vector2(-halfWidth, 0);
        colliderPath[1] = new Vector2(halfWidth, 0);
        colliderPath[2] = new Vector2(halfWidth, currentLength);
        colliderPath[3] = new Vector2(-halfWidth, currentLength);
        
        _collider.SetPath(0, colliderPath);
    }
}
