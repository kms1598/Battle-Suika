using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class Circle : MonoBehaviourPunCallbacks
{
    public PhotonView PV;
    public Vector3 spawnpoint;
    int wallDistance = 3;
    float wallThickness = 0.1f;
    float speed = 0.5f;
    public int level;
    const int maxLevel = 8;
    public bool isMerge = false;
    Animator anim;
    CircleCollider2D collider;
    Rigidbody2D rigid;
    Coroutine isMake = null;
    float deadTime = 0;

    private void Start()
    {
        anim = GetComponent<Animator>();
        collider = GetComponent<CircleCollider2D>();
        rigid = GetComponent<Rigidbody2D>();

        if (PV.IsMine)
        {
            if(level == -1)
            {
                float rand = Random.Range(5, 16) / 10;
                transform.localScale = new Vector3(rand, rand, 0);
                PV.RPC("RPCRockSetting", RpcTarget.AllBuffered);
            }
            else
            {
                level = Random.Range(0, 4);
                anim.SetInteger("level", level);
            }
        }
    }

    [PunRPC]
    void RPCRockSetting()
    {
        GetComponent<SpriteRenderer>().color = Color.gray;
    }

    // Update is called once per frame
    void Update()
    {
        if(!GameManager.instance.isGameover && PV.IsMine && !rigid.simulated)
        {
            Move();

            if(Input.GetMouseButtonUp(0))
            {
                Drop();
            }
        }
    }

    void Move()
    {
        Vector3 mousepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float leftBorder = spawnpoint.x - wallDistance + (wallThickness + transform.localScale.x) / 2;
        float rightBorder = spawnpoint.x + wallDistance - (wallThickness + transform.localScale.x) / 2;

        if (mousepos.x < leftBorder)
        {
            mousepos.x = leftBorder;
        }
        else if (mousepos.x > rightBorder)
        {
            mousepos.x = rightBorder;
        }

        mousepos.y = spawnpoint.y;
        mousepos.z = spawnpoint.z;
        transform.position = Vector3.Lerp(transform.position, mousepos, speed);
    }

    public void Drop()
    {
        rigid.simulated = true;
        AudioManager.instance.PlayerSfx(AudioManager.Sfx.Drop);
        GameManager.instance.userTime = 5.5f;
        isMake = StartCoroutine(MakeNewCircle());
    }

    IEnumerator MakeNewCircle()
    {
        yield return new WaitForSeconds(0.5f);

        GameManager.instance.MakeCircle();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Circle")
        {
            Circle other = collision.gameObject.GetComponent<Circle>();

            if(level >= 0 && level == other.level && !isMerge && !other.isMerge && level < maxLevel)
            {
                float myX = transform.position.x;
                float myY = transform.position.y;
                float otherX = other.transform.position.x;
                float otherY = other.transform.position.y;

                if(myY < otherY || (myY == otherY && myX > otherX))
                {
                    other.Hide(new Vector3(myX, myY, 0));
                    LevelUp();
                }
            }
        }
    }

    void Hide(Vector3 targetPos)
    {
        rigid.velocity = Vector2.zero;
        rigid.angularVelocity = 0;
        isMerge = true;
        collider.enabled = false;

        StartCoroutine(HideRoutine(targetPos));
    }

    IEnumerator HideRoutine(Vector3 targetPos)
    {
        for(int frame = 0; frame < 30;  frame++)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, 0.5f);
            yield return null;
        }

        PhotonNetwork.Destroy(this.gameObject);
    }

    void LevelUp()
    {
        isMerge = true;

        rigid.velocity = Vector2.zero;
        rigid.angularVelocity = 0;

        StartCoroutine(LevelUpRoutine());
    }

    IEnumerator LevelUpRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        anim.SetInteger("level", level + 1);
        AudioManager.instance.PlayerSfx(AudioManager.Sfx.LevelUp);
        yield return new WaitForSeconds(0.2f);

        level++;
        GameManager.instance.SetGauge(level * 0.05f);
        isMerge = false;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (GameManager.instance.isGameover) return;

        if(collision.tag == "Line")
        {
            deadTime += Time.deltaTime;
        }

        if(deadTime >= 2)
        {
            GameManager.instance.PV.RPC("RPCGameOver", RpcTarget.AllBuffered, PhotonNetwork.IsMasterClient ? 1 : 0, PhotonNetwork.PlayerList[PhotonNetwork.IsMasterClient ? 0 : 1].NickName + "님이 라인을 넘었습니다!");
        }
    }
}
