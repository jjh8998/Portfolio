using UnityEngine;
using System.Text;
using System;
using System.Collections.Generic;

public static class CSVParserUtility
{
    private static readonly string[] DefaultCsvResourceFolders =
    {
        "CSVs",
        "Management/CSVs",
        string.Empty
    };

    public static Dictionary<string, int> BuildHeaderMap(List<string> _headerRow)
    {
        Dictionary<string, int> headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (_headerRow == null)
            return headerMap;

        for (int i = 0; i < _headerRow.Count; i++)
        {
            string normalizedHeader = NormalizeHeader(_headerRow[i]);
            if (string.IsNullOrEmpty(normalizedHeader) || headerMap.ContainsKey(normalizedHeader))
                continue;

            headerMap.Add(normalizedHeader, i);
        }

        return headerMap;
    }

    public static string GetColumnValue(
        List<string> _row,
        Dictionary<string, int> _headerMap,
        string _columnName,
        int _fallbackIndex,
        string _defaultValue = "")
    {
        if (_row == null)
            return _defaultValue;

        if (_headerMap != null
            && !string.IsNullOrWhiteSpace(_columnName)
            && _headerMap.TryGetValue(NormalizeHeader(_columnName), out int columnIndex))
        {
            return GetColumnValue(_row, columnIndex, _defaultValue);
        }

        return GetColumnValue(_row, _fallbackIndex, _defaultValue);
    }

    public static string GetColumnValue(
        List<string> _row,
        int _index,
        string _defaultValue = "")
    {
        if (_row == null || _index < 0 || _index >= _row.Count)
            return _defaultValue;

        return _row[_index] ?? _defaultValue;
    }

    public static bool TryResolveCsvResource(ref TextAsset _csvFile, string _logPrefix, params string[] _resourcePaths)
    {
        if (_csvFile != null)
            return true;

        HashSet<string> exactPaths = BuildCsvResourcePathCandidates(_resourcePaths);
        foreach (string path in exactPaths)
        {
            _csvFile = Resources.Load<TextAsset>(path);
            if (_csvFile != null)
                return true;
        }

        HashSet<string> normalizedKeys = BuildCsvNormalizedKeys(_resourcePaths);
        if (normalizedKeys.Count > 0)
        {
            for (int i = 0; i < DefaultCsvResourceFolders.Length; i++)
            {
                TextAsset[] csvAssets = Resources.LoadAll<TextAsset>(DefaultCsvResourceFolders[i]);
                for (int j = 0; j < csvAssets.Length; j++)
                {
                    TextAsset asset = csvAssets[j];
                    if (asset == null)
                        continue;

                    string assetKey = NormalizeResourceLookupKey(asset.name);
                    if (!normalizedKeys.Contains(assetKey))
                        continue;

                    _csvFile = asset;
                    return true;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(_logPrefix))
            Debug.LogError($"{_logPrefix} : CSV file not found in Resources.");

        return false;
    }

    public static int GetOptionalInt(
        List<string> _row,
        Dictionary<string, int> _headerMap,
        string _columnName,
        int _fallbackIndex,
        int _defaultValue,
        string _logPrefix = "",
        int _rowIndex = -1)
    {
        string value = GetColumnValue(_row, _headerMap, _columnName, _fallbackIndex);
        string trimmed = value.Trim();

        if (string.IsNullOrEmpty(trimmed))
            return _defaultValue;

        return ParseInt(trimmed, _logPrefix, _rowIndex, _columnName, _defaultValue);
    }

    public static int GetOptionalIntByHeaders(
        List<string> _row,
        Dictionary<string, int> _headerMap,
        int _defaultValue,
        string _logPrefix = "",
        int _rowIndex = -1,
        params string[] _columnNames)
    {
        if (_columnNames == null || _columnNames.Length == 0)
            return _defaultValue;

        for (int i = 0; i < _columnNames.Length; i++)
        {
            string columnName = _columnNames[i];
            if (string.IsNullOrWhiteSpace(columnName))
                continue;

            if (_headerMap != null && _headerMap.TryGetValue(NormalizeHeader(columnName), out int columnIndex))
                return GetOptionalInt(_row, _headerMap, columnName, columnIndex, _defaultValue, _logPrefix, _rowIndex);
        }

        return _defaultValue;
    }

    public static string GetOptionalStringByHeaders(
        List<string> _row,
        Dictionary<string, int> _headerMap,
        string _defaultValue = "",
        params string[] _columnNames)
    {
        if (_columnNames == null || _columnNames.Length == 0)
            return _defaultValue;

        for (int i = 0; i < _columnNames.Length; i++)
        {
            string columnName = _columnNames[i];
            if (string.IsNullOrWhiteSpace(columnName))
                continue;

            if (_headerMap != null && _headerMap.TryGetValue(NormalizeHeader(columnName), out int columnIndex))
            {
                string value = GetColumnValue(_row, columnIndex, _defaultValue);
                return string.IsNullOrWhiteSpace(value) ? _defaultValue : value.Trim();
            }
        }

        return _defaultValue;
    }

    public static string GetOptionalStringByHeaders(
        List<string> _row,
        Dictionary<string, int> _headerMap,
        int _fallbackIndex,
        string _defaultValue = "",
        params string[] _columnNames)
    {
        string value = GetOptionalStringByHeaders(_row, _headerMap, null, _columnNames);
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        return GetColumnValue(_row, _fallbackIndex, _defaultValue).Trim();
    }

    public static int ParseInt(
        string _value,
        string _logPrefix = "",
        int _rowIndex = -1,
        string _columnName = "",
        int _defaultValue = 0)
    {
        string trimmed = string.IsNullOrWhiteSpace(_value) ? string.Empty : _value.Trim();

        if (string.IsNullOrEmpty(trimmed))
            return _defaultValue;

        if (int.TryParse(trimmed, out int result))
            return result;

        if (!string.IsNullOrWhiteSpace(_logPrefix))
            Debug.LogWarning($"{_logPrefix} : Failed to parse int at row {_rowIndex}, column '{_columnName}', value '{trimmed}'. Using {_defaultValue}.");

        return _defaultValue;
    }

    public static string NormalizeHeader(string _header)
    {
        if (string.IsNullOrWhiteSpace(_header))
            return string.Empty;

        return _header.Trim().TrimStart('\uFEFF');
    }

    public static string NormalizeResourceLookupKey(string _value)
    {
        if (string.IsNullOrWhiteSpace(_value))
            return string.Empty;

        string normalized = _value.Replace('\\', '/').Trim();
        int slashIndex = normalized.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex + 1 < normalized.Length)
            normalized = normalized.Substring(slashIndex + 1);

        int dotIndex = normalized.LastIndexOf('.');
        if (dotIndex > 0)
            normalized = normalized.Substring(0, dotIndex);

        normalized = normalized
            .Replace("Buliding", "Building", StringComparison.OrdinalIgnoreCase)
            .Replace("buliding", "building", StringComparison.OrdinalIgnoreCase);

        StringBuilder builder = new StringBuilder(normalized.Length);
        for (int i = 0; i < normalized.Length; i++)
        {
            char c = normalized[i];
            if (char.IsLetterOrDigit(c))
                builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    public static List<string> ParseCsvLine(string _line)
    {
        List<string> result = new List<string>();
        if (_line == null)
        {
            result.Add(string.Empty);
            return result;
        }

        bool inQuotes = false;
        StringBuilder cell = new StringBuilder();

        for (int i = 0; i < _line.Length; i++)
        {
            char c = _line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < _line.Length && _line[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                    continue;
                }

                inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(cell.ToString());
                cell.Clear();
                continue;
            }

            cell.Append(c);
        }

        result.Add(cell.ToString());
        return result;
    }

    private static HashSet<string> BuildCsvResourcePathCandidates(string[] _resourcePaths)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (_resourcePaths == null)
            return result;

        for (int i = 0; i < _resourcePaths.Length; i++)
        {
            string originalPath = _resourcePaths[i];
            if (string.IsNullOrWhiteSpace(originalPath))
                continue;

            string trimmedPath = originalPath.Trim().Replace('\\', '/');
            AddCsvResourcePathCandidate(result, trimmedPath);

            if (trimmedPath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                AddCsvResourcePathCandidate(result, trimmedPath.Substring(0, trimmedPath.Length - 4));

            if (!trimmedPath.StartsWith("CSVs/", StringComparison.OrdinalIgnoreCase)
                && !trimmedPath.StartsWith("Management/CSVs/", StringComparison.OrdinalIgnoreCase))
            {
                AddCsvResourcePathCandidate(result, $"CSVs/{trimmedPath}");
                AddCsvResourcePathCandidate(result, $"Management/CSVs/{trimmedPath}");
            }
        }

        return result;
    }

    private static void AddCsvResourcePathCandidate(HashSet<string> _paths, string _path)
    {
        if (_paths == null || string.IsNullOrWhiteSpace(_path))
            return;

        _paths.Add(_path);

        string buildingPath = _path.Replace("Buliding", "Building", StringComparison.OrdinalIgnoreCase);
        string bulidingPath = _path.Replace("Building", "Buliding", StringComparison.OrdinalIgnoreCase);

        _paths.Add(buildingPath);
        _paths.Add(bulidingPath);
    }

    private static HashSet<string> BuildCsvNormalizedKeys(string[] _resourcePaths)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (_resourcePaths == null)
            return result;

        for (int i = 0; i < _resourcePaths.Length; i++)
        {
            string key = NormalizeResourceLookupKey(_resourcePaths[i]);
            if (!string.IsNullOrWhiteSpace(key))
                result.Add(key);
        }

        return result;
    }
}
