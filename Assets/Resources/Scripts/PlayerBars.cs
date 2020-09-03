using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PlayerBars : MonoBehaviour
{
    public Image healthSlider;
    public Text healthCount;
    public Image energySlider;

    public GameObject Cell_1;
    public GameObject Cell_2;
    public GameObject Cell_3;

    public void RefreshHealth(float fillAmount, float count)
    {
        if (fillAmount < 0) fillAmount = 0;
        if (fillAmount > 1) fillAmount = 1;

        healthSlider.fillAmount = fillAmount;

        healthCount.text = count.ToString("F0");
    }

    public void RefreshEnergy(float fillAmount)
    {
        if (fillAmount < 0) fillAmount = 0;
        if (fillAmount > 1) fillAmount = 1;

        energySlider.fillAmount = fillAmount;

        if (fillAmount < 0.333f)
        {
            Cell_1.SetActive(false);
            Cell_2.SetActive(false);
            Cell_3.SetActive(false);
        }
        else if (fillAmount >= 0.333f && fillAmount < 0.666f)
        {
            Cell_1.SetActive(true);
            Cell_2.SetActive(false);
            Cell_3.SetActive(false);
        }
        else if (fillAmount >= 0.666f && fillAmount < 1f)
        {
            Cell_1.SetActive(true);
            Cell_2.SetActive(true);
            Cell_3.SetActive(false);
        }
        else
        {
            Cell_1.SetActive(true);
            Cell_2.SetActive(true);
            Cell_3.SetActive(true);
        }
    }
}
