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
    public int rocketDamage; // текущий урон от оружия
    public float rocketSpeed; // скорость полета пули
    public float reloadingTime; // время перезарядки оружия (задержка между соседними атаками)
    public float movingTime; // время в пути (после него идет переопределение пути)
    bool reloading;
    bool moving;
    public float rotateSpeed; // скорость поворота
    public float maxHealthPoint; // максимальный запас здоровья
    public float curHealthPoint; // текущий запас здоровья
    public Transform healthPanel;
    public Image healthSlider;

    public Main main;

    public moveType MT;

    RaycastHit RChit;
    NavMeshHit NMhit;
    NavMeshPath path;
    float pathDist;
    Vector3 targetDir;

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
                if (pathDist >= maxDistance / 2f) return;
            }
        }
    }


    void Update()
    {
        healthPanel.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);

        if ((main.player.transform.position - transform.position).magnitude <= shootRange && !Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.2f, main.player.transform.position - transform.position, out RChit, (main.player.transform.position - transform.position).magnitude, 1 << 9))
        {
            if (Vector3.Angle(transform.forward, main.player.transform.position - transform.position) <= 1f)
            {
                if (!reloading)
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

                    StartCoroutine(Reloading(reloadingTime));
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
                    GetRandomPoint(transform.position, shootRange * 2f);
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

    IEnumerator Reloading(float reloadingTime)
    {
        reloading = true;
        yield return new WaitForSeconds(reloadingTime);
        reloading = false;
    }

    IEnumerator Moving(float movingTime)
    {
        moving = true;
        yield return new WaitForSeconds(movingTime);
        moving = false;
    }
}
