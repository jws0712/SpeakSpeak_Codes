// # System
using System.Collections;
using System.Collections.Generic;

// # UnityEngine
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("CameraSetting")]
    [SerializeField] private float rotSensitive     = default;
    [SerializeField] private float dis              = default;
    [SerializeField] private float RotationMin      = default;
    [SerializeField] private float RotationMax      = default;

    [HideInInspector] public Transform target = null;

    private Camera mainCamera = null;

    private float Yaxis = default;
    private float Xaxis = default;
    private Vector3 targetRotation = default;

    //초기화
    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (GameManager.Instance.IsGameOver || target == null) return;

        CameraMovement();
    }

    //마우스에 따라 카메라를 이동
    private void CameraMovement()
    {
        Yaxis += Input.GetAxis("Mouse X") * rotSensitive;
        Xaxis -= Input.GetAxis("Mouse Y") * rotSensitive;

        Xaxis = Mathf.Clamp(Xaxis, RotationMin, RotationMax);

        targetRotation = new Vector3(Xaxis, Yaxis);

        transform.eulerAngles = targetRotation;
        transform.position = target.position - transform.forward * dis;

        mainCamera.transform.LookAt(target.position);
    }
}
