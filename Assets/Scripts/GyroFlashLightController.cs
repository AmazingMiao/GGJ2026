using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering.Universal; // 必须引用 URP 命名空间
using UnityEngine.InputSystem;         // 必须引用 Input System
using UnityEngine.UI;
using TMPro;

public class StationaryFlashlight : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Light2D flashlight;

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 15f;
    [SerializeField] private float normalIntensity = 1.0f;

    [Header("Gyro Mapping")]
    [SerializeField] private float sensitivity = 12f;
    [SerializeField] private bool invertX;
    [SerializeField] private bool invertY;
    [SerializeField] private bool swapXY;
    [SerializeField] private Vector3 attitudeOffset = Vector3.zero;

    [Header("Network Sync")]
    [SerializeField] private bool useNetworkInput; // PC端勾选

    [Header("Mirror Interaction")]
    [SerializeField] private LayerMask mirrorLayer;
    [SerializeField] private float interactionDistance = 10f;
    [SerializeField] private bool useLightRadiusAsDistance = true;
    [SerializeField] private float rotationCooldown = 0.1f; // 默认冷却时间缩短

    [Header("Debug Info (Read Only)")]
    [SerializeField] private Vector3 debugAttitudeEuler;
    [SerializeField] private Vector2 debugRawXY;
    [SerializeField] private Vector2 debugFinalXY;

    private Quaternion _initialRotation;
    private PlayerInputActions _inputActions;
    private Vector2 _mousePosition;
    private float _lastRotationTime; // 记录上次旋转时间

    void Awake()
    {
        _inputActions = new PlayerInputActions();
        
        // 解决双编辑器 Focus 导致的输入失效问题
        Application.runInBackground = true;
    }

    // 提供给 UGUI Button 调用的公共方法
    public void OnMobileRotateButtonPressed()
    {
        if (useNetworkInput) return;

        // 检查冷却时间
        if (Time.time - _lastRotationTime < rotationCooldown)
        {
            return;
        }

        if (NetworkManagerUDP.Instance != null)
        {
            // 在指令后添加时间戳或随机数，确保 ConsumeLastAction 认为它是新指令
            string uniqueAction = $"ACTION:ROTATE_MIRROR:{DateTime.Now.Ticks}";
            bool success = NetworkManagerUDP.Instance.SendData(uniqueAction);
            
            if (success)
            {
                Debug.Log($"<color=green>[Mobile -> PC] 指令发送成功:</color> {uniqueAction}");
            }
            else
            {
                Debug.LogError($"<color=red>[Mobile -> PC] 指令发送失败！</color> 请检查网络连接或目标 IP 设置。");
            }

            _lastRotationTime = Time.time; // 更新冷却时间
        }
        else
        {
            Debug.LogError("[Mobile] 发送失败：NetworkManagerUDP.Instance 为空！");
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        OnMobileRotateButtonPressed();
    }

    void OnEnable()
    {
        _inputActions.Enable();
    }

    void OnDisable()
    {
        _inputActions.Disable();
    }

    void Start()
    {
        _initialRotation = transform.rotation;
        Debug.Log($"Initial Rotation: {_initialRotation}");

        // 检测 Unity Remote 连接状态
        if (Application.isEditor)
        {
            Debug.Log($"[Remote Debug] 当前运行平台: {Application.platform}");
#if UNITY_EDITOR
            Debug.Log($"[Remote Debug] 目标编译平台: {EditorUserBuildSettings.activeBuildTarget}");
#endif
            
            // 检查是否识别到 iOS 设备
            var devices = InputSystem.devices;
            Debug.Log("[Remote Debug] 已连接的输入设备列表:");
            foreach (var device in devices)
            {
                Debug.Log($"- 设备名称: {device.name}, 描述: {device.description}");
            }
        }
    }

    void Update()
    {
        // 只有在非网络输入模式（即手机端）下检测传感器
        if (!useNetworkInput)
        {
            // 动态检测并启用传感器，解决 Unity Remote 延迟连接问题
            EnsureSensorsEnabled();
            
            // 每隔 3 秒输出一次传感器状态，避免刷屏但保持监控
            if (Application.isEditor && Time.frameCount % 180 == 0)
            {
                var attitude = AttitudeSensor.current;
                bool isRemoteActive = attitude != null && attitude.enabled;
                Debug.Log($"[Remote Debug] 姿态传感器状态: {(isRemoteActive ? "已激活 (OK)" : "未激活 (WAITING)")}");
                
                if (attitude == null)
                {
                    Debug.LogWarning("[Remote Debug] 未检测到 AttitudeSensor。请检查：1.手机是否启动 Remote App 2.Build Settings 是否为 iOS 3.Project Settings > Editor > Device 是否选中设备");
                }
            }
        }

        UpdateFlashlightRotation();
        HandleNetworkActions();
        HandleDebugInput();
    }

    private void EnsureSensorsEnabled()
    {
        // 尝试启用 AttitudeSensor
        if (AttitudeSensor.current != null && !AttitudeSensor.current.enabled)
        {
            InputSystem.EnableDevice(AttitudeSensor.current);
            Debug.Log("AttitudeSensor 已动态激活");
        }

        // 尝试启用 Gyroscope (飞鼠效果需要)
        if (UnityEngine.InputSystem.Gyroscope.current != null && !UnityEngine.InputSystem.Gyroscope.current.enabled)
        {
            InputSystem.EnableDevice(UnityEngine.InputSystem.Gyroscope.current);
            Debug.Log("Gyroscope 已动态激活");
        }

        // 备选：启用重力感应器
        if (GravitySensor.current != null && !GravitySensor.current.enabled)
        {
            InputSystem.EnableDevice(GravitySensor.current);
            Debug.Log("GravitySensor 已动态激活");
        }
    }

    private void UpdateFlashlightRotation()
    {
        // 优先检查网络输入 (PC端逻辑)
        if (useNetworkInput && NetworkManagerUDP.Instance != null)
        {
            string data = NetworkManagerUDP.Instance.GetLastRotation();
            if (!string.IsNullOrEmpty(data) && data.StartsWith("ROT:"))
            {
                string[] parts = data.Substring(4).Split(',');
                if (parts.Length == 4)
                {
                    Quaternion remoteRot = new Quaternion(
                        float.Parse(parts[0]), float.Parse(parts[1]),
                        float.Parse(parts[2]), float.Parse(parts[3]));
                    
                    ApplyRotation(remoteRot);
                    return;
                }
            }
        }

        // 检查 AttitudeSensor 是否可用且已启用 (手机端逻辑)
        var attitudeSensor = AttitudeSensor.current;
        if (attitudeSensor != null && attitudeSensor.enabled)
        {
            Quaternion rawAttitude = attitudeSensor.attitude.ReadValue();
            
            // 如果是手机端且网络管理器存在，发送数据
            if (!useNetworkInput && NetworkManagerUDP.Instance != null)
            {
                bool success = NetworkManagerUDP.Instance.SendData($"ROT:{rawAttitude.x},{rawAttitude.y},{rawAttitude.z},{rawAttitude.w}");
                
                // 仅在状态变化或每隔一段时间输出，避免姿态数据刷屏
                if (Time.frameCount % 300 == 0) 
                {
                    if (success) Debug.Log("<color=cyan>[Mobile] 姿态数据同步中...</color>");
                    else Debug.LogWarning("<color=orange>[Mobile] 姿态数据同步失败，请检查网络。</color>");
                }
            }

            ApplyRotation(rawAttitude);
            return;
        }

        // 2. 使用 PlayerInputActions 的鼠标控制逻辑：跟随光标位置
        Vector2 mouseScreenPos = _inputActions.Player.Point.ReadValue<Vector2>();
        
        // if (Time.frameCount % 60 == 0) Debug.Log($"Mouse Screen Pos: {mouseScreenPos}");

        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("Missing Main Camera!");
            return;
        }

        // 将屏幕坐标转换为世界坐标
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(mainCam.transform.position.z)));
        mouseWorldPos.z = 0;

        // 直接平滑移动到目标位置
        transform.position = Vector3.Lerp(transform.position, mouseWorldPos, Time.deltaTime * smoothSpeed);
    }

    private void ApplyRotation(Quaternion rawAttitude)
    {
        // 应用初始姿态偏移
        Quaternion offsetRotation = Quaternion.Euler(attitudeOffset);
        Quaternion attitude = rawAttitude * offsetRotation;
        
        debugAttitudeEuler = attitude.eulerAngles; 
        
        Vector3 cameraDir = attitude * Vector3.forward;
        
        float yaw = Mathf.Atan2(cameraDir.x, -cameraDir.y) * Mathf.Rad2Deg;
        float pitch = Mathf.Asin(cameraDir.z) * Mathf.Rad2Deg;

        float rawX = yaw / 45f;   
        float rawY = pitch / 45f;
        
        debugRawXY = new Vector2(rawX, rawY); 

        float finalX = swapXY ? rawY : rawX;
        float finalY = swapXY ? rawX : rawY;

        finalX *= (invertX ? -1f : 1f);
        finalY *= (invertY ? -1f : 1f);
        debugFinalXY = new Vector2(finalX, finalY); 

        Vector3 targetWorldPos = new Vector3(finalX * sensitivity, finalY * sensitivity, 0);

        transform.position = Vector3.Lerp(transform.position, targetWorldPos, Time.deltaTime * smoothSpeed);

        ClampToScreen();
    }

    private void ClampToScreen()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 pos = mainCam.WorldToViewportPoint(transform.position);
        pos.x = Mathf.Clamp01(pos.x);
        pos.y = Mathf.Clamp01(pos.y);
        transform.position = mainCam.ViewportToWorldPoint(pos);
    }

    private void HandleNetworkActions()
    {
        if (!useNetworkInput || NetworkManagerUDP.Instance == null) return;

        // 使用 ConsumeLastAction 确保动作只执行一次
        string action = NetworkManagerUDP.Instance.ConsumeLastAction();
        if (!string.IsNullOrEmpty(action))
        {
            Debug.Log($"[PC] 收到网络指令: {action}");
            
            // 检查是否包含旋转指令
            if (action.StartsWith("ACTION:ROTATE_MIRROR"))
            {
                TryRotateMirror();
            }
        }
    }

    private void TryRotateMirror()
    {
        // 确定检测距离：如果勾选了使用光照半径，则从 Light2D 获取
        float finalDistance = interactionDistance;
        if (useLightRadiusAsDistance && flashlight != null)
        {
            finalDistance = flashlight.pointLightOuterRadius;
        }

        // 复用 Mirror 自身的检测逻辑：
        // 镜子只有在被照亮时（isIlluminated 为 true）才应该能被旋转
        // 我们通过 OverlapCircle 检测手电筒光照范围内的所有镜子
        Debug.Log($"[PC] 尝试检测范围内的镜子。位置: {transform.position}, 半径: {finalDistance}, 层级: {mirrorLayer.value}");
        
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, finalDistance, mirrorLayer);
        
        bool foundMirror = false;
        foreach (var col in colliders)
        {
            Debug.Log($"[PC] 范围内检测到物体: {col.name}, Tag: {col.tag}");
            Mirror mirror = col.GetComponent<Mirror>();
            
            if (mirror != null)
            {
                // 只有当镜子当前处于被照亮状态时，才允许旋转
                // 这保证了只有被手电筒“照到”的镜子才能被旋转，无论它在手电筒的哪个方向
                if (mirror.IsIlluminated)
                {
                    foundMirror = true;
                    Debug.Log($"[Interaction] 镜子 {mirror.name} 正被照亮，执行旋转");
                    mirror.RotateMirror();
                    // 如果你想一次只旋转一个，可以保留 break；如果想旋转范围内所有被照亮的镜子，可以去掉
                    break; 
                }
            }
        }

        if (!foundMirror)
        {
            Debug.LogWarning("[PC] 范围内未发现被照亮的 Mirror。请检查 Mirror 是否在光照半径内。");
        }
    }

    private void HandleDebugInput()
    {
        // PC端调试按键 R
        if (Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame)
        {
            Debug.Log("[Debug] PC端按下 R 键，尝试旋转镜子");
            TryRotateMirror();
        }
    }
}
