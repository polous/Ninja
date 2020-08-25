﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class Player : MonoBehaviour
{
    public float moveSpeed; // базовая скорость перемещения игрока
    public float rageSpeed; // скорость перемещения игрока во время ярости
    public float rageTime;
    [HideInInspector] public float timerForRage;
    float s; // текущая скорость игрока

    public float collDamage;
    public float rageCollDamage;

    public float maxEnergy; // максимальный запас энергии
    public float matrixEnergyDrop; // расход энергии на вход в матрицу
    public float energyRecoveryPerSec; // восстановление энергии в секунду вне матрицы
    [HideInInspector] public float curEnergy; // текущий запас энергии
    float sPrev, sCur;
    public float maxHealthPoint; // максимальный запас здоровья
    public float curHealthPoint; // текущий запас здоровья
    [HideInInspector] public Transform healthPanel;
    [HideInInspector] public HealthPanel healthPanelScript;

    Transform CamTarget;
    [HideInInspector] public Main main;
    Joystick joy;
    RaycastHit RChit;
    [HideInInspector] public Collider coll;

    public Color bodyColor;
    [HideInInspector] public MaterialPropertyBlock MPB;
    [HideInInspector] public MeshRenderer mr;

    public bool inMatrix;

    public bool startMoving;
    public Vector3 moveDirection;


    Vector3 normal = Vector3.zero;



    public void StartScene()
    {
        inMatrix = false;
        startMoving = false;

        MPB = new MaterialPropertyBlock();
        mr = GetComponentInChildren<MeshRenderer>();
        mr.GetPropertyBlock(MPB);
        MPB.SetColor("_Color", bodyColor);
        mr.SetPropertyBlock(MPB);

        coll = GetComponent<Collider>();

        CamTarget = GameObject.Find("CamTarget").transform;
        // располагаем игрока в его стартовой позиции на сцене
        transform.position = main.playerSpawnPoint.position;
        transform.rotation = Quaternion.identity;
        s = 0;
        curEnergy = maxEnergy;
        curHealthPoint = maxHealthPoint;
        UIRefresh(curEnergy);

        joy = main.joy;
    }

    // обновление UI
    public void UIRefresh(float curEnergy)
    {
        main.EnergySlider.fillAmount = curEnergy / maxEnergy;
        main.EnergyCount.text = curEnergy.ToString("F0");
    }

    private void Update()
    {
        if (main == null) return;

        if (healthPanel != null) healthPanel.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);

        if (!main.readyToGo) return;

        sPrev = s;
        // определяем и задаем направление движения
        Vector3 direction = Vector3.forward * joy.Vertical + Vector3.right * joy.Horizontal;
        if (direction.magnitude == 0) s = 0;
        else s = moveSpeed;
        sCur = s;

        // определяем, хватает ли игроку энергии для начала движения
        //if (sPrev == 0 && sCur > 0)
        //{
        //    if (curEnergy >= matrixEnergyDrop)
        //    {
        //        if (inMatrix == false)
        //        {
        //            inMatrix = true;
        //            main.ToneMap.enabled = true;
        //            main.curSlowerCoeff = main.matrixCoeff;
        //        }
        //    }
        //}

        //if (curEnergy >= matrixEnergyDrop)
        //{
        //    if (inMatrix == false)
        //    {
        //        print("");
        //        inMatrix = true;
        //        main.ToneMap.enabled = true;
        //        main.curSlowerCoeff = main.matrixCoeff;
        //    }
        //}

        // двигаем игрока в заданном направлении        
        if (moveDirection != Vector3.zero)
        {
            if (timerForRage > 0)
            {
                transform.position += moveDirection * rageSpeed * Time.deltaTime * main.curSlowerCoeff;

                timerForRage -= Time.deltaTime * main.curSlowerCoeff;

                if (timerForRage <= 0)
                {
                    mr.GetPropertyBlock(MPB);
                    MPB.SetColor("_Color", bodyColor);
                    mr.SetPropertyBlock(MPB);

                    timerForRage = -1;
                }
            }
            if (timerForRage < 0)
            {
                transform.position += moveDirection * moveSpeed * Time.deltaTime * main.curSlowerCoeff;
            }

            if (Physics.SphereCast(coll.bounds.center, 0.4f, moveDirection, out RChit, 1, 1 << 9))
            {
                normal = RChit.normal;
            }
        }

        if (curEnergy < maxEnergy)
        {
            curEnergy += energyRecoveryPerSec * Time.deltaTime * main.curSlowerCoeff;
            if (curEnergy > maxEnergy) curEnergy = maxEnergy;
            UIRefresh(curEnergy);
        }
    }

    void LateUpdate()
    {
        // тянем камеру за игроком (только по вертикали)
        CamTarget.position = new Vector3(CamTarget.position.x, CamTarget.position.y, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Wall")
        {
            //Debug.DrawRay(RChit.point, -moveDirection, Color.red, 20);

            moveDirection = Vector3.Reflect(moveDirection, normal).normalized;

            transform.rotation = Quaternion.LookRotation(moveDirection);

            //Debug.DrawRay(RChit.point, moveDirection, Color.red, 20);
        }
    }
}
