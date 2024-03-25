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

        if (PhotonNetwork.InRoom) //방에 있는 사람이 메인 씬으로 돌아오는 경우
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

    public void Connect() //서버 연결
    {
        if(nickNameInput.text != "") //닉네임 입력 필수
            PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster() => PhotonNetwork.JoinLobby();

    public override void OnJoinedLobby() //로비에 들어오면 실행됨
    {
        mainPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        roomPanel.SetActive(false);
        
        if(nickNameInput.text != "") //게임 씬에서 메인 씬으로 돌아올때 닉네임이 사라지는 것을 방지
            PhotonNetwork.LocalPlayer.NickName = nickNameInput.text;

        for (int i = 0; i < cellBtn.Length; i++) roomList[i] = null;
    }

    public void Disconnect() => PhotonNetwork.Disconnect(); //연결 끊기

    public override void OnDisconnected(DisconnectCause cause)
    {
        mainPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(false);
    }//연결 끊었을 때 판넬 설정

    public void CreateRoom(int roomNum) //방 만들기
    {
        PhotonNetwork.JoinOrCreateRoom("Room" + roomNum, new RoomOptions { MaxPlayers = 2 }, null);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnJoinedRoom() //방에 들어오면 방과 플레이어를 설정
    {
        SetRoom();
    }

    void SetRoom() //방의 정보 정리
    {
        roomPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;
        UpdateRoomInfo();
        RoomRenewal();
        chatInput.text = "";
        for (int i = 0; i < chatText.Length; i++) chatText[i].text = ""; //채팅창 비우기
    }

    void RoomRenewal() //플레이어 정보 업데이트
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

    public override void OnPlayerEnteredRoom(Player newPlayer) //누군가 입장
    {
        RoomRenewal();
        PV.RPC("ChatRPC", RpcTarget.All, "<color=yellow>" + newPlayer.NickName + " joined game</color>");
        UpdateRoomInfo();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer) //누군가 퇴장
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
    void ChatRPC(string msg) //채팅 구현
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

    void LobbyRenewal() //로비에서의 방 정보 업데이트
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

    public override void OnRoomListUpdate(List<RoomInfo> roomList) //왜인지 로비로 돌아올때만 실행이 되서 방에 들어갈 때는 따로 구현해버림
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
