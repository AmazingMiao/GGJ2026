using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening; // 引入 DOTween 命名空间

public class Door : MonoBehaviour
{
    [Header("Status")]
    [SerializeField] private bool isOpen = false;

    [Header("Spline Settings")]
    [SerializeField] private SplineAnimate splineAnimate;

    private bool _lastState;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (splineAnimate == null)
            splineAnimate = GetComponent<SplineAnimate>();
        
        _lastState = isOpen;
        
        // 初始化状态
        if (splineAnimate != null)
        {
            // Unity 6 中 SplineAnimate 的属性名可能有所变动
            // 移除不存在的 RestartOnEnable
            splineAnimate.PlayOnAwake = false;
            
            // 初始位置
            splineAnimate.ElapsedTime = isOpen ? splineAnimate.Duration : 0f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // 检测布尔值变化
        if (isOpen != _lastState)
        {
            TriggerDoor(isOpen);
            _lastState = isOpen;
        }
    }

    public void TriggerDoor(bool open)
    {
        if (splineAnimate == null) return;

        // 停止之前的 Tween 动画，防止冲突
        DOTween.Kill(splineAnimate);

        if (open)
        {
            // 使用 DOTween 平滑控制 NormalizedTime 到 1
            DOTween.To(() => splineAnimate.NormalizedTime, x => splineAnimate.NormalizedTime = x, 1f, splineAnimate.Duration)
                .SetTarget(splineAnimate)
                .SetEase(Ease.InOutQuad);
            Debug.Log("Door Opening Smoothly...");
        }
        else
        {
            // 使用 DOTween 平滑控制 NormalizedTime 回到 0
            DOTween.To(() => splineAnimate.NormalizedTime, x => splineAnimate.NormalizedTime = x, 0f, splineAnimate.Duration)
                .SetTarget(splineAnimate)
                .SetEase(Ease.InOutQuad);
            Debug.Log("Door Closing Smoothly...");
        }
    }

    // 提供给外部（如 Crystal）调用的接口
    public void SetOpen(bool value)
    {
        isOpen = value;
    }
}
