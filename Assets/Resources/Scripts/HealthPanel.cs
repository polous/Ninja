using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class HealthPanel : MonoBehaviour
{
    public Image healthSlider;
    public List<Text> damageText;

    public void HitFunction(float fillAmount, float damage)
    {
        if (fillAmount < 0) fillAmount = 0;
        healthSlider.fillAmount = fillAmount;

        //foreach (Text t in damageText)
        //{
        //    if (!t.gameObject.activeSelf)
        //    {
        //        t.gameObject.SetActive(true);
        //        t.GetComponent<Animator>().SetTrigger("hit");
        //        t.text = "-" + damage.ToString();
        //        StartCoroutine(Deactivate(t.gameObject));
        //        break;
        //    }
        //}
    }

    public void HealFunction(float fillAmount, float healCount)
    {
        if (fillAmount > 1) fillAmount = 1;
        healthSlider.fillAmount = fillAmount;

        //foreach (Text t in damageText)
        //{
        //    if (!t.gameObject.activeSelf)
        //    {
        //        t.gameObject.SetActive(true);
        //        t.GetComponent<Animator>().SetTrigger("hit");
        //        t.text = "+" + healCount.ToString();
        //        StartCoroutine(Deactivate(t.gameObject));
        //        break;
        //    }
        //}
    }

    IEnumerator Deactivate(GameObject go)
    {
        yield return new WaitForSeconds(1);
        go.SetActive(false);
    }

}
