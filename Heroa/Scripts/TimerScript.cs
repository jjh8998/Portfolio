using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerScript : MonoBehaviour
{
    public static TimerScript instance = null;

    public bool timerOn = true;

    [SerializeField]
    private Slider timer = null;
    [SerializeField]
    private float time = 5f;
    private float curTime = 0f;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        ResetTime();
    }

    // Update is called once per frame
    void Update()
    {
        if (timerOn == true)
        {
            if (curTime >= 0)
            {
                curTime -= 1 * Time.deltaTime;
            }
            else
            {
                WordCollector.instance.Answer_Coloring(false, 99);
                ResetTime();
            }

            timer.value = curTime / time; // 타이머 표시
        }
    }

    public void ResetTime()
    {
        curTime = time;
    }
}
