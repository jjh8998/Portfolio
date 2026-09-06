using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 도시의 소유권 전환을 중앙에서 관리하는 싱글턴 컴포넌트입니다.
/// 게임 내에서 도시 소유자가 변경될 때 관련 컬렉션을 업데이트하고
/// 변경 이벤트를 발행합니다.
/// </summary>
public class CityOwnershipManager : MonoBehaviour
{
    public static CityOwnershipManager instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// 소유권 전환 시 가능한 결과 코드입니다.
    /// </summary>
    public enum TransferResult
    {
        Success, // 전환 성공
        InvalidCity, // 유효하지 않은 도시 참조
        InvalidNewOwner, // 유효하지 않은(또는 null) 새 소유주
        SameOwner // 이미 같은 소유자여서 전환할 필요 없음
    }

    /// <summary>
    /// 도시 소유자 변경 시 발생하는 이벤트.
    /// 파라미터: (변경된 도시, 이전 소유자, 새로운 소유자)
    /// </summary>
    public event Action<CityScript, FactionManager, FactionManager> AnyCityOwnerChanged;

    /// <summary>
    /// 주어진 도시의 소유권을 새로운 소유주로 변경합니다.
    /// 내부적으로 이전 소유자의 도시 목록에서 제거하고, 새로운 소유자의 목록에 추가합니다.
    /// 이벤트를 통해 외부에 변경 사실을 알립니다.
    public TransferResult Transfer(CityScript _city, FactionManager _newOwner, bool _takeAllOldOwnerSharesIfEliminated = false)
    {
        // 입력 검증
        if (_city == null) return TransferResult.InvalidCity;
        if (_newOwner == null) return TransferResult.InvalidNewOwner;

        // 현재 소유자 가져오기
        FactionManager oldOwner = _city.cityData.owner;

        // 소유자가 동일하면 아무것도 하지 않음
        if (ReferenceEquals(oldOwner, _newOwner)) return TransferResult.SameOwner;

        // 이전 소유자의 도시 컬렉션에서 제거
        if (oldOwner != null && oldOwner.HasCity(_city))
            oldOwner.RemoveCity(_city);

        // 새 소유자에 도시가 없다면 추가
        if (!_newOwner.HasCity(_city))
            _newOwner.AddCity(_city);

        // 도시 데이터에 소유자 설정
        _city.cityData.owner = _newOwner;

        // 변경 이벤트 발생 (null-safe)
        AnyCityOwnerChanged?.Invoke(_city, oldOwner, _newOwner);

        if (_takeAllOldOwnerSharesIfEliminated
            && oldOwner != null
            && oldOwner.ownedCities != null
            && oldOwner.ownedCities.Count == 0)
        {
            if (CityShareManager.instance != null)
                CityShareManager.instance.TransferAllShares(oldOwner, _newOwner);

            oldOwner.TryEliminateByCityLoss();
        }

        return TransferResult.Success;
    }
}
