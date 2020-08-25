using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public enum enemyType
{
    RandomMovement,
    FollowToPlayer
}

public class Enemy : MonoBehaviour
{
    public float moveSpeed; // скорость перемещения
    public float shootRange; // дистанция атаки
    public float movingTime; // время в пути (после него идет переопределение пути)
    float findMovePositionRange = 8;

    public int rocketsCountPerShoot; // количество пуль выпускаемое за один выстрел (если > 1 - дробовик)
    public float rocketSpreadCoeff; // коэффициент разброса пуль
    public float rocketDamage; // текущий урон от оружия
    public float rocketSpeed; // скорость полета пули
    public float reloadingTime; // время перезарядки оружия (задержка между соседними атаками в секундах)

    public float collDamage;
    public float collHeal;

    public float maxHealthPoint; // максимальный запас здоровья
    [HideInInspector] public float curHealthPoint; // текущий запас здоровья

    public float voidZoneCastRange; // максимальная дистанция кастования войд зоны
    public float voidZoneDamage; // урон от войд зоны
    public float voidZoneRadius; // радиус войд зоны
    public float voidZoneDuration; // продолжительность от начала каста до непосредственно взрыва (в секундах)
    public float voidZoneReloadingTime; // время перезарядки войд зоны (задержка между соседними кастами в секундах)

    public bool isTwister;
    public float twisterAngle; // угловой сектор, в рамках которого враг-TwisterTurel будет вращаться
    public float twistSpeed; // скорость поворота Twister-a

    public bool isArmored;

    float curAng, targAng;
    int rotateDir;

    //[HideInInspector] public Transform healthPanel;
    //[HideInInspector] public HealthPanel healthPanelScript;
    //[HideInInspector] public GameObject AimRing;

    [HideInInspector] public Main main;

    public enemyType MT;

    RaycastHit RChit;
    NavMeshHit NMhit;
    NavMeshPath path;
    float pathDist;
    Vector3 targetDir;

    bool moving;
    Vector3 fwd, dir;
    float timerForReloading;
    float timerForVoidZoneReloading;
    float timerForVoidZoneCasting;

    int i;

    public Color bodyColor;
    [HideInInspector] public MaterialPropertyBlock MPB;
    [HideInInspector] public MeshRenderer mr;
    [HideInInspector] public Collider coll;
    [HideInInspector] public Animator anim;

    public Color rocketColor;
    public float rocketSize;


    public void StartScene()
    {
        MPB = new MaterialPropertyBlock();
        mr = GetComponentInChildren<MeshRenderer>();
        mr.GetPropertyBlock(MPB);
        MPB.SetColor("_Color", bodyColor);
        mr.SetPropertyBlock(MPB);
        coll = GetComponent<Collider>();
        anim = GetComponentInChildren<Animator>();

        AnimatorStateInfo state = anim.GetCurrentAnimatorStateInfo(0);
        anim.Play(state.fullPathHash, -1, Random.Range(0f, 1f));

        curHealthPoint = maxHealthPoint;

        path = new NavMeshPath();

        timerForVoidZoneReloading = Random.Range(0, voidZoneReloadingTime); // рандомный таймер для войд зоны (чтобы на старте все враги не начинали одновременный каст)
        timerForReloading = reloadingTime;
        timerForVoidZoneCasting = voidZoneDuration;

        if (rocketsCountPerShoot == 0) rocketsCountPerShoot = 1;

        curAng = 0;
        targAng = twisterAngle / 2f;
        rotateDir = 1;
    }

    // Получение случайной точки на Navmesh
    void GetRandomPoint(Vector3 center, float maxDistance)
    {
        for (int c = 0; c < 50; c++)
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
        path.ClearCorners();
    }


    void Update()
    {
        if (main == null) return;

        //if (healthPanel != null) healthPanel.position = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);

        if (!main.readyToGo) return;


        if (main.player != null)
        {
            // кастуем войд зону
            if (voidZoneDamage > 0)
            {
                if (voidZoneReloadingTime > 0) // исключаем из расчета тех врагов, кто вообще не кастер войд зон
                {
                    VoidZoneCasting();

                    // пока враг кастует войд зону, он больше ничем не занимается
                    if (timerForVoidZoneCasting < voidZoneDuration) return;
                }
            }

            if (!isTwister)
            {
                fwd = transform.forward; fwd.y = 0;
                dir = main.player.transform.position - transform.position; dir.y = 0;
                if ((main.player.transform.position - transform.position).magnitude <= shootRange &&
                    !Physics.Raycast(coll.bounds.center, main.player.transform.position - transform.position, out RChit, (main.player.transform.position - transform.position).magnitude, 1 << 9))
                {
                    if (shootRange > 0)
                    {
                        // стреляем/бьем игрока
                        timerForReloading += Time.deltaTime * main.curSlowerCoeff;

                        if (timerForReloading >= reloadingTime)
                        {
                            RocketShooting(1);

                            timerForReloading = 0;
                        }
                    }
                }
            }
            else
            {
                float step = twistSpeed * Time.deltaTime * main.curSlowerCoeff;
                if (curAng + step >= targAng)
                {
                    if (rotateDir == 1) rotateDir = -1;
                    else rotateDir = 1;
                    curAng = 0;
                    targAng = twisterAngle;
                }
                else
                {
                    curAng += step;

                    if (rotateDir == 1)
                    {
                        transform.Rotate(Vector3.up, step);
                    }
                    else
                    {
                        transform.Rotate(-Vector3.up, step);
                    }
                }

                if (shootRange > 0)
                {
                    timerForReloading += Time.deltaTime * main.curSlowerCoeff;
                    if (timerForReloading >= reloadingTime)
                    {
                        RocketShooting(2);

                        timerForReloading = 0;
                    }
                }
            }

            // двигаем врагов
            if (MT == enemyType.RandomMovement)
            {
                if (!moving && moveSpeed > 0)
                {
                    GetRandomPoint(transform.position, findMovePositionRange);
                    i = 1;

                    StartCoroutine(Moving(movingTime));
                }
                else
                {
                    if (path.corners.Length > 1)
                    {
                        if (i != path.corners.Length)
                        {
                            transform.position = Vector3.MoveTowards(transform.position, path.corners[i], Time.deltaTime * moveSpeed * main.curSlowerCoeff);
                            if ((path.corners[i] - transform.position).magnitude <= 0.01f) i++;
                        }
                        else
                        {
                            moving = false;
                        }
                    }
                }
            }

            else if (MT == enemyType.FollowToPlayer)
            {
                if (!moving && moveSpeed > 0)
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

    private void RocketShooting(int type) // type = 1 - стрельба в направлении игрока, 2 - стрельба в направлении взгляда
    {
        for (int i = 0; i < rocketsCountPerShoot; i++)
        {
            Rocket rocket = main.rocketsPool.GetChild(0).GetComponent<Rocket>();
            rocket.transform.parent = null;
            rocket.transform.position = coll.bounds.center;
            rocket.startPoint = rocket.transform.position;
            rocket.maxRange = shootRange;
            rocket.MyShooterTag = tag;
            rocket.flying = true;
            rocket.speed = rocketSpeed;
            rocket.damage = rocketDamage;

            rocket.RocketParamsChanger(MPB, rocketColor, rocketSize);

            Vector3 randomVector = new Vector3(Random.Range(-rocketSpreadCoeff, +rocketSpreadCoeff), 0, Random.Range(-rocketSpreadCoeff, +rocketSpreadCoeff));
            Vector3 lastPoint = Vector3.zero;
            if (type == 1) lastPoint = transform.position + (main.player.transform.position - transform.position).normalized * shootRange + randomVector;
            if (type == 2) lastPoint = transform.position + transform.forward * shootRange + randomVector;
            Vector3 direction = lastPoint - transform.position;

            rocket.direction = direction;
        }
    }

    private void VoidZoneCasting()
    {
        timerForVoidZoneReloading += Time.deltaTime * main.curSlowerCoeff;
        timerForVoidZoneCasting += Time.deltaTime * main.curSlowerCoeff;

        if (timerForVoidZoneReloading >= voidZoneReloadingTime)
        {
            VoidZone voidZone = main.voidZonesPool.GetChild(0).GetComponent<VoidZone>();
            //voidZone.transform.parent = null;
            //if ((main.player.transform.position - transform.position).magnitude <= shootRange)
            //{
            //    voidZone.transform.position = main.player.transform.position;
            //}
            //else
            //{
            //    GetRandomPoint(transform.position, voidZoneCastRange);
            //    if (path.corners.Length > 1) voidZone.transform.position = path.corners.Last();
            //}
            if (voidZoneCastRange == 0)
            {
                voidZone.transform.position = transform.position;
                voidZone.transform.parent = transform;
            }
            else
            {
                GetRandomPoint(transform.position, voidZoneCastRange);
                if (path.corners.Length > 1) voidZone.transform.position = path.corners.Last();
                voidZone.transform.parent = null;
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
    }

    IEnumerator Moving(float movingTime)
    {
        moving = true;
        yield return new WaitForSeconds(movingTime);
        moving = false;
    }

    // ФУНКЦИЯ ОПРЕДЕЛЕНИЯ НАПРАВЛЕНИЯ ПОВОРОТА (ВЛЕВО ИЛИ ВПРАВО)
    int TypeOfTurn(Vector3 dir)
    {
        Quaternion b = Quaternion.LookRotation(dir);
        Quaternion a = transform.rotation;
        float _a = a.eulerAngles.y;
        float _b = b.eulerAngles.y;

        if (_a >= 180f && _b >= 180f)
        {
            _b = _b - _a;
            if (_b >= 0) return 1;
            else return -1;
        }
        else if (_a <= 180f && _b <= 180f)
        {
            _b = _b - _a;
            if (_b >= 0) return 1;
            else return -1;
        }
        else if (_a >= 180f && _b <= 180f)
        {
            _b = _b - _a + 180f;
            if (_b <= 0) return 1;
            else return -1;
        }
        else if (_a <= 180f && _b >= 180f)
        {
            _b = _b - _a - 180f;
            if (_b <= 0) return 1;
            else return -1;
        }
        else
            return 1;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            Player p = main.player;

            if (collHeal != 0 && p.curHealthPoint < p.maxHealthPoint)
            {
                p.curHealthPoint += collHeal;
                if (p.curHealthPoint > p.maxHealthPoint) p.curHealthPoint = p.maxHealthPoint;
                p.healthPanelScript.HealFunction(p.curHealthPoint / p.maxHealthPoint, collHeal);
            }

            main.BodyHitReaction(mr, MPB, bodyColor);
            if (p.timerForRage > 0) curHealthPoint -= p.rageCollDamage;
            else curHealthPoint -= p.collDamage;
            //e.healthPanelScript.HitFunction(e.curHealthPoint / e.maxHealthPoint, damage);

            if (curHealthPoint <= 0)
            {
                main.EnemyDie(this);
            }
            else
            {
                if (isArmored)
                {
                    Vector3 normal = Vector3.zero;
                    Vector3 dir = coll.bounds.center - p.coll.bounds.center;
                    if (Physics.Raycast(p.coll.bounds.center, dir.normalized, out RChit, dir.magnitude + 0.1f, 1 << 10))
                    {
                        normal = RChit.normal;
                    }

                    p.moveDirection = Vector3.Reflect(p.moveDirection, normal).normalized;
                    p.transform.rotation = Quaternion.LookRotation(p.moveDirection);
                }

                if (collDamage > 0 && p.timerForRage <= 0)
                {
                    main.BodyHitReaction(p.mr, p.MPB, p.bodyColor);

                    p.curHealthPoint -= collDamage;
                    p.healthPanelScript.HitFunction(p.curHealthPoint / p.maxHealthPoint, collDamage);

                    if (p.curHealthPoint <= 0)
                    {
                        main.PlayerDie(p);
                    }
                }
            }
        }
    }


}
