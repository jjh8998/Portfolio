using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;

/// <summary>
/// 전쟁(Claim) 요청을 처리하고, CardGame 씬 전환 및 복귀 시 소유권 이전을 담당합니다.
/// </summary>
public class WarManager : MonoBehaviour
{
    private const int QuickSaveSlotIndex = 0;
    private const int DefaultBattleEnemyMaxHp = 50;
    private const string CeoDatabaseResourcePath = "Databases/CEODatabase";

    private bool isBattleRunning;
    private bool isPlayerDefenseDeckSelectionPending;
    private static CEODatabaseSO ceoDatabaseCache;

    public bool DeveloperMode => IsWarWithoutShareAllowed();
    public bool DeveloperModePlayerAsDefender => IsPlayerAsDefenderWarTestAllowed();

    private void Start()
    {
        // 전투 결과가 있으면 다른 초기화 완료 후 처리
        if (BattleSceneData.HasResult)
            StartCoroutine(ProcessBattleResultDelayed());
    }

    private IEnumerator ProcessBattleResultDelayed()
    {
        // 모든 Start()와 GiveStartingCity()가 완료될 때까지 충분히 대기
        yield return null;
        yield return null;
        ProcessBattleResult();
    }

    /// <summary>
    /// <summary>
    /// 전쟁을 선포하고 전투 씬으로 이동합니다.
    /// </summary>
    public bool DeclareWar(CityScript _city, FactionManager _attacker, System.Collections.Generic.List<string> _deckIds = null)
    {
        bool playerControlsDefender = IsPlayerDefenderBattle(_city, _attacker);
        return DeclareWarInternal(_city, _attacker, _deckIds, playerControlsDefender);
    }

    public bool DeclareDeveloperWarAgainstPlayer(CityScript _attackerCity, FactionManager _playerDefender, System.Collections.Generic.List<string> _playerDeckIds = null)
    {
        if (!DeveloperModePlayerAsDefender)
        {
            Debug.LogWarning("[WarManager] Developer defender battle requested while developer defender mode is disabled.");
            return false;
        }

        FactionManager enemyAttacker = _attackerCity != null && _attackerCity.cityData != null
            ? _attackerCity.cityData.owner
            : null;
        CityScript playerTargetCity = FindFirstCityOwnedBy(_playerDefender);

        if (enemyAttacker == null || _playerDefender == null || playerTargetCity == null)
        {
            Debug.LogWarning("[WarManager] Developer defender battle cancelled because attacker faction, player faction, or player target city could not be resolved.");
            return false;
        }

        if (ReferenceEquals(enemyAttacker, _playerDefender))
            return false;

        Debug.LogWarning(
            $"[WarManager] Developer defender battle enabled. Using existing DeclareWar path. Attacker='{enemyAttacker.factionName}', Defender='{_playerDefender.factionName}', TargetCity='{playerTargetCity.cityData.cityName}'.");

        return DeclareWarInternal(playerTargetCity, enemyAttacker, _playerDeckIds, true);
    }

    /// <summary>
    /// 플레이어 자기 도시의 스캐빈저 점거 슬롯을 해금하기 위한 전투를 시작합니다.
    /// 도시 소유권 이전이 아니라, 승리 시 해당 슬롯 잠금만 해제됩니다.
    /// </summary>
    public bool DeclareScavengerBattle(CityScript _city, FactionManager _player, int _slotIndex, System.Collections.Generic.List<string> _deckIds)
    {
        if (_city == null || _city.cityData == null || _player == null)
        {
            Debug.LogWarning("[WarManager] DeclareScavengerBattle cancelled: city or player is null.");
            return false;
        }

        if (isBattleRunning)
            return false;

        if (!_city.IsSlotScavengerLocked(_slotIndex))
        {
            Debug.LogWarning($"[WarManager] DeclareScavengerBattle cancelled: slot {_slotIndex} is not scavenger-locked in '{_city.cityData.cityName}'.");
            return false;
        }

        SaveManager saveManager = FindFirstObjectByType<SaveManager>();

        Debug.Log("[WarManager] Saving current game before scavenger battle scene transition.");

        if (saveManager == null || !saveManager.SaveCurrentGame(QuickSaveSlotIndex))
        {
            Debug.LogWarning("[WarManager] Scavenger battle scene transition aborted because save failed.");
            return false;
        }

        BattleSceneData.Clear();
        BattleSceneData.ScavengerSlotUnlock = true;
        BattleSceneData.ScavengerTargetCityName = _city.cityData.cityName;
        BattleSceneData.ScavengerTargetSlotIndex = _slotIndex;
        BattleSceneData.TargetCityName = _city.cityData.cityName;

        BattleSceneData.PlayerControlsDefender = false;
        BattleSceneData.PlayerFactionName = _player.GetSaveKey();
        BattleSceneData.AttackerFactionName = _player.GetSaveKey();

        // 적은 ScavengerEnemyScript(덱/HP/AI)가 CardGame 씬에서 직접 구성한다.
        BattleSceneData.EnemyFactionName = "스캐빈저";
        BattleSceneData.EnemyMaxHp = 0;
        BattleSceneData.AttackerModifiers = BattleModifierCollector.Collect(_player);
        BattleSceneData.DefenderModifiers = BattleModifierSnapshot.Empty();

        if (_deckIds != null)
            BattleSceneData.PlayerDeckIds = new System.Collections.Generic.List<string>(_deckIds);

        isBattleRunning = true;
        Debug.Log("[WarManager] Save completed. Loading scavenger battle scene.");
        SceneManager.LoadScene("CardGame");
        return true;
    }

    private bool DeclareWarInternal(CityScript _city, FactionManager _attacker, System.Collections.Generic.List<string> _deckIds, bool _playerControlsDefender)
    {
        if (_city == null || _attacker == null) return false;
        if (isBattleRunning) return false;

        FactionManager attacker = _attacker;
        FactionManager defender = _city.cityData.owner;
        CityScript targetCity = _city;

        if (attacker == null || defender == null || targetCity == null)
        {
            Debug.LogWarning("[WarManager] DeclareWar cancelled because attacker, defender, or target city could not be resolved.");
            return false;
        }

        if (ReferenceEquals(defender, attacker)) return false;

        CityShareManager cityShareManager = CityShareManager.instance != null
            ? CityShareManager.instance
            : FindFirstObjectByType<CityShareManager>();

        if (IsWarWithoutShareAllowed())
        {
            Debug.LogWarning($"[WarManager] DeveloperModeManager option enabled. DeclareWar is bypassing city share requirements for '{targetCity.cityData.cityName}'.");
        }
        else if (cityShareManager == null)
        {
            Debug.LogWarning("[WarManager] CityShareManager not found. DeclareWar is using the legacy ownership check.");
        }
        else if (!cityShareManager.TryPrepareCityShares(targetCity, out string sharePrepareMessage))
        {
            Debug.LogWarning($"[WarManager] City share data is unavailable for '{targetCity.cityData.cityName}'. DeclareWar is using the legacy fallback. Reason: {sharePrepareMessage}");
        }
        else if (!cityShareManager.CanClaimOwnership(targetCity, attacker, out string shareClaimMessage))
        {
            Debug.LogWarning($"[WarManager] DeclareWar blocked for '{targetCity.cityData.cityName}'. Reason: {shareClaimMessage}");
            return false;
        }

        NationAIController attackerAIController = FindNationAIController(attacker);
        NationAIController defenderAIController = FindNationAIController(defender);
        if (attackerAIController != null && defenderAIController != null)
        {
            AIWarResolver aiWarResolver = FindFirstObjectByType<AIWarResolver>();
            if (aiWarResolver == null)
            {
                Debug.LogWarning("[WarManager] AIWarResolver not found. AI vs AI war was cancelled.");
                return false;
            }

            bool resolved = aiWarResolver.TryResolveAIWar(targetCity, attacker, out bool attackerWon);
            if (resolved)
            {
                NotifyAttackerTargetCityWarResult(attackerAIController, targetCity, attacker, defender, attackerWon);

                GameNewsUIController newsUI = FindFirstObjectByType<GameNewsUIController>();
                if (newsUI != null)
                    newsUI.ShowAIWarResult(attacker, defender, targetCity, attackerWon);
            }

            return resolved;
        }

        FactionManager playerSideFaction = _playerControlsDefender ? defender : attacker;
        FactionManager enemySideFaction = _playerControlsDefender ? attacker : defender;

        if (_playerControlsDefender
            && playerSideFaction != null
            && playerSideFaction.IsPlayerFaction
            && _deckIds == null
            && !HasValidSavedDeck(playerSideFaction))
        {
            return HandleMissingPlayerDefenseDeck(targetCity, attacker, defender, playerSideFaction, attackerAIController);
        }

        SaveManager saveManager = FindFirstObjectByType<SaveManager>();

        Debug.Log("[WarManager] Saving current game before battle scene transition.");

        if (saveManager == null || !saveManager.SaveCurrentGame(QuickSaveSlotIndex))
        {
            Debug.LogWarning("[WarManager] Battle scene transition aborted because save failed.");
            return false;
        }

        // 전투 데이터를 정적 클래스에 저장
        BattleSceneData.Clear();
        BattleSceneData.IsTutorial = TutorialManager.ShouldStartBattleTutorialOnNextWar();
        BattleSceneData.AttackerFactionName = attacker.GetSaveKey();
        BattleSceneData.DefenderFactionName = defender.GetSaveKey();
        BattleSceneData.PlayerControlsDefender = _playerControlsDefender;
        BattleSceneData.TargetCityName = targetCity.cityData.cityName;

        BattleSceneData.PlayerFactionName = playerSideFaction != null ? playerSideFaction.GetSaveKey() : null;

        BattleSceneData.EnemyFactionName = enemySideFaction != null ? enemySideFaction.factionName : null;
        BattleSceneData.EnemyCeoId = ResolveEnemyCeoId(enemySideFaction);
        BattleSceneData.EnemyCeoProfileSprite = ResolveEnemyCeoProfileSprite(BattleSceneData.EnemyCeoId);
        BattleSceneData.EnemyMaxHp = DefaultBattleEnemyMaxHp;
        BattleSceneData.AttackerModifiers = BattleModifierCollector.Collect(playerSideFaction);
        BattleSceneData.DefenderModifiers = BattleModifierCollector.Collect(enemySideFaction);

        NationAIController enemyAI = FindNationAIController(enemySideFaction);
        BattleSceneData.EnemyPersonality = ClonePersonality(enemyAI);
        if (BattleSceneData.EnemyPersonality == null)
        {
            string enemyName = enemySideFaction != null ? enemySideFaction.factionName : "Unknown";
            Debug.LogWarning($"[WarManager] Enemy NationAIController personality를 찾지 못했습니다. Fallback personality를 사용합니다. Enemy={enemyName}");
        }

        // 적 측 AI가 실제 보유한 카드 인벤토리 풀 전달 (덱 구성은 InventoryDeckBuilder가 수행)
        BattleSceneData.EnemyInventoryCardIds = BuildEnemyDeckIdsFromInventory(enemySideFaction);

        System.Collections.Generic.List<string> playerDeckIds = _deckIds ?? ResolvePlayerBattleDeckIds(playerSideFaction);
        if (playerDeckIds != null)
        {
            BattleSceneData.PlayerDeckIds = new System.Collections.Generic.List<string>(playerDeckIds);
        }

        isBattleRunning = true;
        Debug.Log("[WarManager] Save completed. Loading battle scene.");
        SceneManager.LoadScene("CardGame");
        return true;
    }

    private static bool IsPlayerDefenderBattle(CityScript city, FactionManager attacker)
    {
        FactionManager defender = city != null && city.cityData != null
            ? city.cityData.owner
            : null;

        return defender != null
            && defender.IsPlayerFaction
            && (attacker == null || !attacker.IsPlayerFaction);
    }

    private static System.Collections.Generic.List<string> ResolvePlayerBattleDeckIds(FactionManager playerSideFaction)
    {
        if (playerSideFaction == null || !playerSideFaction.IsPlayerFaction)
            return null;

        System.Collections.Generic.List<string> deckIds = playerSideFaction.GetSavedDeck();
        return deckIds != null && deckIds.Count >= Deck.MinDeckSize ? deckIds : null;
    }

    private bool HandleMissingPlayerDefenseDeck(
        CityScript targetCity,
        FactionManager attacker,
        FactionManager defender,
        FactionManager playerSideFaction,
        NationAIController attackerAIController)
    {
        CardInventory inventory = playerSideFaction != null ? playerSideFaction.GetCardInventory() : null;
        int cardCount = inventory != null ? inventory.Count : 0;

        if (cardCount < Deck.MinDeckSize)
        {
            Debug.LogWarning($"[WarManager] Player defender has no valid deck and not enough cards. Auto defeat. Cards={cardCount}/{Deck.MinDeckSize}");
            ResolvePlayerDefenseAutoDefeat(targetCity, attacker, defender, attackerAIController);
            return true;
        }

        if (isPlayerDefenseDeckSelectionPending)
        {
            Debug.LogWarning("[WarManager] Player defense deck selection is already pending. Duplicate war request ignored.");
            return false;
        }

        DeckBuilderUI deckBuilder = DeckBuilderUI.Instance != null
            ? DeckBuilderUI.Instance
            : UnityEngine.Object.FindAnyObjectByType<DeckBuilderUI>(FindObjectsInactive.Include);

        if (deckBuilder == null)
        {
            Debug.LogWarning("[WarManager] DeckBuilderUI not found. Player defense battle is pending but cannot open required deck builder.");
            return false;
        }

        isPlayerDefenseDeckSelectionPending = true;
        deckBuilder.OpenForRequiredDefenseDeck(playerSideFaction, deckIds =>
        {
            isPlayerDefenseDeckSelectionPending = false;

            if (deckIds == null || deckIds.Count < Deck.MinDeckSize)
            {
                Debug.LogWarning("[WarManager] Required defense deck confirmation returned an invalid deck. Battle start cancelled.");
                return;
            }

            DeclareWarInternal(targetCity, attacker, deckIds, true);
        });

        return true;
    }

    private void ResolvePlayerDefenseAutoDefeat(
        CityScript targetCity,
        FactionManager attacker,
        FactionManager defender,
        NationAIController attackerAIController)
    {
        const bool attackerWon = true;

        NotifyAttackerTargetCityWarResult(attackerAIController, targetCity, attacker, defender, attackerWon);

        if (CityOwnershipManager.instance != null)
            CityOwnershipManager.instance.Transfer(targetCity, attacker, true);
        else
            Debug.LogWarning("[WarManager] CityOwnershipManager not found. Player defense auto defeat could not transfer city ownership.");

        GameNewsUIController newsUI = FindFirstObjectByType<GameNewsUIController>();
        if (newsUI != null)
        {
            string attackerName = attacker != null && !string.IsNullOrWhiteSpace(attacker.factionName)
                ? attacker.factionName
                : "Unknown";
            string defenderName = defender != null && !string.IsNullOrWhiteSpace(defender.factionName)
                ? defender.factionName
                : "Unknown";
            string cityName = targetCity != null && targetCity.cityData != null && !string.IsNullOrWhiteSpace(targetCity.cityData.cityName)
                ? targetCity.cityData.cityName
                : "Unknown";

            newsUI.ShowNews($"[NEWS] {defenderName}이 방어 덱을 구성할 카드가 부족해 {cityName} 방어전에서 패배했습니다. 승자: {attackerName}");
        }

        SaveManager saveManager = FindFirstObjectByType<SaveManager>();
        if (saveManager != null)
            saveManager.SaveCurrentGame(QuickSaveSlotIndex);
    }

    private static bool HasValidSavedDeck(FactionManager faction)
    {
        if (faction == null)
            return false;

        System.Collections.Generic.List<string> deckIds = faction.GetSavedDeck();
        return deckIds != null && deckIds.Count >= Deck.MinDeckSize;
    }

    public static void PrepareTutorialBattleEnemy(CityScript city)
    {
        FactionManager owner = city != null && city.cityData != null
            ? city.cityData.owner
            : null;

        if (owner == null)
            return;

        CEOData ceo = ResolveEnemyCeo(owner);

        BattleSceneData.EnemyFactionName = owner.factionName;
        BattleSceneData.EnemyCeoId = ceo != null ? ceo.id : owner.ceoId;
        BattleSceneData.EnemyCeoProfileSprite = ceo != null ? ceo.profileSprite : null;
    }

    private static string ResolveEnemyCeoId(FactionManager defender)
    {
        if (defender == null)
            return null;

        CEOData ceo = ResolveEnemyCeo(defender);
        if (ceo != null)
            return ceo.id;

        return !string.IsNullOrWhiteSpace(defender.ceoId) ? defender.ceoId : null;
    }

    private static CEOData ResolveEnemyCeo(FactionManager defender)
    {
        if (defender == null)
            return null;

        CEODatabaseSO ceoDatabase = GetCeoDatabase();
        CEOData ceo = null;

        if (ceoDatabase != null && !string.IsNullOrWhiteSpace(defender.ceoId))
            ceo = ceoDatabase.GetCEOByID(defender.ceoId);

        if (ceo == null && ceoDatabase != null && !string.IsNullOrWhiteSpace(defender.factionName))
            ceo = ceoDatabase.GetCEOByName(defender.factionName);

        return ceo;
    }

    private static CEODatabaseSO GetCeoDatabase()
    {
        if (ceoDatabaseCache == null)
            ceoDatabaseCache = Resources.Load<CEODatabaseSO>(CeoDatabaseResourcePath);

        if (ceoDatabaseCache == null)
        {
            ceoDatabaseCache = ScriptableObject.CreateInstance<CEODatabaseSO>();
            ceoDatabaseCache.LoadCSV();
        }

        return ceoDatabaseCache;
    }

    private static Sprite ResolveEnemyCeoProfileSprite(string ceoId)
    {
        if (string.IsNullOrWhiteSpace(ceoId))
            return null;

        CEODatabaseSO ceoDatabase = GetCeoDatabase();
        CEOData ceo = ceoDatabase != null ? ceoDatabase.GetCEOByID(ceoId) : null;
        return ceo != null ? ceo.profileSprite : null;
    }
    /// <summary>
    /// Management 씬 복귀 시 전투 결과가 있으면 카드 소모 및 소유권을 처리합니다.
    /// </summary>
    private void ProcessBattleResult()
    {
        if (!BattleSceneData.HasResult) return;

        if (BattleSceneData.ScavengerSlotUnlock)
        {
            ProcessScavengerBattleResult();
            return;
        }

        string cityName = BattleSceneData.TargetCityName;
        string attackerId = BattleSceneData.AttackerFactionName;
        string defenderId = BattleSceneData.DefenderFactionName;
        string playerId = BattleSceneData.PlayerFactionName;
        bool attackerWon = BattleSceneData.AttackerWon;
        var consumedCardIds = new System.Collections.Generic.List<string>(BattleSceneData.ConsumedCardIds);

        BattleSceneData.Clear();

        FactionManager attacker = FindFactionById(attackerId);
        FactionManager defender = FindFactionById(defenderId);
        FactionManager playerSideFaction = FindFactionById(playerId);
        if (playerSideFaction == null)
            playerSideFaction = attacker;
        CityScript targetCity = FindCityByName(cityName);
        if (defender == null && targetCity != null && targetCity.cityData != null)
            defender = targetCity.cityData.owner;
        NationAIController attackerAIController = FindNationAIController(attacker);
        NotifyAttackerTargetCityWarResult(attackerAIController, targetCity, attacker, defender, attackerWon);

        // 승패 무관하게 플레이어가 전투에서 직접 사용한 카드 소모
        if (playerSideFaction != null && consumedCardIds.Count > 0)
            playerSideFaction.ConsumeCards(consumedCardIds);

        if (!attackerWon) return;

        if (targetCity == null || attacker == null) return;

        // CityOwnershipManager를 통한 소유권 이전
        if (CityOwnershipManager.instance != null)
            CityOwnershipManager.instance.Transfer(targetCity, attacker, true);
    }

    /// <summary>
    /// 스캐빈저 슬롯 해금 전투 결과 처리. 승리 시 대상 슬롯의 잠금을 해제한다.
    /// 소유권 이전은 하지 않는다.
    /// </summary>
    private void ProcessScavengerBattleResult()
    {
        string cityName = BattleSceneData.ScavengerTargetCityName;
        int slotIndex = BattleSceneData.ScavengerTargetSlotIndex;
        string playerId = BattleSceneData.PlayerFactionName;
        bool attackerWon = BattleSceneData.AttackerWon;
        var consumedCardIds = new System.Collections.Generic.List<string>(BattleSceneData.ConsumedCardIds);

        BattleSceneData.Clear();

        FactionManager playerSideFaction = FindFactionById(playerId);

        // 승패 무관하게 플레이어가 실제 사용한 카드 소모
        if (playerSideFaction != null && consumedCardIds.Count > 0)
            playerSideFaction.ConsumeCards(consumedCardIds);

        if (!attackerWon)
            return;

        CityScript targetCity = FindCityByName(cityName);
        if (targetCity == null)
        {
            Debug.LogWarning($"[WarManager] Scavenger victory: target city '{cityName}' not found. Slot unlock skipped.");
            return;
        }

        if (!targetCity.UnlockScavengerSlot(slotIndex))
        {
            Debug.LogWarning($"[WarManager] Scavenger victory: slot {slotIndex} of '{cityName}' could not be unlocked.");
            return;
        }

        Debug.Log($"[WarManager] Scavenger victory: slot {slotIndex} of '{cityName}' unlocked.");

        // 해금 상태를 즉시 저장해 다음 로드에서도 유지되도록 한다.
        SaveManager saveManager = FindFirstObjectByType<SaveManager>();
        if (saveManager != null)
            saveManager.SaveCurrentGame(QuickSaveSlotIndex);
    }

    private void NotifyAttackerTargetCityWarResult(
        NationAIController attackerAIController,
        CityScript city,
        FactionManager attacker,
        FactionManager defender,
        bool attackerWon)
    {
        if (attackerAIController == null)
            return;

        FactionAITargetCityMemory targetCityMemory = attackerAIController.GetTargetCityMemory();
        if (targetCityMemory == null)
            return;

        targetCityMemory.NotifyWarResult(city, attacker, defender, attackerWon);
    }

    /// <summary>
    /// 방어자 팩션의 카드 인벤토리를 전투용 적 덱 ID 목록으로 변환한다.
    /// 같은 카드를 보유 수량만큼 반복 추가한다. 인벤토리가 비어 있으면 null을 반환하여
    /// BattleInitializer가 임시 덱으로 폴백하도록 한다.
    /// </summary>
    private static System.Collections.Generic.List<string> BuildEnemyDeckIdsFromInventory(FactionManager defender)
    {
        if (defender == null)
            return null;

        CardInventory inventory = defender.GetCardInventory();
        if (inventory == null)
            return null;

        System.Collections.Generic.Dictionary<string, int> cardCounts = inventory.GetCardCountMapForReadOnlyUse();
        if (cardCounts == null)
            return null;

        var ids = new System.Collections.Generic.List<string>(inventory.Count);
        foreach (System.Collections.Generic.KeyValuePair<string, int> pair in cardCounts)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value <= 0)
                continue;

            for (int i = 0; i < pair.Value; i++)
                ids.Add(pair.Key);
        }

        return ids.Count > 0 ? ids : null;
    }

    private CityScript FindFirstCityOwnedBy(FactionManager owner)
    {
        if (owner == null)
            return null;

        if (owner.ownedCities != null)
        {
            for (int i = 0; i < owner.ownedCities.Count; i++)
            {
                CityScript ownedCity = owner.ownedCities[i];
                if (ownedCity != null
                    && ownedCity.cityData != null
                    && ReferenceEquals(ownedCity.cityData.owner, owner))
                {
                    return ownedCity;
                }
            }
        }

        return FindObjectsByType<CityScript>(FindObjectsSortMode.None)
            .FirstOrDefault(c => c != null
                && c.cityData != null
                && ReferenceEquals(c.cityData.owner, owner));
    }

    private CityScript FindCityByName(string _cityName)
    {
        string canonicalCityName = CityDisplayNameUtility.ToCanonicalName(_cityName);
        return FindObjectsByType<CityScript>(FindObjectsSortMode.None)
            .FirstOrDefault(c => c.cityData != null
                && string.Equals(
                    CityDisplayNameUtility.ToCanonicalName(c.cityData.cityName),
                    canonicalCityName,
                    System.StringComparison.OrdinalIgnoreCase));
    }

    private FactionManager FindFactionById(string _factionId)
    {
        if (string.IsNullOrWhiteSpace(_factionId))
            return null;

        return FindObjectsByType<FactionManager>(FindObjectsSortMode.None)
            .FirstOrDefault(f => f != null
                && string.Equals(f.GetSaveKey(), _factionId, System.StringComparison.OrdinalIgnoreCase));
    }

    private NationAIController FindNationAIController(FactionManager defender)
    {
        if (defender == null)
            return null;

        NationAIController[] controllers = FindObjectsByType<NationAIController>(FindObjectsSortMode.None);
        foreach (NationAIController controller in controllers)
        {
            if (controller == null)
                continue;

            FactionManager attachedFaction = controller.GetComponent<FactionManager>();
            if (attachedFaction == defender)
                return controller;

            FactionManager serializedFaction = GetPrivateField<FactionManager>(controller, "factionManager");
            if (serializedFaction == defender)
                return controller;
        }

        return null;
    }

    private bool IsWarWithoutShareAllowed()
    {
        DeveloperModeManager developerModeManager = FindFirstObjectByType<DeveloperModeManager>();
        return developerModeManager != null && developerModeManager.AllowWarWithoutShare;
    }

    private bool IsPlayerAsDefenderWarTestAllowed()
    {
        DeveloperModeManager developerModeManager = FindFirstObjectByType<DeveloperModeManager>();
        return developerModeManager != null
            && developerModeManager.AllowWarWithoutShare
            && developerModeManager.PlayerAsDefenderWarTest;
    }

    private static BigFivePersonality ClonePersonality(NationAIController controller)
    {
        if (controller == null)
            return null;

        BigFivePersonality source = GetPrivateField<BigFivePersonality>(controller, "personality");
        if (source == null)
            return null;

        return new BigFivePersonality
        {
            openness = source.openness,
            conscientiousness = source.conscientiousness,
            extraversion = source.extraversion,
            agreeableness = source.agreeableness,
            neuroticism = source.neuroticism,
        };
    }

    private static T GetPrivateField<T>(object target, string fieldName) where T : class
    {
        if (target == null || string.IsNullOrEmpty(fieldName))
            return null;

        System.Reflection.FieldInfo field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        return field?.GetValue(target) as T;
    }
}
