using UnityEngine;

public class MobileControls : MonoBehaviour
{
    // 在 StationaryFlashlight.cs 或新脚本中添加
    [SerializeField] private GameObject mobileUIRoot;

    void Start() {
        // 仅在移动端平台激活 UI
        bool isMobile = Application.isMobilePlatform || Application.platform == RuntimePlatform.IPhonePlayer;
        if (mobileUIRoot != null) {
            mobileUIRoot.SetActive(isMobile);
        }
    }

}
