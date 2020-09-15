﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    public bool JOY_DIR_INVERSE;
    [Space]
    public Player player; // префаб игрока
    public Transform rocketsPool; // пул прожектайлов
    public Transform voidZonesPool; // пул войд зон
    public Transform voidZoneCastEffectsPool; // пул эффектов кастования войд зоны
    public Transform healthPanelsPool; // пул UI панелей здоровья
    //public Transform energyPanelsPool; // пул UI панелей energy
    public Transform PlayerBarsPool; // пул UI панелей HP/EN
    public Transform deathEffectsPool; // пул эффектов смерти
    public float matrixCoeff; // коэффициент замедления времени во время движения игрока 
    public float curSlowerCoeff; // текущее замедление (либо нормальная скорость [=1], либо пониженная [=matrixCoeff])
    //public Image HealthSlider;
    //public Text HealthCount;
    public Transform playerSpawnPoint;
    public Joystick joy;

    public List<Enemy> enemies = new List<Enemy>();

    public Text MessagePanel;
    public Text Timer;
    float globalTimer;

    public bool readyToGo;

    public GameObject RepeatButton;
    public GameObject NextButton;
    public GameObject StartButton;

    public Image ToneMap;
    public Text LevelName;
    public Image HandImage;

    public List<GameObject> dontDestroyOnLoadGameObjects;

    public float storedPlayerHealth;
    public float playerHealthRecoveryCount; // доля (в % от maxHealthPoint) хп, на которое восстановится здоровье игрока на следующем уровне (но не больше максимального значения)


    void Awake()
    {
        // заполняем пул прожектайлов
        for (int i = 0; i < 100; i++)
        {
            GameObject rocket = Instantiate(Resources.Load<GameObject>("Prefabs/Rocket")) as GameObject;
            rocket.transform.SetParent(rocketsPool);
            rocket.GetComponent<Rocket>().main = this;
        }

        // заполняем пул эффектов смерти
        for (int i = 0; i < 30; i++)
        {
            GameObject DE = Instantiate(Resources.Load<GameObject>("Prefabs/DeathEffect")) as GameObject;
            DE.transform.SetParent(deathEffectsPool);
        }

        // заполняем пул войд зон
        for (int i = 0; i < 30; i++)
        {
            GameObject voidzone = Instantiate(Resources.Load<GameObject>("Prefabs/VoidZone")) as GameObject;
            voidzone.transform.SetParent(voidZonesPool);
            voidzone.GetComponent<VoidZone>().main = this;
        }

        // заполняем пул эффектов кастования войд зон
        for (int i = 0; i < 30; i++)
        {
            GameObject voidzonecasteffect = Instantiate(Resources.Load<GameObject>("Prefabs/VoidZoneCastEffect")) as GameObject;
            voidzonecasteffect.transform.SetParent(voidZoneCastEffectsPool);
        }
    }


    void Start()
    {
        StartScene();
    }


    void StartScene()
    {
        MessagePanel.text = "";
        Timer.text = "";
        globalTimer = 0;
        readyToGo = false;
        joy.main = this;
        //StartButton.SetActive(true);
        RepeatButton.SetActive(false);
        NextButton.SetActive(false);

        LevelName.text = "LEVEL 1";

        // находим игрока на сцене
        player = Instantiate(Resources.Load<GameObject>("Prefabs/Player")).GetComponent<Player>();
        player.main = this;

        // инстанциируем для игрока хэлс бар
        //Transform hPanelp = Instantiate(Resources.Load<GameObject>("Prefabs/healthPanel")).transform;
        //hPanelp.SetParent(healthPanelsPool);
        //hPanelp.localScale = new Vector3(1, 1, 1);
        //player.healthPanel = hPanelp;
        //player.healthPanelScript = hPanelp.GetComponent<HealthPanel>();

        // инстанциируем для игрока energy бар
        //Transform ep = Instantiate(Resources.Load<GameObject>("Prefabs/EnergyPanel")).transform;
        //ep.SetParent(energyPanelsPool);
        //ep.localScale = Vector3.one;
        //player.energyPanel = ep;
        //player.energySlider = ep.transform.GetChild(0).GetComponent<Image>();

        //инстанциируем для игрока HP/ EN бар
        Transform HP_EN = Instantiate(Resources.Load<GameObject>("Prefabs/PlayerBars")).transform;
        HP_EN.SetParent(PlayerBarsPool);
        HP_EN.localScale = new Vector3(1, 1, 1);
        player.playerBarsPanel = HP_EN;
        player.playerBarsScript = HP_EN.GetComponent<PlayerBars>();

        player.StartScene();

        curSlowerCoeff = 1; // на старте игры скорость игры нормальная

        // находим врагов на сцене
        enemies = FindObjectsOfType<Enemy>().ToList();
        foreach (Enemy e in enemies)
        {
            e.main = this;
            //инстанциируем для врагов хэлс бары
            Transform hPanele = Instantiate(Resources.Load<GameObject>("Prefabs/healthPanel")).transform;
            hPanele.SetParent(healthPanelsPool);
            hPanele.localScale = new Vector3(1, 1, 1);
            e.healthPanel = hPanele;
            e.healthPanelScript = hPanele.GetComponent<HealthPanel>();
            e.healthPanel.gameObject.SetActive(false);
            e.StartScene();
        }

        // делаем сохранение и загрузку ХП игрока между уровнями
        int curSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (curSceneIndex == 0)
        {
            storedPlayerHealth = player.maxHealthPoint;
        }

        player.curHealthPoint = storedPlayerHealth + playerHealthRecoveryCount * player.maxHealthPoint / 100f;
        if (player.curHealthPoint > player.maxHealthPoint) player.curHealthPoint = player.maxHealthPoint;

        player.playerBarsScript.RefreshHealth(player.curHealthPoint / player.maxHealthPoint, player.curHealthPoint);
    }


    public void BodyHitReaction(MeshRenderer mr, MaterialPropertyBlock MPB, Color color)
    {
        StartCoroutine(ChangeBodyColor(mr, MPB, color));
    }

    // меняем цвет тушки
    IEnumerator ChangeBodyColor(MeshRenderer mr, MaterialPropertyBlock MPB, Color color)
    {
        mr.GetPropertyBlock(MPB);
        MPB.SetColor("_Color", Color.red);
        mr.SetPropertyBlock(MPB);

        yield return new WaitForSeconds(0.2f);

        if (mr != null)
        {
            mr.GetPropertyBlock(MPB);
            MPB.SetColor("_Color", color);
            mr.SetPropertyBlock(MPB);
        }
    }

    // меняем цвет тушки
    IEnumerator ChangeBodyColorInRage(MeshRenderer mr, MaterialPropertyBlock MPB, Color colorNew, float duration, Color colorOld)
    {
        mr.GetPropertyBlock(MPB);
        MPB.SetColor("_Color", colorNew);
        mr.SetPropertyBlock(MPB);

        yield return new WaitForSeconds(duration);

        if (mr != null)
        {
            mr.GetPropertyBlock(MPB);
            MPB.SetColor("_Color", colorOld);
            mr.SetPropertyBlock(MPB);
        }
    }

    public void EnemyDie(Enemy e)
    {
        StartCoroutine(EnemyDeath(e));
    }

    // убиваем врага
    IEnumerator EnemyDeath(Enemy e)
    {
        e.healthPanel.gameObject.SetActive(false);
        e.enabled = false;
        foreach (MeshRenderer mr in e.GetComponentsInChildren<MeshRenderer>()) mr.enabled = false;
        e.GetComponent<Collider>().enabled = false;
        //e.AimRing.SetActive(false);

        Transform deathEffect = deathEffectsPool.GetChild(0);
        deathEffect.SetParent(null);
        deathEffect.position = e.transform.position;

        enemies.Remove(e);

        yield return new WaitForSeconds(1);

        Destroy(e.gameObject);
        Destroy(e.healthPanel.gameObject);

        StartCoroutine(EndOfBattle());

        yield return new WaitForSeconds(1);

        if(deathEffect != null) deathEffect.SetParent(deathEffectsPool);
    }

    IEnumerator WaitLastFlyingRocket()
    {
        while (true)
        {
            if (FindObjectsOfType<Rocket>().Length == 0) yield break;
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator EndOfBattle()
    {
        //yield return StartCoroutine(WaitLastFlyingRocket());
        yield return null;
        if (player != null && enemies.Count == 0)
        {
            foreach (Rocket r in FindObjectsOfType<Rocket>())
            {
                r.transform.SetParent(rocketsPool);
                r.flying = false;
            }

            MessagePanel.text = "YOU WON!\n" + Timer.text + " seconds";
            RepeatButton.SetActive(true);
            NextButton.SetActive(true);
        }
        else if (player == null)
        {
            MessagePanel.text = "YOU LOSE!";
            RepeatButton.SetActive(true);
            NextButton.SetActive(false);
        }
    }

    public void PlayerDie(Player p)
    {
        StartCoroutine(PlayerDeath(p));
    }

    // убиваем игрока
    IEnumerator PlayerDeath(Player p)
    {
        //p.energyPanel.gameObject.SetActive(false);
        p.playerBarsPanel.gameObject.SetActive(false);
        p.enabled = false;
        foreach (MeshRenderer mr in p.GetComponentsInChildren<MeshRenderer>()) mr.enabled = false;
        p.GetComponent<Collider>().enabled = false;

        Transform deathEffect = deathEffectsPool.GetChild(0);
        deathEffect.SetParent(null);
        deathEffect.position = p.transform.position;

        p.inMatrix = false;
        curSlowerCoeff = 1;
        ToneMap.enabled = false;
        player = null;

        yield return new WaitForSeconds(1);

        Destroy(p.gameObject);
        //Destroy(p.energyPanel.gameObject);
        Destroy(p.playerBarsPanel.gameObject);

        StartCoroutine(EndOfBattle());

        yield return new WaitForSeconds(1);

        if (deathEffect != null) deathEffect.SetParent(deathEffectsPool);
    }

    void LateUpdate()
    {
        if (readyToGo && player != null && enemies.Count > 0)
        {
            globalTimer += Time.deltaTime;
            Timer.text = globalTimer.ToString("F0");
        }
    }

    public void ResetCurrentLevel()
    {
        StartCoroutine(resetCurrentLevel());
    }

    IEnumerator resetCurrentLevel()
    {
        if (player != null)
        {
            //Destroy(player.healthPanel.gameObject);
            Destroy(player.playerBarsPanel.gameObject);
            Destroy(player.gameObject);

            yield return null;
        }

        int curSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (curSceneIndex == 0)
        {
            SceneManager.LoadScene(curSceneIndex);
        }
        else
        {
            foreach (Enemy e in enemies)
            {
                Destroy(e.healthPanel.gameObject);
            }

            foreach (GameObject go in dontDestroyOnLoadGameObjects)
            {
                DontDestroyOnLoad(go);
            }

            yield return null;

            AsyncOperation operation = SceneManager.LoadSceneAsync(curSceneIndex);
            while (!operation.isDone)
            {
                yield return null;
            }

            // прогрузилась сцена
            yield return null;
            StartScene();
        }
    }

    public void LoadNextLevel()
    {
        StartCoroutine(loadNextLevel());
    }

    IEnumerator loadNextLevel()
    {
        storedPlayerHealth = player.curHealthPoint;

        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (SceneManager.sceneCountInBuildSettings == nextSceneIndex)
        {
            MessagePanel.text = "You have completed\nall the levels!";
            yield break;
        }

        //Destroy(player.healthPanel.gameObject);
        Destroy(player.playerBarsPanel.gameObject);
        Destroy(player.gameObject);

        yield return null;

        foreach (GameObject go in dontDestroyOnLoadGameObjects)
        {
            DontDestroyOnLoad(go);
        }

        yield return null;

        AsyncOperation operation = SceneManager.LoadSceneAsync(nextSceneIndex);
        while (!operation.isDone)
        {
            yield return null;
        }

        // прогрузилась сцена
        yield return null;
        StartScene();

        LevelName.text = "LEVEL " + (nextSceneIndex + 1).ToString();
    }

    public void StartCurrentLevel()
    {
        StartButton.SetActive(false);
        //StartCoroutine(StartTimer());
        readyToGo = true;
    }

    public IEnumerator StartTimer()
    {
        MessagePanel.text = "3";
        yield return new WaitForSeconds(1);
        MessagePanel.text = "2";
        yield return new WaitForSeconds(1);
        MessagePanel.text = "1";
        yield return new WaitForSeconds(1);
        MessagePanel.text = "GO!!!";
        yield return new WaitForSeconds(1);
        readyToGo = true;
        MessagePanel.text = "";
    }

    public void stopAllCorutines()
    {
        StopAllCoroutines();
    }
}
