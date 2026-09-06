using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialScript : MonoBehaviour
{
    public bool isStageTutorial;
    public bool isDrawTutorial;
    public bool isItemTutorial;

    [SerializeField]
    private GameObject tutorialPanel;
    [SerializeField]
    private TextMeshProUGUI tutorialText;

    [SerializeField]
    private List<TutorialStep> tutorialSteps = new List<TutorialStep>();
    private int currentStepIndex = 0;
    private bool isTutorialActive = false;

    private TutorialStep step;

    // Update is called once per frame
    void Update()
    {
        if (!isTutorialActive) return;

        if (step != null && step.isClickToContinue)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // 마우스 클릭
                NextStep();
            }
            else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                // 모바일 터치 시작
                NextStep();
            }
        }
    }

    public void StartTutorial()
    {
        currentStepIndex = 0;
        isTutorialActive = true;
        ShowCurrentStep();
    }

    public void ShowCurrentStep()
    {
        if (currentStepIndex >= tutorialSteps.Count)
        {
            EndTutorial();
            return;
        }

        if (step != null)
        {
            // 이전 step 지우기
            if (step.obj != null)
            {
                step.obj.SetActive(false);
            }
            if (step.nextButton.Count > 0 && step.nextButton != null)
            {
                foreach (var button in step.nextButton)
                {
                    if (button != null)
                        button.onClick.RemoveListener(NextStep);
                }
            }
        }

        step = tutorialSteps[currentStepIndex];
        tutorialText.text = step.message;
        tutorialText.rectTransform.anchoredPosition = step.messagePos;
        tutorialPanel.SetActive(true);

        if (step.obj != null)
        {
            step.obj.SetActive(true);
        }
        if (step.nextButton.Count > 0 && step.nextButton != null)
        {
            foreach (var button in step.nextButton)
            {
                button.onClick.AddListener(NextStep);
            }
        }
    }

    public void NextStep()
    {
        currentStepIndex++;
        ShowCurrentStep();
    }

    private void EndTutorial()
    {
        tutorialPanel.SetActive(false);
        isTutorialActive = false;

        if (isStageTutorial)
        {
            DatabaseManager.instance.myPlayerData.clearStageTutorial = true;
        }
        if (isDrawTutorial)
        {
            DatabaseManager.instance.myPlayerData.clearDrawTutorial = true;
            TutorialManager.instance.StartItemTutorial();
        }
        if (isItemTutorial)
        {
            DatabaseManager.instance.myPlayerData.clearItemTutorial = true;
        }

        DatabaseManager.instance.SaveMyPlayerDataToJson();
    }
}
