using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager instance;
    public PhotonView PV;

    public GameObject[] prefabs;
    public GameObject makedCircle;
    public TMP_Text[] playerNames;
    public Slider[] gaugeUI;
    public Image[] skillUI;
    public Image[] blind;
    public bool isGameover = false;
    public Image gameover;
    public Button leaveBtn;
    public Image timelimit;
    const float time = 5;
    public float userTime = time;
    public Image skill1Blind;
    bool canSkill1 = true;
    const int skill1CoolTime = 1;
    public Image skill2Blind;
    bool canSkill2 = true;
    const int skill2CoolTime = 15;

    int isRock = 0;

    int[] skillNum = new int[2];

    Vector3 makepoint;

    void Awake()
    {
        Application.targetFrameRate = 60;
        Time.timeScale = 1;

        if (instance == null)
            instance = this;

        DefaultPool pool = PhotonNetwork.PrefabPool as DefaultPool;

        if(pool != null && this.prefabs != null)
        {
            foreach(GameObject prefab in prefabs)
            {
                if(!pool.ResourceCache.ContainsKey(prefab.name))
                    pool.ResourceCache.Add(prefab.name, prefab);
            }
        }

        for(int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            playerNames[i].text = PhotonNetwork.PlayerList[i].NickName;
            gaugeUI[i].value = 0;
            skillNum[i] = 0;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            makepoint = new Vector3(-5, 3, 0);
        }
        else
        {
            makepoint = new Vector3(5, 3, 0);
        }


        makedCircle = PhotonNetwork.Instantiate("Circle", makepoint, Quaternion.identity);
        makedCircle.GetComponent<Circle>().spawnpoint = makepoint;
    }
    private void Update()
    {
        if(isGameover) return;
        
        Skill(PhotonNetwork.IsMasterClient ? 0 : 1);

        userTime -= Time.deltaTime;
        timelimit.fillAmount = userTime / 5;

        if(userTime <= 0)
        {
            makedCircle.GetComponent<Circle>().Drop();
        }

        skill1Blind.gameObject.SetActive(!canSkill1);
        skill2Blind.gameObject.SetActive(!canSkill2);
    }

    public void MakeCircle()
    {
        makedCircle = PhotonNetwork.Instantiate("Circle", makepoint, Quaternion.identity);
        makedCircle.GetComponent<Circle>().spawnpoint = makepoint;

        if(isRock > 0)
        {
            makedCircle.GetComponent<Circle>().level = -1;
            isRock--;
        }
    }

    public void SetGauge(float score)
    {
        PV.RPC("RPCSetGauge", RpcTarget.AllBuffered, PhotonNetwork.IsMasterClient, score);
    }

    [PunRPC]
    void RPCSetGauge(bool isMaster, float score)
    {
        int index = isMaster ? 0 : 1;

        if (gaugeUI[index].value + score >= 1 && skillNum[index] < 5)
        {
            gaugeUI[index].value = gaugeUI[index].value + score - 1;
            AudioManager.instance.PlayerSfx(AudioManager.Sfx.SkillGauge);
            PV.RPC("RPCSetSkillNum", RpcTarget.AllBuffered, index, 1);
        }
        else
        {
            gaugeUI[index].value += score;
        }
    }

    [PunRPC]
    void RPCSetSkillNum(int index, int num)
    {
        skillNum[index] += num;

        if (skillNum[index] >= 5)
            skillNum[index] = 5;

        PV.RPC("RPCSetSkillUI", RpcTarget.AllBuffered, index);
    }
    [PunRPC]
    void RPCSetSkillUI(int index)
    {
        for (int i = 0 + index * 5; i < skillNum[index] + index * 5; i++)
        {
            skillUI[i].GetComponent<Image>().color = Color.red;
        }
        for (int i = skillNum[index] + index * 5; i < (index + 1) * 5; i++)
        {
            skillUI[i].GetComponent<Image>().color = Color.white;
        }
    }
    public void GameOver()
    {
        PV.RPC("RPCGameOver", RpcTarget.Others);
        PhotonNetwork.LoadLevel(0);
    }
    [PunRPC]
    public void RPCGameOver()
    {
        PhotonNetwork.LoadLevel(0);
    }

    void Skill(int index)
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (skillNum[index] >= 1 && canSkill1)
            {
                canSkill1 = false;
                PV.RPC("RPCSetSkillNum", RpcTarget.AllBuffered, index, -1);
                PV.RPC("RPCSkill1", RpcTarget.Others);
                PV.RPC("RPCPlaySfx", RpcTarget.AllBuffered, AudioManager.Sfx.Skill1);
                StartCoroutine(UseSkill1());
            }
        }
        else if(Input.GetKeyDown(KeyCode.W))
        {
            if (skillNum[index] >= 3 && canSkill2)
            {
                canSkill2 = false;
                PV.RPC("RPCSetSkillNum", RpcTarget.AllBuffered, index, -3);
                PV.RPC("RPCSkill2", RpcTarget.AllBuffered, (index + 1) % 2);
                PV.RPC("RPCPlaySfx", RpcTarget.AllBuffered, AudioManager.Sfx.Skill2);
                StartCoroutine(UseSkill2());
            }
        }

        if (skillNum[index] >= 5)
        {
            PV.RPC("RPCGameOver", RpcTarget.AllBuffered, PhotonNetwork.IsMasterClient ? 0 : 1, PhotonNetwork.PlayerList[PhotonNetwork.IsMasterClient ? 0 : 1].NickName + " 님이 스킬 포인트 다섯 개를 모았습니다!");
        }
    }

    IEnumerator UseSkill1() //ref를 못쓴대서 코루틴 함수 2개 이용
    {
        yield return new WaitForSeconds(skill1CoolTime);

        canSkill1 = true;
    }

    IEnumerator UseSkill2()
    {
        yield return new WaitForSeconds(skill2CoolTime);

        canSkill2 = true;
    }

    [PunRPC]
    void RPCSkill1()
    {
        this.isRock++;
    }

    [PunRPC]
    void RPCSkill2(int target)
    {
        StartCoroutine(Skill2(target));
    }

    IEnumerator Skill2(int target)
    {
        blind[target].gameObject.SetActive(true);
        yield return new WaitForSeconds(skill2CoolTime);
        blind[target].gameObject.SetActive(false);
    }

    [PunRPC]
    void RPCPlaySfx(AudioManager.Sfx sfx)
    {
        AudioManager.instance.PlayerSfx(sfx);
    }

    [PunRPC]
    void RPCGameOver(int winner, string whyWin)
    {
        PV.RPC("RPCPlaySfx", RpcTarget.AllBuffered, AudioManager.Sfx.GameOver);
        gameover.transform.GetChild(1).GetComponent<TMP_Text>().text = PhotonNetwork.PlayerList[winner].NickName;
        gameover.transform.GetChild(2).GetComponent<TMP_Text>().text = whyWin;
        gameover.gameObject.SetActive(true);
        leaveBtn.interactable = PhotonNetwork.IsMasterClient;
        isGameover = true;
    }
}