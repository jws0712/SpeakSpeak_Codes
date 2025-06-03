// # System
using System.Collections;
using System.Collections.Generic;

// # Unity
using UnityEngine;

// # Photon
using Photon.Pun;

// # TextMeshPro
using TMPro;

// # Project
using EasyTransition;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("GameManagerInfo")]
    [SerializeField] private TextMeshProUGUI deathCountText      = null;
    [SerializeField] private TransitionSettings transition       = null;
    [SerializeField] private Transform defaultspawnPoint         = null;
    [SerializeField] private GameObject GameOverPanel            = null;
    [SerializeField] private GameObject testPanel                = null;
    [SerializeField] private float gameOverDurationTime          = default;

    //static ����
    public static GameManager Instance;

    //private ����
    private float deathCount         = default;

    private bool isGameOver          = default;
    private bool isGameClear         = default;
    private bool isGameStart         = default;
    private bool isGameCrash         = default;

    private Transform playerTransform = null;
    private Transform respawnPostiton = null;

    private MyRole myRole = null;

    //������Ƽ
    public bool IsGameOver  => isGameOver;
    public bool IsGameStart => isGameStart;
    public bool IsGameClear => isGameClear;
    public Transform PlayerTransform => playerTransform;

    public Transform RespawnPostiton { get{ return respawnPostiton; } set { respawnPostiton = value; } }

    //�̱���
    private void Awake()
    {
        Instance = this;

    }

    //�ʱ�ȭ
    private void Start()
    {
        PhotonNetwork.SendRate = 60;
        PhotonNetwork.SerializationRate = 30;

        myRole = FindAnyObjectByType<MyRole>();

        Cursor.visible   = false;
        Cursor.lockState = CursorLockMode.Locked;

        SoundManager.instance.StopMusic();
        SoundManager.instance.PlayMusic("GameMusic");

        respawnPostiton = defaultspawnPoint;

        isGameStart = true;

        if (myRole.RoleID == 0)
        {
            PhotonNetwork.Instantiate("Player_1", defaultspawnPoint.position, Quaternion.identity);
        }
        else
        {
            PhotonNetwork.Instantiate("Player_2", defaultspawnPoint.position, Quaternion.identity);
        }
    }

    private void Update()
    {
        if (!isGameCrash && PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            isGameCrash = true;

            TransitionManager.Instance().Transition("2. Duo Lobby", transition, 0);
        }
    }

    //�÷��̾� ��ġ�� �Ѱܹ���
    public void SetPlayerTransform(Transform playerTransform)
    {
        this.playerTransform = playerTransform;
    }

    public void OnLoadScene()
    {
        testPanel.SetActive(false);

        isGameStart = true;

        TransitionManager.Instance().onTransitionCutPointReached -= OnLoadScene;
    }

    //���� ����
    public void GameOver()
    {
        photonView.RPC("RPC_GameOver", RpcTarget.All);
    }

    //���� Ŭ����
    public void GameClear()
    {
        photonView.RPC("RPC_GameClear", RpcTarget.All);
    }

    [PunRPC]
    private void RPC_GameOver()
    {
        StartCoroutine(Co_GameOver());
    }

    [PunRPC]
    private void RPC_GameClear()
    {
        isGameClear = true;
        TransitionManager.Instance().Transition("4. GameClear", transition, 0);
    }



    //���ӿ��� �ڷ�ƾ
    private IEnumerator Co_GameOver()
    {
        isGameOver = true;

        deathCount += 1;
        deathCountText.text = deathCount.ToString();

        GameOverPanel.SetActive(true);

        playerTransform.gameObject.SetActive(false);

        EffectManager.Instance.PlayEffect(playerTransform.position, "GameOver");

        yield return new WaitForSeconds(gameOverDurationTime);

        GameOverPanel.SetActive(false);
        Respawn();
    }

    //�÷��̾� ������
    private void Respawn()
    {
        playerTransform.position = respawnPostiton.position;

        EffectManager.Instance.PlayEffect(respawnPostiton.position, "Respawn");
        EffectManager.Instance.PlayEffect(respawnPostiton.position, "CheckPoint");

        playerTransform.gameObject.SetActive(true);

        isGameOver = false;
    }
}