using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionManager : MonoBehaviour
{
    public static ConditionManager instance;

    [SerializeField] private GameObject heroConditionPanel;
    [SerializeField] private GameObject monsterConditionPanel;

    private List<Transform> heroConditions = new List<Transform>();
    private List<Transform> monsterConditions = new List<Transform>();

    private void Awake()
    {
        if (instance == null) instance = this;

        InitConditionList(heroConditionPanel, heroConditions);
        InitConditionList(monsterConditionPanel, monsterConditions);
    }

    private void InitConditionList(GameObject panel, List<Transform> list)
    {
        for (int i = 0; i < panel.transform.childCount; i++)
        {
            list.Add(panel.transform.GetChild(i));
            list[i].gameObject.SetActive(false);
        }
    }

    public void AddCondition(bool isHero, ConditionType condition, int duration)
    {
        List<Transform> target = isHero ? heroConditions : monsterConditions;

        // 덮어쓰기
        foreach (var tf in target)
        {
            if (tf.gameObject.activeSelf &&
                tf.GetComponent<ConditionScript>().condition == condition)
            {
                tf.GetComponent<ConditionScript>().SetCondition(condition, duration);
                return;
            }
        }

        // 새로 적용
        foreach (var tf in target)
        {
            if (!tf.gameObject.activeSelf)
            {
                tf.gameObject.SetActive(true);
                tf.GetComponent<ConditionScript>().SetCondition(condition, duration);
                return;
            }
        }
    }

    public void RemoveCondition(bool isHero, ConditionType condition)
    {
        List<Transform> target = isHero ? heroConditions : monsterConditions;

        foreach (var tf in target)
        {
            if (tf.gameObject.activeSelf &&
                tf.GetComponent<ConditionScript>().condition == condition)
            {
                tf.gameObject.SetActive(false);
                return;
            }
        }
    }

    public void EndTurn()
    {
        foreach (var tf in heroConditions)
        {
            if (tf.gameObject.activeSelf)
                tf.GetComponent<ConditionScript>().EndTurn();
        }

        foreach (var tf in monsterConditions)
        {
            if (tf.gameObject.activeSelf)
                tf.GetComponent<ConditionScript>().EndTurn();
        }
    }
}
