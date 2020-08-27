using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    //[HideInInspector] public Transform healthPanel;
    //[HideInInspector] public HealthPanel healthPanelScript;
    [HideInInspector] public Transform energyPanel;
    [HideInInspector] public Image energySlider;

    Transform CamTarget;
    [HideInInspector] public Main main;
    Joystick joy;
    RaycastHit RChit;
    [HideInInspector] public Collider coll;

    public Color bodyColor;
    public Color rageBodyColor;
    [HideInInspector] public MaterialPropertyBlock MPB;
    [HideInInspector] public MeshRenderer mr;
    [HideInInspector] public TrailRenderer tr;
    [HideInInspector] public LineRenderer lr;
    [HideInInspector] public Animator anim;

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
        tr = GetComponentInChildren<TrailRenderer>();
        lr = GetComponentInChildren<LineRenderer>();
        anim = GetComponentInChildren<Animator>();
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
        UIEnergyRefresh();

        joy = main.joy;
    }

    // обновление UI Energy
    public void UIEnergyRefresh()
    {
        energySlider.fillAmount = curEnergy / maxEnergy;
    }

    //обновление UI Health
    public void UIHealthRefresh()
    {
        main.HealthSlider.fillAmount = curHealthPoint / maxHealthPoint;
        main.HealthCount.text = curHealthPoint.ToString("F0");
    }

    private void Update()
    {
        if (main == null) return;

        if (energyPanel != null) energyPanel.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);

        if (!main.readyToGo) return;

        sPrev = s;
        // определяем и задаем направление движения
        Vector3 direction = Vector3.forward * joy.Vertical + Vector3.right * joy.Horizontal;
        if (direction.magnitude == 0) s = 0;
        else s = moveSpeed;
        sCur = s;


        if (inMatrix)
        {
            PathShower(joy.direction.normalized);
        }

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

                    anim.enabled = true;               

                    tr.enabled = false;

                    timerForRage = -1;
                }
            }
            if (timerForRage < 0)
            {
                transform.position += moveDirection * moveSpeed * Time.deltaTime * main.curSlowerCoeff;
            }
        }

        if (curEnergy < maxEnergy)
        {
            curEnergy += energyRecoveryPerSec * Time.deltaTime * main.curSlowerCoeff;
            if (curEnergy > maxEnergy) curEnergy = maxEnergy;
            UIEnergyRefresh();
        }
    }

    void LateUpdate()
    {
        // тянем камеру за игроком (только по вертикали)
        CamTarget.position = new Vector3(CamTarget.position.x, CamTarget.position.y, transform.position.z);
    }


    private void OnCollisionEnter(Collision other)
    {
        if (other.collider.tag == "Wall")
        {
            // обнуляем позицию по У
            transform.position = new Vector3(transform.position.x, 0, transform.position.z);

            Debug.DrawRay(other.GetContact(0).point, -moveDirection, Color.red, 10);

            //print("col: " + other.GetContact(0).point);

            moveDirection = Vector3.Reflect(moveDirection, other.GetContact(0).normal).normalized;

            transform.rotation = Quaternion.LookRotation(moveDirection);

            Debug.DrawRay(other.GetContact(0).point, moveDirection, Color.red, 10);
        }
    }

    Vector3[] GetPath(Vector3 dir, float height, float lastdist)
    {
        List<Vector3> path = new List<Vector3>();
        path.Add(transform.position + new Vector3(0, height, 0));
        RaycastHit hit_OBS; float distToObs = 10f;
        RaycastHit hit_NPC; float distToNpc = 10f;

        //print("func: "+dir.x + "/" + dir.y + "/" + dir.z);
        if (Physics.SphereCast(path[0], 0.4f, dir, out hit_OBS, distToObs, 1 << 9))
        {
            distToObs = hit_OBS.distance;

            //Debug.DrawRay(hit_OBS.point, -dir * 2f, Color.black, 10);
        }
        if (Physics.SphereCast(path[0], 0.4f, dir, out hit_NPC, distToNpc, 1 << 10))
        {
            distToNpc = hit_NPC.distance;
        }

        if (distToObs < distToNpc)
        {
            path.Add(hit_OBS.point);
            //print("func: " + hit_OBS.point);
            dir = Vector3.Reflect(dir, hit_OBS.normal).normalized;

            //Debug.DrawRay(hit_OBS.point, dir * 2f, Color.black, 10);
        }
        else if (distToObs > distToNpc)
        {
            path.Add(hit_NPC.point);
            dir = Vector3.Reflect(dir, hit_NPC.normal).normalized;
        }
        else
        {
            path.Add(path[0] + dir * 10f);
            return path.ToArray();
        }

        distToObs = lastdist;
        distToNpc = lastdist;
        if (Physics.SphereCast(path[1], 0.4f, dir, out hit_OBS, lastdist, 1 << 9))
        {
            distToObs = hit_OBS.distance;
        }
        if (Physics.SphereCast(path[1], 0.4f, dir, out hit_NPC, lastdist, 1 << 10))
        {
            distToNpc = hit_NPC.distance;
        }

        if (distToObs < distToNpc)
        {
            path.Add(hit_OBS.point);
        }
        else if (distToObs > distToNpc)
        {
            path.Add(hit_NPC.point);
        }
        else
        {
            path.Add(path[1] + dir * lastdist);
        }

        return path.ToArray();
    }

    public void PathShower(Vector3 dir)
    {
        Vector3[] path = GetPath(dir, 0.4f, 3f);
        //Vector3[] path = GetPath2(transform.position, dir * rageSpeed);
        lr.positionCount = path.Length;
        lr.SetPositions(path);
    }


    Vector3[] GetPath2(Vector3 origin, Vector3 speed)
    {
        GameObject bullet = Instantiate(Resources.Load<GameObject>("Prefabs/Sphere"), origin, Quaternion.identity);
        bullet.GetComponent<Rigidbody>().AddForce(speed, ForceMode.VelocityChange);

        Physics.autoSimulation = false;

        // Симуляция:
        Vector3[] points = new Vector3[50];

        points[0] = origin;
        for (int i = 1; i < points.Length; i++)
        {
            Physics.Simulate(0.2f);

            points[i] = bullet.transform.position;
        }

        // Зачистка:
        Physics.autoSimulation = true;

        Destroy(bullet.gameObject);

        return points;
    }
}
