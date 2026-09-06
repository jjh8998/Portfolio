using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PIChartUIController : MonoBehaviour, IPointerClickHandler
{
    private const int RandomColorMaxAttempts = 64;

    [Serializable]
    public class PIChartEntry
    {
        public string label;
        public float value;
        public Color color;
        public bool useCustomColor;
        public object userData;

        public PIChartEntry()
        {
            label = string.Empty;
            value = 0f;
            color = Color.white;
            useCustomColor = false;
            userData = null;
        }

        public PIChartEntry(string _label, float _value)
        {
            label = _label;
            value = _value;
            color = Color.white;
            useCustomColor = false;
            userData = null;
        }

        public PIChartEntry(string _label, float _value, object _userData)
        {
            label = _label;
            value = _value;
            color = Color.white;
            useCustomColor = false;
            userData = _userData;
        }

        public PIChartEntry(string _label, float _value, Color _color)
        {
            label = _label;
            value = _value;
            color = _color;
            useCustomColor = true;
            userData = null;
        }

        public PIChartEntry(string _label, float _value, Color _color, object _userData)
        {
            label = _label;
            value = _value;
            color = _color;
            useCustomColor = true;
            userData = _userData;
        }

        public PIChartEntry(string _label, float _value, Color _color, bool _useCustomColor)
        {
            label = _label;
            value = _value;
            color = _color;
            useCustomColor = _useCustomColor;
            userData = null;
        }

        public PIChartEntry(string _label, float _value, Color _color, bool _useCustomColor, object _userData)
        {
            label = _label;
            value = _value;
            color = _color;
            useCustomColor = _useCustomColor;
            userData = _userData;
        }
    }

    private struct SliceRange
    {
        public PIChartEntry entry;
        public float startRatio;
        public float endRatio;
    }

    [SerializeField] private RectTransform sliceRoot;
    [SerializeField] private Image slicePrefab;
    [SerializeField] private TextMeshProUGUI labelPrefab;
    [SerializeField] private Vector2 chartSize = new Vector2(180f, 180f);
    [SerializeField, Range(0f, 360f)] private float startAngle;
    [SerializeField] private float labelDistance = 80f;
    [SerializeField] private float minLabelValue = 5f;
    [SerializeField] private TextMeshProUGUI emptyText;
    [SerializeField] private bool sortByValueDescending;
    [SerializeField] private bool includeZeroValue;
    [SerializeField] private Color[] defaultColors = { Color.white };

    private readonly List<Image> slices = new List<Image>();
    private readonly List<TextMeshProUGUI> labels = new List<TextMeshProUGUI>();
    private readonly List<PIChartEntry> chartEntries = new List<PIChartEntry>();
    private readonly List<SliceRange> sliceRanges = new List<SliceRange>();
    private readonly Dictionary<string, Color> cachedRandomColorsByLabel = new Dictionary<string, Color>();

    public event Action<PIChartEntry> EntryClicked;

    public void SetChartData(List<PIChartEntry> _entries)
    {
        chartEntries.Clear();

        if (_entries != null)
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                PIChartEntry entry = _entries[i];
                if (entry == null)
                {
                    continue;
                }

                float value = Mathf.Max(0f, entry.value);
                if (!includeZeroValue && value <= 0f)
                {
                    continue;
                }

                chartEntries.Add(entry);
            }
        }

        if (sortByValueDescending)
        {
            chartEntries.Sort((a, b) => Mathf.Max(0f, b.value).CompareTo(Mathf.Max(0f, a.value)));
        }

        RefreshChart(chartEntries);
    }

    public void Clear()
    {
        sliceRanges.Clear();

        if (slicePrefab != null)
        {
            slicePrefab.gameObject.SetActive(false);
        }

        if (labelPrefab != null)
        {
            labelPrefab.gameObject.SetActive(false);
        }

        for (int i = 0; i < slices.Count; i++)
        {
            if (slices[i] != null)
            {
                slices[i].gameObject.SetActive(false);
            }
        }

        for (int i = 0; i < labels.Count; i++)
        {
            if (labels[i] != null)
            {
                labels[i].gameObject.SetActive(false);
            }
        }

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(true);
        }
    }

    private void RefreshChart(List<PIChartEntry> _entries)
    {
        ApplyChartSize();
        sliceRanges.Clear();

        float totalValue = 0f;
        for (int i = 0; i < _entries.Count; i++)
        {
            totalValue += Mathf.Max(0f, _entries[i].value);
        }

        if (totalValue <= 0f || slicePrefab == null)
        {
            Clear();
            return;
        }

        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(false);
        }

        if (labelPrefab != null)
        {
            labelPrefab.gameObject.SetActive(false);
        }

        for (int i = 0; i < labels.Count; i++)
        {
            if (labels[i] != null)
            {
                labels[i].gameObject.SetActive(false);
            }
        }

        Transform parent = sliceRoot != null ? sliceRoot : transform;
        HashSet<Color32> usedColors = new HashSet<Color32>();
        float startRatio = 0f;
        float normalizedStartAngle = Mathf.Repeat(startAngle, 360f);
        int labelIndex = 0;

        for (int i = 0; i < _entries.Count; i++)
        {
            Image slice = GetOrCreateSlice(i, parent);
            if (slice == null)
            {
                continue;
            }

            PIChartEntry entry = _entries[i];
            float normalizedValue = Mathf.Max(0f, entry.value) / totalValue;
            RectTransform rectTransform = slice.rectTransform;

            slice.type = Image.Type.Filled;
            slice.fillMethod = Image.FillMethod.Radial360;
            slice.fillClockwise = true;
            slice.fillAmount = normalizedValue;
            slice.color = ResolveSliceColor(i, entry, usedColors);
            slice.gameObject.name = string.IsNullOrEmpty(entry.label) ? "Pie Slice" : "Pie Slice - " + entry.label;
            slice.gameObject.SetActive(true);

            if (rectTransform != null)
            {
                rectTransform.sizeDelta = GetChartSize();
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, normalizedStartAngle - startRatio * 360f);
            }

            if (ShouldShowLabel(entry))
            {
                CreateOrUpdateLabel(labelIndex, entry, slice, startRatio, normalizedValue, normalizedStartAngle);
                labelIndex++;
            }

            sliceRanges.Add(new SliceRange
            {
                entry = entry,
                startRatio = startRatio,
                endRatio = startRatio + normalizedValue
            });

            startRatio += normalizedValue;
        }

        for (int i = _entries.Count; i < slices.Count; i++)
        {
            if (slices[i] != null)
            {
                slices[i].gameObject.SetActive(false);
            }
        }

        for (int i = labelIndex; i < labels.Count; i++)
        {
            if (labels[i] != null)
            {
                labels[i].gameObject.SetActive(false);
            }
        }
    }

    private Image GetOrCreateSlice(int _index, Transform _parent)
    {
        while (slices.Count <= _index)
        {
            Image newSlice = Instantiate(slicePrefab, _parent);
            newSlice.gameObject.SetActive(false);
            slices.Add(newSlice);
        }

        Image slice = slices[_index];
        if (slice != null && slice.transform.parent != _parent)
        {
            slice.transform.SetParent(_parent, false);
        }

        return slice;
    }

    private void CreateOrUpdateLabel(int _index, PIChartEntry _entry, Image _slice, float _startRatio, float _normalizedValue, float _startAngle)
    {
        if (!ShouldShowLabel(_entry) || labelPrefab == null || _slice == null)
        {
            return;
        }

        TextMeshProUGUI label = GetOrCreateLabel(_index, _slice.transform);
        if (label == null)
        {
            return;
        }

        float originAngle = GetRadial360OriginAngle(_slice);
        float fillDirection = _slice != null && !_slice.fillClockwise ? 1f : -1f;
        float angle = (originAngle + fillDirection * _normalizedValue * 180f) * Mathf.Deg2Rad;
        Vector2 labelDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        label.text = _entry.label;
        label.gameObject.name = "Pie Label - " + _entry.label;
        label.rectTransform.anchoredPosition = labelDirection * labelDistance;
        label.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _startRatio * 360f - _startAngle);
        label.gameObject.SetActive(true);
    }

    private TextMeshProUGUI GetOrCreateLabel(int _index, Transform _parent)
    {
        if (_parent == null)
        {
            return null;
        }

        while (labels.Count <= _index)
        {
            TextMeshProUGUI newLabel = Instantiate(labelPrefab, _parent);
            newLabel.gameObject.SetActive(false);
            labels.Add(newLabel);
        }

        TextMeshProUGUI label = labels[_index];
        if (label != null && label.transform.parent != _parent)
        {
            label.transform.SetParent(_parent, false);
        }

        return label;
    }

    private void ApplyChartSize()
    {
        if (sliceRoot != null)
        {
            sliceRoot.sizeDelta = GetChartSize();
        }
    }

    public void OnPointerClick(PointerEventData _eventData)
    {
        if (_eventData == null || sliceRanges.Count == 0)
            return;

        if (TryGetClickedEntry(_eventData, out PIChartEntry entry))
            EntryClicked?.Invoke(entry);
    }

    private Vector2 GetChartSize()
    {
        return new Vector2(Mathf.Max(1f, chartSize.x), Mathf.Max(1f, chartSize.y));
    }

    private bool TryGetClickedEntry(PointerEventData _eventData, out PIChartEntry _entry)
    {
        _entry = null;

        RectTransform chartRect = GetChartRectTransform();
        if (chartRect == null)
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(chartRect, _eventData.position, _eventData.pressEventCamera, out Vector2 localPoint))
            return false;

        localPoint -= chartRect.rect.center;

        Vector2 radius = GetChartRadius(chartRect);
        if (radius.x <= 0f || radius.y <= 0f)
            return false;

        float normalizedDistance = (localPoint.x * localPoint.x) / (radius.x * radius.x)
                                   + (localPoint.y * localPoint.y) / (radius.y * radius.y);
        if (normalizedDistance > 1f)
            return false;

        float clickRatio = GetClickRatio(localPoint);
        for (int i = 0; i < sliceRanges.Count; i++)
        {
            SliceRange range = sliceRanges[i];
            if (clickRatio >= range.startRatio && clickRatio < range.endRatio)
            {
                _entry = range.entry;
                return _entry != null;
            }
        }

        SliceRange lastRange = sliceRanges[sliceRanges.Count - 1];
        if (Mathf.Approximately(clickRatio, 1f) && Mathf.Approximately(lastRange.endRatio, 1f))
        {
            _entry = lastRange.entry;
            return _entry != null;
        }

        return false;
    }

    private RectTransform GetChartRectTransform()
    {
        if (sliceRoot != null)
            return sliceRoot;

        return transform as RectTransform;
    }

    private Vector2 GetChartRadius(RectTransform _chartRect)
    {
        if (_chartRect == null)
            return Vector2.zero;

        Rect rect = _chartRect.rect;
        Vector2 size = new Vector2(Mathf.Abs(rect.width), Mathf.Abs(rect.height));
        if (size.x <= 0f || size.y <= 0f)
            size = GetChartSize();

        return new Vector2(Mathf.Max(1f, size.x * 0.5f), Mathf.Max(1f, size.y * 0.5f));
    }

    private float GetClickRatio(Vector2 localPoint)
    {
        float angle = Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg;
        float originAngle = GetRadial360OriginAngle(GetFirstVisibleSlice());
        return Mathf.Repeat(originAngle + Mathf.Repeat(startAngle, 360f) - angle, 360f) / 360f;
    }

    private Image GetFirstVisibleSlice()
    {
        for (int i = 0; i < slices.Count; i++)
        {
            Image slice = slices[i];
            if (slice != null && slice.gameObject.activeSelf)
                return slice;
        }

        return slicePrefab;
    }

    private float GetRadial360OriginAngle(Image _slice)
    {
        if (_slice == null)
        {
            return -90f;
        }

        switch ((Image.Origin360)_slice.fillOrigin)
        {
            case Image.Origin360.Right:
                return 0f;
            case Image.Origin360.Top:
                return 90f;
            case Image.Origin360.Left:
                return 180f;
            case Image.Origin360.Bottom:
            default:
                return -90f;
        }
    }

    private bool ShouldShowLabel(PIChartEntry _entry)
    {
        return _entry != null &&
               !string.IsNullOrWhiteSpace(_entry.label) &&
               Mathf.Max(0f, _entry.value) >= minLabelValue;
    }

    private Color ResolveSliceColor(int _index, PIChartEntry _entry, HashSet<Color32> _usedColors)
    {
        if (_entry != null && _entry.useCustomColor)
        {
            AddUsedColor(_usedColors, _entry.color);
            return _entry.color;
        }

        if (TryGetDefaultColor(_index, _usedColors, out Color defaultColor))
        {
            AddUsedColor(_usedColors, defaultColor);
            return defaultColor;
        }

        Color randomColor = GetRandomColor(_entry != null ? _entry.label : string.Empty, _usedColors);
        AddUsedColor(_usedColors, randomColor);
        return randomColor;
    }

    private bool TryGetDefaultColor(int _index, HashSet<Color32> _usedColors, out Color _color)
    {
        if (defaultColors != null && _index >= 0 && _index < defaultColors.Length)
        {
            Color color = defaultColors[_index];
            if (_usedColors == null || !_usedColors.Contains((Color32)color))
            {
                _color = color;
                return true;
            }
        }

        _color = Color.white;
        return false;
    }

    private Color GetRandomColor(string label, HashSet<Color32> usedColors)
    {
        if (!string.IsNullOrWhiteSpace(label) &&
            cachedRandomColorsByLabel.TryGetValue(label, out Color cachedColor) &&
            (usedColors == null || !usedColors.Contains((Color32)cachedColor)))
        {
            return cachedColor;
        }

        Color randomColor = CreateRandomColor(usedColors);
        if (!string.IsNullOrWhiteSpace(label))
        {
            cachedRandomColorsByLabel[label] = randomColor;
        }

        return randomColor;
    }

    private Color CreateRandomColor(HashSet<Color32> usedColors)
    {
        Color randomColor = Color.white;
        for (int i = 0; i < RandomColorMaxAttempts; i++)
        {
            randomColor = UnityEngine.Random.ColorHSV(0f, 1f, 0.55f, 1f, 0.65f, 1f, 1f, 1f);
            if (usedColors == null || !usedColors.Contains((Color32)randomColor))
            {
                return randomColor;
            }
        }

        return randomColor;
    }

    private void AddUsedColor(HashSet<Color32> usedColors, Color color)
    {
        if (usedColors != null)
        {
            usedColors.Add((Color32)color);
        }
    }
}
