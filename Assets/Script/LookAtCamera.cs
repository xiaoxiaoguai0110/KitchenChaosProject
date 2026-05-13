using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    public enum Mode
    {
        LookAt,
        LookAtInverted,
        CameraForward,
        CameraForwardInvert
    }

    [SerializeField]private Mode mode;


    private void Update()
    {
        switch (mode)
        {
            case Mode.LookAt:
                transform.LookAt(Camera.main.transform);
                break;
            case Mode.LookAtInverted:
                transform.LookAt(transform.position - Camera.main.transform.position + transform.position);
                break;
            case Mode.CameraForward:
                transform.forward = Camera.main.transform.forward;
                break;
            case Mode.CameraForwardInvert:
                transform.forward = -Camera.main.transform.forward;
                break;
            default:
                break;
        }
    }
}
