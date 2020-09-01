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
    }
}
