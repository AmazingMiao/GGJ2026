using UnityEngine;

public class MirrorButton : MonoBehaviour
{
    [SerializeField] private Mirror targetMirror;

    public void OnButtonPressed()
    {
        if (targetMirror != null)
        {
            targetMirror.RotateMirror();
        }
    }

    // If using a simple trigger for the button
    private void OnMouseDown()
    {
        OnButtonPressed();
    }
}
