using System;
using System.Collections.Generic;

namespace Common.Randomization
{
    /// <summary>
    /// 가중치 그룹 데이터
    /// 그룹이 선택된 후 items 내부에서 균등 랜덤으로 하나 선택된다.
    /// </summary>
    [Serializable]
    public sealed class WeightedGroup<T>
    {
        public float weight = 1f;
        public List<T> items = new();

        public WeightedGroup() { }

        public WeightedGroup(float _weight, IEnumerable<T> _items)
        {
            weight = _weight;
            items = _items != null ? new List<T>(_items) : new List<T>();
        }

        public bool IsValid()
        {
            return weight > 0f && items != null && items.Count > 0;
        }
    }
}
