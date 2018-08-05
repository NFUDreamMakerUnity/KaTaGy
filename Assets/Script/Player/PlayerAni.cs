﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PlayerAni : Photon.MonoBehaviour
{
    protected SmoothFollow cameraControl;
    protected Player player;
    protected Animator anim;

    [Header("武器")]
   // public GameObject weapon;
    public MeshRenderer swordRecyclePos;   //回收武器的地方
    public MeshRenderer pullSwordPos;      //拔起武器的地方
    public Transform weapon_Detect;     //武器攻擊判斷中心
    //public Transform weapon_Detect_Hand; //第2個攻擊判斷區域(艾倫手)
    public LayerMask canAtkMask;

    [Header("Combo")]
    public bool canClick = true;
    //後續combo被點擊
    public bool nextComboBool;
    //前搖點
    protected bool brfore_shaking;
    //後搖點
    protected bool after_shaking;
    protected bool stop_Ani;

    protected bool startDetect_1 = false;
    protected bool startDetect_2 = false;
    protected bool startDetect_3 = false;
    public GameObject[] swordLight = new GameObject[3];
    public List<GameObject> alreadyDamage;

    protected Vector3 nextAtkPoint;
    protected Vector3 currentAtkDir;

    protected int comboIndex;
    protected float beHit_time = 0.25f;
    protected bool canStiffness = true;

    protected Tweener myTweener;

    private void Start()
    {
        anim = GetComponent<Animator>();
        player = GetComponent<Player>();

        cameraControl = SmoothFollow.instance;
    }

    /* private void Update()
     {
         DetectAtkRanage();
     }*/

    #region 武器切換
    [PunRPC]
    public void weaponOC(bool _t)
    {
        anim.SetBool("NowBuild", _t);
        anim.SetTrigger("Switch");
    }

    public void WeaponChangePos(int _n)
    {
        switch (_n)
        {
            //武器回背上
            case (0):
                /*     weapon.transform.SetParent(swordRecyclePos);
                     weapon.transform.localPosition = new Vector3(0, 0, 0);
                     weapon.transform.localEulerAngles = new Vector3(0, 0, 0);
                     weapon.transform.localScale = new Vector3(1, 1, 1);*/
                swordRecyclePos.enabled = true;
                pullSwordPos.enabled = false;

                break;
            //武器回手上
            case (1):
                /* weapon.transform.SetParent(pullSwordPos);
                 weapon.transform.localPosition = new Vector3(0, 0, 0);
                 weapon.transform.localEulerAngles = new Vector3(0, 0, 0);
                 weapon.transform.localScale = new Vector3(1, 1, 1);*/
                swordRecyclePos.enabled = false;
                pullSwordPos.enabled = true;
                break;
            //玩家不可移動
            case (2):
                player.stopAnything_Switch(true);
                break;
            //玩家可移動,模式可在切換
            case (3):
                player.stopAnything_Switch(false);
                player.StopClick = false;
                break;
        }
    }
    #endregion

    #region 被攻擊僵直反饋
    public void beOtherHit()
    {
        if (!player.deadManager.checkDead)
        {
            if (canStiffness)
            {
                canStiffness = false;
                StartCoroutine(player.MatchTimeManager.SetCountDown(StiffnessEnd, beHit_time));
            }
        }
    }
    void StiffnessEnd()
    {
        Debug.Log("重製");
        canStiffness = true;
        player.stopAnything_Switch(false);
    }
    #endregion

    //取消動作Ani
    protected void CancleAllAni()
    {
        ComboAniEnd();
        SwitchAtkRange(8);
        Ani_Run(false);
    }

    public void GoBackIdle_canMove()
    {
        ComboAniEnd();
        Ani_Run(false);
        player.stopAnything_Switch(false);
    }

    void ComboAniEnd()
    {
        comboIndex = 0;
        anim.SetInteger("comboIndex", 0);
        canClick = true;
        nextComboBool = false;
        after_shaking = false;
        brfore_shaking = false;
        if (myTweener != null)
            myTweener.Kill();
    }

    #region 閃避
    [PunRPC]
    public void GoDodge(Vector3 _dir)
    {
        CancleAllAni();
        anim.SetTrigger("Dodge");
        GoMovePoint(_dir, 18f, .25f, Ease.OutExpo);
    }
    #endregion

    #region Combo
    //按下判斷
    public virtual void TypeCombo(Vector3 atkDir)
    {

    }
    //動畫播放間判定
    public virtual void comboCheck(int _n)
    {

    }
    //角色攻擊位移
    public virtual void GoMovePos(int _t)
    {

    }

    protected void GoMovePoint(Vector3 _dir, float _range, float _time, Ease _track)
    {
        nextAtkPoint = transform.localPosition + _dir.normalized * _range;
        nextAtkPoint.y = transform.localPosition.y;
        myTweener = transform.DOMove(nextAtkPoint, _time).SetEase(_track);
        myTweener.OnUpdate(stopMove);
    }

    protected void GoBackPoint(Vector3 _dir, float _range, float _time, Ease _track)
    {
        nextAtkPoint = transform.localPosition - _dir.normalized * _range;
        nextAtkPoint.y = transform.localPosition.y;
        myTweener = transform.DOMove(nextAtkPoint, _time).SetEase(_track);
        myTweener.OnUpdate(stopMoveBack);
    }
    #endregion

    #region Combo傳輸
    //第一次按下
    protected void comboFirst(int Index, Vector3 Dir)
    {
        comboIndex = Index;
        anim.SetInteger("comboIndex", comboIndex);
        anim.SetTrigger("Combo");

        currentAtkDir = Dir.normalized;
        player.Net.RPC("TP_Combo", PhotonTargets.Others, comboIndex);
    }
    //之後按下的
    protected void Nextcombo(int Index)
    {
        comboIndex = Index;
        nextComboBool = true;
        if (after_shaking && photonView.isMine)
            goNextCombo();
    }
    //前往下個combo
    protected void goNextCombo()
    {
        anim.SetInteger("comboIndex", comboIndex);
        anim.SetBool("Action", true);
        nextComboBool = false;
        after_shaking = false;
        brfore_shaking = false;
        player.Net.RPC("TP_Combo", PhotonTargets.Others, comboIndex);
    }

    [PunRPC]
    protected void TP_Combo(int _i)
    {
        if (!photonView.isMine)
            alreadyDamage.Clear();
        anim.CrossFade("combo" + _i, 0.07f, 0);
    }
    #endregion

    #region 偵測攻擊最遠範圍
    protected RaycastHit hit;
    protected void stopMove()
    {
      //  if (player.MySkill != Player.skillData.Dodge)
            RedressDir();

        if (Physics.BoxCast(detectStartPos.position, new Vector3(2f, 4, 0.2f), detectStartPos.forward, out hit, detectStartPos.rotation, 7.0f, farDistance))
        {
            myTweener.Kill();
        }
    }

    protected void stopMoveBack()
    {
        if (Physics.BoxCast(detectStartPos.position, new Vector3(2f, 4, 0.2f), -detectStartPos.forward, out hit, detectStartPos.rotation, 1.5f, farDistance))
        {
            myTweener.Kill();
        }
    }

    void RedressDir()
    {
        Collider[] Enemy = Physics.OverlapSphere(transform.position, viewRadius, canAtkMask);
        if (Enemy.Length != 0)
        {
            for (int i = 0; i < Enemy.Length; i++)
            {
                Transform target = Enemy[i].transform;
                Vector3 dirToTarget = (target.position - transform.position).normalized;
                if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
                {
                    player.CharacterRot = Quaternion.LookRotation(dirToTarget.normalized);
                    transform.rotation = player.CharacterRot;
                    currentAtkDir = dirToTarget.normalized;
                    Debug.Log("矯正結束");
                    break;
                }
            }
        }
    }
    public float viewRadius;
    [Range(0, 360)]
    public int viewAngle;
    public Transform detectStartPos;
    public LayerMask farDistance;
    /// <summary>
    /// editor觀看用
    /// </summary>
    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
    #endregion

    [PunRPC]
    public void waitBuild(bool _t)
    {
        anim.SetBool("Building", _t);
    }

    //攻擊區間傷害判斷
    public virtual void DetectAtkRanage()
    {

    }
    //給予正確目標傷害
    protected virtual void GetCurrentTarget(Collider[] _enemies)
    {
        
    }
    //目前傷害判定區及刀光特效
    public virtual void SwitchAtkRange(int _n)
    {

    }

    #region 檢查敵人是否已得到傷害 
    protected bool checkIf(GameObject _enemy)
    {
        if (alreadyDamage.Contains(_enemy))
            return true;
        else
            return false;
    }
    #endregion

    #region 擊退
    [PunRPC]
    public void pushOtherTarget(Vector3 _dir, float _dis)
    {
        CancleAllAni();
        myTweener = transform.DOMove(transform.localPosition + _dir.normalized * _dis, .6f).SetEase(Ease.OutBack);
        myTweener.OnUpdate(stopMoveBack);
    }
    #endregion

    [Header("觀看傷害碰撞使用")]
    public GameObject test;
    public GameObject test2;
    public GameObject test888;
    public GameObject testFinal;
    /*private void OnDrawGizmos()
    {
        test.transform.position = weapon_Detect.position;
        test.transform.rotation = weapon_Detect.rotation;

        testFinal.transform.position = weapon_Detect.position;
        testFinal.transform.rotation = weapon_Detect.rotation;

        test2.transform.position = weapon_Detect_Hand.position;
        test2.transform.rotation = weapon_Detect_Hand.rotation;

        test888.transform.position = detectStartPos.position;
        test888.transform.rotation = detectStartPos.rotation;

        //Gizmos.color = Color.red;
        //Gizmos.DrawLine(detectStartPos.position, detectStartPos.position+ detectStartPos.forward* maxDistance);
       
    }*/

    [PunRPC]
    public void Ani_Run(bool isRun)
    {
        anim.SetBool("Run", isRun);        
    }

    [PunRPC]
    public void Die()
    {
        anim.SetTrigger("Die");
    }
}