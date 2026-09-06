using System;
using System.Collections.Generic;

namespace Common.Randomization
{
    /// <summary>
    /// 가중치 그룹 기반 랜덤 선택기
    /// 반드시 외부에서 Random을 주입해야 한다.
    /// </summary>
    public static class RandomDrawScript
    {
        /// <summary>
        /// 가중치 기반으로 그룹을 선택하고,
        /// 해당 그룹 내부에서 균등 랜덤으로 아이템을 반환한다.
        /// </summary>
        public static T Pick<T>(IReadOnlyList<WeightedGroup<T>> _groups, Random _random)
        {
            if (_groups == null)
                throw new ArgumentNullException(nameof(_groups));

            if (_random == null)
                throw new ArgumentNullException(nameof(_random));

            int groupIndex = PickGroupIndex(_groups, _random);
            var group = _groups[groupIndex];

            int itemIndex = _random.Next(group.items.Count);
            return group.items[itemIndex];
        }

        /// <summary>
        /// 그룹 인덱스만 반환 (후처리용)
        /// </summary>
        public static int PickGroupIndex<T>(IReadOnlyList<WeightedGroup<T>> _groups, Random _random)
        {
            if (_groups == null)
                throw new ArgumentNullException(nameof(_groups));

            if (_random == null)
                throw new ArgumentNullException(nameof(_random));

            float totalWeight = 0f;

            for (int i = 0; i < _groups.Count; i++)
            {
                var g = _groups[i];
                if (g == null || !g.IsValid())
                    continue;

                totalWeight += g.weight;
            }

            if (totalWeight <= 0f)
                throw new InvalidOperationException("No valid group to pick from.");

            double roll = _random.NextDouble() * totalWeight;
            float cumulative = 0f;

            for (int i = 0; i < _groups.Count; i++)
            {
                var g = _groups[i];
                if (g == null || !g.IsValid())
                    continue;

                cumulative += g.weight;

                if (roll < cumulative)
                    return i;
            }

            // fallback (float 오차 대응)
            for (int i = _groups.Count - 1; i >= 0; i--)
            {
                var g = _groups[i];
                if (g != null && g.IsValid())
                    return i;
            }

            throw new InvalidOperationException("Failed to pick group.");
        }

        /// <summary>
        /// 여러 개 추첨 (중복 허용)
        /// </summary>
        public static List<T> PickMany<T>(IReadOnlyList<WeightedGroup<T>> _groups, int _count, Random _random)
        {
            if (_count < 0)
                throw new ArgumentOutOfRangeException(nameof(_count));

            List<T> results = new(_count);

            for (int i = 0; i < _count; i++)
            {
                results.Add(Pick(_groups, _random));
            }

            return results;
        }

        /// <summary>
        /// 유효성 검사
        /// </summary>
        public static bool CanPick<T>(IReadOnlyList<WeightedGroup<T>> _groups)
        {
            if (_groups == null || _groups.Count == 0)
                return false;

            float totalWeight = 0f;

            for (int i = 0; i < _groups.Count; i++)
            {
                var g = _groups[i];
                if (g == null || !g.IsValid())
                    continue;

                totalWeight += g.weight;
            }

            return totalWeight > 0f;
        }

        /// <summary>
        /// 디버그용 상세 검증
        /// </summary>
        public static string Validate<T>(IReadOnlyList<WeightedGroup<T>> _groups)
        {
            if (_groups == null)
                return "Groups is null.";

            if (_groups.Count == 0)
                return "Groups is empty.";

            float totalWeight = 0f;
            bool hasValid = false;

            for (int i = 0; i < _groups.Count; i++)
            {
                var g = _groups[i];

                if (g == null)
                    continue;

                if (!g.IsValid())
                    continue;

                hasValid = true;
                totalWeight += g.weight;
            }

            if (!hasValid)
                return "No valid group.";

            if (totalWeight <= 0f)
                return "Total weight must be > 0.";

            return string.Empty;
        }
    }
}
