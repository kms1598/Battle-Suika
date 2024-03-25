using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using static AudioManager;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("MainPanel")]
    public GameObject mainPanel;
    public TMP_InputField nickNameInput;

    [Header("LobbyPanel")]
    public GameObject lobbyPanel;
    public Button[] cellBtn;

    [Header("RoomPanel")]
    public GameObject roomPanel;
    public TMP_Text roomNameText;
    public Button startBtn;
    public GameObject[] players;
    public TMP_Text[] playerNameText;
    public TMP_Text[] chatText;
    public TMP_InputField chatInput;

    [Header("ETC")]
    public PhotonView PV;
    RoomInfo[] roomList = new RoomInfo[4];

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        Screen.SetResolution(1920, 1080, false);

        if (PhotonNetwork.InRoom) //�濡 �ִ� ����� ���� ������ ���ƿ��� ���
        {
            SetRoom();
            mainPanel.SetActive(false);
            lobbyPanel.SetActive(false);
            roomPanel.SetActive(true);
        }
        else
        {
            mainPanel.SetActive(true);
            lobbyPanel.SetActive(false);
            roomPanel.SetActive(false);
        }
    }

    public void Connect() //���� ����
    {
        if(nickNameInput.text != "") //�г��� �Է� �ʼ�
            PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster() => PhotonNetwork.JoinLobby();

    public override void OnJoinedLobby() //�κ� ������ �����
    {
        mainPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        roomPanel.SetActive(false);
        
        if(nickNameInput.text != "") //���� ������ ���� ������ ���ƿö� �г����� ������� ���� ����
            PhotonNetwork.LocalPlayer.NickName = nickNameInput.text;

        for (int i = 0; i < cellBtn.Length; i++) roomList[i] = null;
    }

    public void Disconnect() => PhotonNetwork.Disconnect(); //���� ����

    public override void OnDisconnected(DisconnectCause cause)
    {
        mainPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(false);
    }//���� ������ �� �ǳ� ����

    public void CreateRoom(int roomNum) //�� �����
    {
        PhotonNetwork.JoinOrCreateRoom("Room" + roomNum, new RoomOptions { MaxPlayers = 2 }, null);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnJoinedRoom() //�濡 ������ ��� �÷��̾ ����
    {
        SetRoom();
    }

    void SetRoom() //���� ���� ����
    {
        roomPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        UpdateRoomInfo();
        RoomRenewal();
        chatInput.text = "";
        for (int i = 0; i < chatText.Length; i++) chatText[i].text = ""; //ä��â ����
    }

    void RoomRenewal() //�÷��̾� ���� ������Ʈ
    {
        foreach(GameObject player in players)
        {
            player.SetActive(false);
        }
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            players[i].SetActive(true);
            playerNameText[i].text = PhotonNetwork.PlayerList[i].NickName;
        }

        if(PhotonNetwork.IsMasterClient)
        {
            startBtn.gameObject.SetActive(true);

            if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
                startBtn.interactable = true;
            else
                startBtn.interactable = false;
        }
        else
        {
            startBtn.gameObject.SetActive(false);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) //������ ����
    {
        RoomRenewal();
        PV.RPC("ChatRPC", RpcTarget.All, "<color=yellow>" + newPlayer.NickName + " joined game</color>");
        UpdateRoomInfo();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer) //������ ����
    {
        RoomRenewal();
        PV.RPC("ChatRPC", RpcTarget.All, "<color=yellow>" + otherPlayer.NickName + " leaved game</color>");
        UpdateRoomInfo();
    }

    public void Send()
    {
        string msg = PhotonNetwork.NickName + ": " + chatInput.text;
        PV.RPC("ChatRPC", RpcTarget.All, msg);
        PV.RPC("RPCPlayChatSfx", RpcTarget.Others);
        chatInput.text = "";
    }
    [PunRPC]
    void RPCPlayChatSfx()
    {
        AudioManager.instance.PlayerSfx(AudioManager.Sfx.Click);
    }
    [PunRPC]
    void ChatRPC(string msg) //ä�� ����
    {
        bool isInput = false;
        for(int i = 0; i < chatText.Length; i++)
        {
            if (chatText[i].text == "")
            {
                isInput = true;
                chatText[i].text = msg;
                break;
            }
        }
        if(!isInput)
        {
            for(int i = 1; i < chatText.Length; i++)
            {
                chatText[i-1].text = chatText[i].text;
            }
            chatText[chatText.Length - 1].text = msg;
        }
    }

    void LobbyRenewal() //�κ񿡼��� �� ���� ������Ʈ
    {
        for(int i = 0 ; i < cellBtn.Length ; i++)
        {
            if (roomList[i] != null)
            {
                cellBtn[i].transform.GetChild(1).GetComponent<TMP_Text>().text = roomList[i].PlayerCount + "/" + roomList[i].MaxPlayers;
            }
            else
            {
                cellBtn[i].transform.GetChild(1).GetComponent<TMP_Text>().text = "0/2";
            }
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList) //������ �κ�� ���ƿö��� ������ �Ǽ� �濡 �� ���� ���� �����ع���
    {
        int roomCount = roomList.Count;
        for(int i = 0; i < roomCount ; i++)
        {
            if (!roomList[i].RemovedFromList)
            {
                this.roomList[roomList[i].Name[4] - '1'] = roomList[i];
            }
            else
            {
                this.roomList[roomList[i].Name[4] - '1'] = null;
            }
        }
        LobbyRenewal();
    }

    public void UpdateRoomInfo()
    {
        RoomInfo roomInfo = PhotonNetwork.CurrentRoom;
        roomList[roomInfo.Name[4] - '1'] = roomInfo;
        LobbyRenewal();
    }

    public void StartGame()
    {
        PV.RPC("RPCStartGame",  RpcTarget.Others);
        PhotonNetwork.LoadLevel(1);
    }
    [PunRPC]
    public void RPCStartGame()
    {
        PhotonNetwork.LoadLevel(1);
    }
}
