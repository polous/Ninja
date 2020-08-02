using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public enum moveType
{
    Random,
    Follow,
    Stay
}

public class Enemy : MonoBehaviour
{
    public float moveSpeed; // скорость перемещения
    public float shootRange; // дистанция атаки
    public float rotateSpeed; // скорость поворота
    public float movingTime; // время в пути (после него идет переопределение пути)

    public int rocketDamage; // текущий урон от оружия
    public float rocketSpeed; // скорость полета пули
    public float reloadingTime; // время перезарядки оружия (задержка между соседними атаками в секундах)

    public float maxHealthPoint; // максимальный запас здоровья
    public float curHealthPoint; // текущий запас здоровья

    public float voidZoneDamage; // урон от войд зоны
    public float voidZoneRadius; // радиус войд зоны
    public int voidZoneDuration; // продолжительность от начала каста до непосредственно взрыва (в секундах)
    public float voidZoneReloadingTime; // время перезарядки войд зоны (задержка между соседними кастами в секундах)

    public Transform healthPanel;
    public HealthPanel healthPanelScript;
    public GameObject AimRing;

    public Main main;

    public moveType MT;

    RaycastHit RChit;
    NavMeshHit NMhit;
    NavMeshPath path;
    float pathDist;
    Vector3 targetDir;

    bool moving;
    public float timerForReloading;
    public float timerForVoidZoneReloading;
    public float timerForVoidZoneCasting;

    int i;

    public Color bodyColor;
    public MaterialPropertyBlock MPB;
    public MeshRenderer mr;


    void Start()
    {
        MPB = new MaterialPropertyBlock();
        mr = GetComponentInChildren<MeshRenderer>();
        mr.GetPropertyBlock(MPB);
        MPB.SetColor("_Color", bodyColor);
        mr.SetPropertyBlock(MPB);

        curHealthPoint = maxHealthPoint;

        path = new NavMeshPath();

        timerForVoidZoneReloading = Random.Range(0, voidZoneReloadingTime); // рандомный таймер для войд зоны (чтобы на старте все враги не начинали одновременный каст)
        timerForReloading = reloadingTime;
        timerForVoidZoneCasting = voidZoneDuration;
    }

    // Получение случайной точки на Navmesh
    void GetRandomPoint(Vector3 center, float maxDistance)
    {
        while (true)
        {
            // случайная точка внутри окружности, расположенной в center с радиусом maxDistance
            Vector3 randomPos = new Vector3(Random.Range(center.x - maxDistance, center.x + maxDistance), 0, Random.Range(center.z - maxDistance, center.z + maxDistance));
            // вычисляем путь до randomPos по Navmesh сетке
            NavMesh.CalculatePath(transform.position, randomPos, NavMesh.AllAreas, path);
            // если путь построен
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                // вычисляем длину пути
                if (path.corners.Length >= 2)
                {
                    for (int i = 0; i < path.corners.Length - 1; i++)
                    {
                        pathDist += (path.corners[i + 1] - path.corners[i]).magnitude;
                    }
                }
                // если длина пути достаточная
                if (pathDist >= maxDistance * 2f / 3f) return;
            }
        }
    }


    void Update()
    {
        healthPanel.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);

        if (main.player != null)
        {
            // кастуем войд зону
            if (voidZoneReloadingTime > 0) // исключаем из расчета тех врагов, кто вообще не кастер войд зон
            {
                timerForVoidZoneReloading += Time.deltaTime * main.curSlowerCoeff;
                timerForVoidZoneCasting += Time.deltaTime * main.curSlowerCoeff;

                if (timerForVoidZoneReloading >= voidZoneReloadingTime)
                {
                    VoidZone voidZone = main.voidZonesPool.GetChild(0).GetComponent<VoidZone>();
                    voidZone.transform.parent = null;
                    if ((main.player.transform.position - transform.position).magnitude <= shootRange)
                    {
                        voidZone.transform.position = main.player.transform.position;
                    }
                    else
                    {
                        GetRandomPoint(transform.position, shootRange * 1.5f);
                        voidZone.transform.position = path.corners.Last();
                    }
                    voidZone.damage = voidZoneDamage;
                    voidZone.radius = voidZoneRadius;
                    voidZone.transform.localScale = Vector3.one * voidZoneRadius;
                    voidZone.duration = voidZoneDuration;
                    voidZone.isCasting = true;
                    voidZone.Custer = this;

                    Transform vzce = main.voidZoneCastEffectsPool.GetChild(0);
                    vzce.transform.parent = null;
                    vzce.transform.position = transform.position;
                    voidZone.castEffect = vzce;

                    timerForVoidZoneReloading = 0;
                    timerForVoidZoneCasting = 0;
                }

                // пока враг кастует войд зон, он больше ничем не занимается
                if (timerForVoidZoneCasting < voidZoneDuration) return;
            }

            if ((main.player.transform.position - transform.position).magnitude <= shootRange && !Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.2f, main.player.transform.position - transform.position, out RChit, (main.player.transform.position - transform.position).magnitude, 1 << 9))
            {
                // поворачиваемся а потом стреляем/бьем игрока
                if (Vector3.Angle(transform.forward, main.player.transform.position - transform.position) <= 1f)
                {
                    timerForReloading += Time.deltaTime * main.curSlowerCoeff;
                    if (timerForReloading >= reloadingTime)
                    {
                        Rocket rocket = main.rocketsPool.GetChild(0).GetComponent<Rocket>();
                        rocket.transform.parent = null;
                        rocket.transform.position = transform.position + 0.5f * Vector3.up;
                        rocket.startPoint = rocket.transform.position;
                        rocket.maxRange = shootRange;
                        rocket.MyShooterTag = tag;
                        rocket.flying = true;
                        rocket.speed = rocketSpeed;
                        rocket.damage = rocketDamage;
                        rocket.direction = main.player.transform.position - transform.position;

                        timerForReloading = 0;
                    }
                }
                else
                {
                    targetDir = main.player.transform.position - transform.position; targetDir.y = 0;
                    transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, targetDir, rotateSpeed * Time.deltaTime * main.curSlowerCoeff, 0));
                }
            }
            else
            {
                if (MT == moveType.Random)
                {
                    if (!moving)
                    {
                        GetRandomPoint(transform.position, shootRange * 1.5f);
                        i = 1;

                        StartCoroutine(Moving(movingTime));
                    }
                    else
                    {
                        if (i != path.corners.Length)
                        {
                            transform.position = Vector3.MoveTowards(transform.position, path.corners[i], Time.deltaTime * moveSpeed * main.curSlowerCoeff);
                            targetDir = path.corners[i] - transform.position; targetDir.y = 0;
                            transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, targetDir, rotateSpeed * Time.deltaTime * main.curSlowerCoeff, 0));

                            if ((path.corners[i] - transform.position).magnitude <= 0.01f) i++;
                        }
                        else
                        {
                            moving = false;
                        }
                    }
                }

                else if (MT == moveType.Follow)
                {
                    if (!moving)
                    {
                        NavMesh.CalculatePath(transform.position, main.player.transform.position, NavMesh.AllAreas, path);
                        i = 1;

                        StartCoroutine(Moving(movingTime));
                    }
                    else
                    {
                        if (i != path.corners.Length)
                        {
                            transform.position = Vector3.MoveTowards(transform.position, path.corners[i], Time.deltaTime * moveSpeed * main.curSlowerCoeff);
                            targetDir = path.corners[i] - transform.position; targetDir.y = 0;
                            transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, targetDir, rotateSpeed * Time.deltaTime * main.curSlowerCoeff, 0));
                            //if (transform.position != path.corners[i]) transform.rotation = Quaternion.LookRotation(targetDir);

                            if ((path.corners[i] - transform.position).magnitude <= 0.01f) i++;
                        }
                        else
                        {
                            moving = false;
                        }
                    }
                }
            }
        }
    }

    IEnumerator Moving(float movingTime)
    {
        moving = true;
        yield return new WaitForSeconds(movingTime);
        moving = false;
    }
}
