using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResearchLineDrawer : MonoBehaviour
{
    [SerializeField] private RectTransform lineRoot;
    [SerializeField] private Image linePrefab;
    [SerializeField] private float lineThickness = 4f;
    [SerializeField] private Color defaultLineColor = Color.white;

    private readonly List<Image> activeLines = new List<Image>();
    private bool loggedMissingReference;

    public void RefreshLines(ResearchButtonScript[] _researchButtons)
    {
        ClearLines();

        if (_researchButtons == null || _researchButtons.Length == 0)
            return;

        if (lineRoot == null || linePrefab == null)
        {
            LogMissingReferencesOnce();
            return;
        }

        Dictionary<string, ResearchButtonScript> buttonByResearchId = BuildButtonMap(_researchButtons);
        HashSet<string> drawnLineKeys = new HashSet<string>();

        for (int i = 0; i < _researchButtons.Length; i++)
        {
            ResearchButtonScript currentButton = _researchButtons[i];
            ResearchData currentResearch = currentButton != null ? currentButton.CurrentResearchData : null;
            if (currentResearch == null || currentResearch.prerequisiteResearchIds == null)
                continue;

            for (int j = 0; j < currentResearch.prerequisiteResearchIds.Count; j++)
            {
                string prerequisiteId = currentResearch.prerequisiteResearchIds[j];
                if (string.IsNullOrWhiteSpace(prerequisiteId))
                    continue;

                if (!buttonByResearchId.TryGetValue(prerequisiteId, out ResearchButtonScript prerequisiteButton))
                    continue;

                string lineKey = prerequisiteId + "->" + currentResearch.id;
                if (!drawnLineKeys.Add(lineKey))
                    continue;

                DrawLine(prerequisiteButton, currentButton);
            }
        }
    }

    private Dictionary<string, ResearchButtonScript> BuildButtonMap(ResearchButtonScript[] researchButtons)
    {
        Dictionary<string, ResearchButtonScript> result = new Dictionary<string, ResearchButtonScript>();
        for (int i = 0; i < researchButtons.Length; i++)
        {
            ResearchButtonScript button = researchButtons[i];
            if (button == null || string.IsNullOrWhiteSpace(button.ResearchId))
                continue;

            if (!result.ContainsKey(button.ResearchId))
                result.Add(button.ResearchId, button);
        }

        return result;
    }

    private void DrawLine(ResearchButtonScript _fromButton, ResearchButtonScript _toButton)
    {
        RectTransform fromRect = _fromButton != null ? _fromButton.transform as RectTransform : null;
        RectTransform toRect = _toButton != null ? _toButton.transform as RectTransform : null;
        if (fromRect == null || toRect == null)
            return;

        if (!TryGetLocalCenter(fromRect, out Vector2 fromPoint))
            return;

        if (!TryGetLocalCenter(toRect, out Vector2 toPoint))
            return;

        Image line = Instantiate(linePrefab, lineRoot);
        if (line == null)
            return;

        line.gameObject.SetActive(true);
        line.color = defaultLineColor;

        RectTransform lineRect = line.rectTransform;
        Vector2 delta = toPoint - fromPoint;
        float distance = delta.magnitude;
        Vector2 center = (fromPoint + toPoint) * 0.5f;

        lineRect.anchoredPosition = center;
        lineRect.sizeDelta = new Vector2(distance, Mathf.Max(1f, lineThickness));
        lineRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        activeLines.Add(line);
    }

    private bool TryGetLocalCenter(RectTransform _target, out Vector2 _localPoint)
    {
        _localPoint = Vector2.zero;

        if (_target == null || lineRoot == null)
            return false;

        Vector3 worldCenter = _target.TransformPoint(_target.rect.center);
        Camera camera = GetCanvasCamera();
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(camera, worldCenter);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(lineRoot, screenPoint, camera, out _localPoint);
    }

    private Camera GetCanvasCamera()
    {
        Canvas canvas = lineRoot != null ? lineRoot.GetComponentInParent<Canvas>() : null;
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private void ClearLines()
    {
        for (int i = 0; i < activeLines.Count; i++)
        {
            if (activeLines[i] != null)
                Destroy(activeLines[i].gameObject);
        }

        activeLines.Clear();
    }

    private void LogMissingReferencesOnce()
    {
        if (loggedMissingReference)
            return;

        loggedMissingReference = true;
        Debug.LogWarning($"{nameof(ResearchLineDrawer)}: lineRoot or linePrefab is not assigned.", this);
    }
}
