using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening; // 引入 DOTween 命名空间

public class Mirror : Crystal
{
    [Header("Mirror Settings")]
    [SerializeField] protected MirrorBeamController beamController;
    [SerializeField] private float rotationStep = 30f;
    [SerializeField] private bool canRotate = true;
    [SerializeField] private float rotationDuration = 0.3f; // 旋转动画持续时间

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip rotateSound;

    private void Start()
    {
        if (beamController == null)
            beamController = GetComponentInChildren<MirrorBeamController>();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        UpdateMirrorLight();
    }

    protected override void UpdateVisuals()
    {
        base.UpdateVisuals();
        UpdateMirrorLight();
    }

    private void UpdateMirrorLight()
    {
        if (beamController != null)
        {
            beamController.gameObject.SetActive(isIlluminated);
        }
    }

    protected override void CheckIllumination()
    {
        // 镜子在旋转时，如果依赖自身的触发器检测可能会有延迟
        // 调用基类的主动检测逻辑
        base.CheckIllumination();
    }

    public void RotateMirror()
    {
        if (!canRotate)
        {
            Debug.Log($"[Mirror] {gameObject.name} 旋转已被禁用 (canRotate = false)");
            return;
        }

        // 防止在动画过程中重复触发
        if (DOTween.IsTweening(transform)) return;

        Debug.Log($"[Mirror] {gameObject.name} RotateMirror() 被调用，当前旋转: {transform.eulerAngles.z}");
        
        // 播放旋转音效
        if (audioSource != null && rotateSound != null)
        {
            audioSource.PlayOneShot(rotateSound);
        }

        // 计算目标角度
        float currentZ = transform.eulerAngles.z;
        float targetZ = currentZ + rotationStep;
        
        // 使用 DOTween 进行平滑旋转
        transform.DORotate(new Vector3(0, 0, targetZ), rotationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuad)
            .OnUpdate(() => {
                // 动画过程中同步变换，确保光线位置正确
                Physics2D.SyncTransforms();
            })
            .OnComplete(() => {
                // 动画结束后进行最终对齐和状态检测
                float finalZ = transform.eulerAngles.z;
                finalZ %= 360f;
                if (finalZ < 0) finalZ += 360f;
                finalZ = Mathf.Round(finalZ / rotationStep) * rotationStep;
                if (Mathf.Approximately(finalZ, 360f)) finalZ = 0f;
                
                transform.eulerAngles = new Vector3(0, 0, finalZ);
                
                Physics2D.SyncTransforms();
                CheckIllumination();
                UpdateVisuals();
                
                Debug.Log($"[Mirror] {gameObject.name} 旋转完成，最终角度: {transform.eulerAngles.z:F6}");
            });
    }
}
