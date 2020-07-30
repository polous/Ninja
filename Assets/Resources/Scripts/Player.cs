using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed; // базовая скорость игрока
    float s; // текущая скорость игрока
    public float shootRange; // дистанция стрельбы
    public float reloadingTime; // время "перезарядки" оружия
    bool reloading;
    public float rotateSpeed; // скорость поворота
    public float maxEnergy; // максимальный запас энергии
    public float matrixEnergyDrop; // расход энергии на вход в матрицу
    public float energyGrowPerSec; // расход энергии в секунду в матрице
    public float energyRecoveryPerSec; // восстановление энергии в секунду вне матрицы
    public float curEnergy; // текущий запас энергии
    public float sPrev, sCur;

    
    public Joystick joy;
    public Transform SpawnPoint;
    Transform CamTarget;
    public Main main;
    RaycastHit RChit;

    public List<Enemy> enemies = new List<Enemy>();
    Enemy myAim;

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

        CamTarget = GameObject.Find("CamTarget").transform;
        // располагаем игрока в его стартовой позиции на сцене
        transform.position = SpawnPoint.position;
        s = 0;
        curEnergy = maxEnergy;
        UIRefresh(curEnergy);
    }

    // обновление UI
    void UIRefresh(float curEnergy)
    {
        main.EnergySlider.fillAmount = curEnergy / maxEnergy;
        main.EnergyCount.text = curEnergy.ToString("F0");
    }

    private void Update()
    {
        myAim = null;
        sPrev = s;
        // определяем и задаем направление движения
        Vector3 direction = Vector3.forward * joy.Vertical + Vector3.right * joy.Horizontal;
        if (direction.magnitude <= 0.05f) s = 0;
        else s = moveSpeed;
        sCur = s;

        // определяем, хватает ли игроку энергии для начала движения
        if (s != 0)
        {
            if (sPrev == 0 && sCur > 0)
            {
                if(curEnergy >= matrixEnergyDrop)
                {
                    // тратим энергию перманентно на активацию (опционально: если это не нужно, то matrixEnergyDrop установить рааным 0)
                    curEnergy -= matrixEnergyDrop;
                    UIRefresh(curEnergy);
                }
                else
                {
                    s = 0;
                }
            }
        }

        // включаем режим МАТРИЦА
        if (s != 0)
        {
            main.curSlowerCoeff = main.matrixCoeff;
            // тратим энергию посекундно (опционально: если это не нужно, то energyGrowPerSec установить равным 0)
            if (curEnergy > 0) curEnergy -= energyGrowPerSec * Time.deltaTime;
            else
            {
                curEnergy = 0;
                s = 0;
            }
            UIRefresh(curEnergy);
        }
        else main.curSlowerCoeff = 1;

        // двигаем и поворачиваем игрока в заданном направлении        
        if (s != 0)
        {
            transform.position += direction.normalized * s * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // определяем близжайщего видимого (не закрытого препятствиями) врага
        float nearestShootDist = shootRange;        
        foreach (Enemy e in enemies)
        {
            if ((e.transform.position - transform.position).magnitude <= nearestShootDist && !Physics.SphereCast(transform.position + Vector3.up * 0.5f, 0.2f, e.transform.position - transform.position, out RChit, (e.transform.position - transform.position).magnitude, 1 << 9))
            {
                myAim = e;
                nearestShootDist = (e.transform.position - transform.position).magnitude;
            }
        }

        // если игрок стоит (скорость передвижения == 0), то он долбит по ближайшему врагу
        // и восстанавливает энергию
        if (s == 0)
        {
            if (curEnergy < maxEnergy) curEnergy += energyRecoveryPerSec * Time.deltaTime;
            else curEnergy = maxEnergy;
            UIRefresh(curEnergy);

            if (myAim != null)
            {
                // если смотрим на врага, то стреляем в него
                if (Vector3.Angle(transform.forward, myAim.transform.position - transform.position) <= 1f)
                {
                    if (!reloading)
                    {
                        // вытаскиваем из пула и настраиваем прожектайл 
                        Rocket rocket = main.rocketsPool.GetChild(0).GetComponent<Rocket>();
                        rocket.transform.parent = null;
                        rocket.transform.position = transform.position + 0.5f * Vector3.up;
                        rocket.startPoint = rocket.transform.position;
                        rocket.maxRange = shootRange;
                        rocket.MyShooterTag = tag;
                        rocket.flying = true;
                        rocket.direction = myAim.transform.position - transform.position;
                        // "пережаряжаемся" (задержка между выстрелами)
                        StartCoroutine(Reloading(reloadingTime));
                    }
                }
                // иначе поворачиваемся на врага для стрельбы
                else
                {
                    Vector3 targetDir = myAim.transform.position - transform.position;
                    Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, rotateSpeed * Time.deltaTime, 0);

                    transform.rotation = Quaternion.LookRotation(newDir);
                }
            }
        }
    }

    // "перезарядка" оружия (задержка между выстрелами)
    IEnumerator Reloading(float reloadingTime)
    {
        reloading = true;
        yield return new WaitForSeconds(reloadingTime);
        reloading = false;
    }


    void LateUpdate()
    {
        // тянем камеру за игроком (только по вертикали)
        CamTarget.position = new Vector3(CamTarget.position.x, CamTarget.position.y, transform.position.z);
    }
}
