// # System
using System.Collections;
using System.Collections.Generic;

// # UnityEngine
using UnityEngine;
using UnityEngine.Rendering;


// # Photon
using Photon.Pun;

public class Player2Controller : MonoBehaviourPunCallbacks
{
    //private 변수
    private Camera mainCamera = null;
    private PlayerController playerOneController = null;

    //초기화
    private void Start()
    {
        mainCamera = Camera.main;

        Volume volume = GetComponentInChildren<Volume>();
        volume.enabled = photonView.IsMine;

        StartCoroutine(Co_WaitSetCamera());
    }

    //카메라 셋팅
    private IEnumerator Co_WaitSetCamera()
    {
        yield return new WaitUntil(() => GameManager.Instance.IsGameStart && GameManager.Instance.PlayerTransform != null);

        playerOneController = GameManager.Instance.PlayerTransform.GetComponent<PlayerController>();

        mainCamera.GetComponent<CameraController>().target = playerOneController.CameraTarget;

        transform.SetParent(playerOneController.gameObject.transform);
    }
}
