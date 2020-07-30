using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    public Player player; // префаб игрока
    public Transform rocketsPool; // пул прожектайлов
    public float matrixCoeff; // коэффициент замедления времени во время движения игрока 
    public float curSlowerCoeff; // текущее замедление (либо нормальная скорость [=1], либо пониженная [=matrixCoeff])
    public Image EnergySlider;
    public Text EnergyCount;


    void Start()
    {
        player = FindObjectOfType<Player>();
        // заполняем пул прожектайлами
        for (int i = 0; i < 100; i++)
        {
            GameObject rocket = Instantiate(Resources.Load<GameObject>("Prefabs/Rocket")) as GameObject;
            rocket.transform.SetParent(rocketsPool);
            rocket.GetComponent<Rocket>().main = this;
        }
        curSlowerCoeff = 1; // на старте игры скорость игры нормальная
    }

    public void BodyHitReaction(MeshRenderer mr, MaterialPropertyBlock MPB, Color color)
    {
        StartCoroutine(ChangeBodyColor(mr, MPB, color));
    }

    IEnumerator ChangeBodyColor(MeshRenderer mr, MaterialPropertyBlock MPB, Color color)
    {
        mr.GetPropertyBlock(MPB);
        MPB.SetColor("_Color", Color.red);
        mr.SetPropertyBlock(MPB);

        yield return new WaitForSeconds(0.2f);

        mr.GetPropertyBlock(MPB);
        MPB.SetColor("_Color", color);
        mr.SetPropertyBlock(MPB);
    }
}
