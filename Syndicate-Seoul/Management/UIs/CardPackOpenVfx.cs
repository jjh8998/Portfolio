using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드팩 개봉 연출. 흔들림 → 최고등급 색 번쩍임 → 디지털 디졸브(팩 분해) 순서로
/// 재생한 뒤 결과 카드 그리드를 연다.
/// 디졸브는 기존 'UI/Card Artwork Dissolve' 셰이더의 _DissolveAmount 를
/// 코루틴이 0(보임)→1(사라짐) 으로 구동해 구현한다.
/// </summary>
public class CardPackOpenVfx : MonoBehaviour
{
    private const string DissolveShaderName = "UI/Card Artwork Dissolve";

    [Header("Refs")]
    [Tooltip("연출 동안만 켜지는 전체 화면 컨테이너")]
    [SerializeField] private GameObject vfxRoot;
    [Tooltip("개봉 연출에 표시되는 팩 이미지")]
    [SerializeField] private Image packImage;
    [Tooltip("팩 주변 번쩍임(가산 글로우, 평소 alpha 0)")]
    [SerializeField] private Image flashImage;
    [Tooltip("RenderTexture로 렌더되는 파티클 버스트 (없으면 생략)")]
    [SerializeField] private ParticleSystem burstParticles;
    [Tooltip("파티클 RT를 가산으로 표시하는 RawImage 오브젝트 (없으면 생략)")]
    [SerializeField] private GameObject particleRtObject;
    [Tooltip("파티클 색을 HDR로 끌어올려 Bloom을 유발하는 배수")]
    [SerializeField, Range(1f, 5f)] private float particleColorBoost = 2.4f;

    [Header("Timing (초)")]
    [SerializeField] private float shakeDuration = 0.52f;
    [SerializeField] private float flashDuration = 0.56f;
    [SerializeField] private float dissolveDuration = 0.82f;

    [Header("Shake")]
    [SerializeField] private float shakeStrength = 14f;
    [SerializeField] private float shakeScaleBoost = 0.04f;
    [SerializeField] private AnimationCurve shakeDecay = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Flash")]
    [Tooltip("0→1→0 산 모양 권장 (빠르게 밝아지고 천천히 사라짐)")]
    [SerializeField] private AnimationCurve flashCurve =
        new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f));
    [Tooltip("번쩍임 시작 스케일 (작게 시작)")]
    [SerializeField, Range(0f, 2f)] private float flashScaleStart = 0.65f;
    [Tooltip("번쩍임 끝 스케일 (팽창하며 터짐)")]
    [SerializeField, Range(0f, 3f)] private float flashScaleEnd = 1.45f;
    [Tooltip("절정에서 흰빛이 섞이는 정도 (강렬한 코어)")]
    [SerializeField, Range(0f, 1f)] private float flashWhiteHot = 0.5f;

    [Header("Dissolve")]
    [SerializeField] private AnimationCurve dissolveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Range(0f, 0.2f)] private float dissolveEdgeWidth = 0.05f;
    [SerializeField] private float dissolveNoiseScale = 18f;
    [SerializeField, Range(0f, 1f)] private float dissolveDirectionBias = 0.4f;
    [Tooltip("디졸브 중 팩이 위로 솟는 거리(px)")]
    [SerializeField] private float dissolveRise = 36f;
    [Tooltip("디졸브 중 팩이 작아지는 비율")]
    [SerializeField, Range(0f, 0.6f)] private float dissolveShrink = 0.18f;

    private static Shader dissolveShader;
    private Coroutine playRoutine;

    private Vector2 packBasePosition;
    private Vector3 packBaseScale = Vector3.one;
    private Vector3 flashBaseScale = Vector3.one;
    private bool packBaseCaptured;

    /// <summary>
    /// 개봉 연출을 시작한다. 오버레이는 즉시 켜서 뒤 패널을 가리고,
    /// 연출이 끝나면 결과 그리드를 연다.
    /// </summary>
    public void Play(CardPackData packData, List<string> gainedCardIds, ShowCardListPanel grid)
    {
        if (playRoutine != null)
            StopCoroutine(playRoutine);

        CapturePackBaseTransform();
        SetupPackSprite(packData);
        SetRootActive(true);
        playRoutine = StartCoroutine(PlayThenShow(gainedCardIds, grid));
    }

    private IEnumerator PlayThenShow(List<string> gainedCardIds, ShowCardListPanel grid)
    {
        Color topColor = ResolveTopTierColor(gainedCardIds);

        RestorePackBaseTransform();
        ClearFlash();

        yield return Shake();

        TriggerParticles(topColor);
        yield return Flash(topColor);
        yield return Dissolve(topColor);
        StopParticles();

        SetRootActive(false);
        RestorePackBaseTransform();
        ClearFlash();

        if (grid != null)
            grid.Open(gainedCardIds);
        else
            Debug.LogWarning("[CardPackOpenVfx] ShowCardListPanel is null. 결과 그리드를 열 수 없습니다.");

        playRoutine = null;
    }

    /// <summary>뽑힌 카드 중 최고 tier 의 색을 반환 (기존 CardTierColors 재사용).</summary>
    private Color ResolveTopTierColor(List<string> gainedCardIds)
    {
        int maxTier = 1;

        if (gainedCardIds != null)
        {
            CardDatabase database = CardDatabase.Instance;
            for (int i = 0; i < gainedCardIds.Count; i++)
            {
                CardData card = database != null ? database.GetById(gainedCardIds[i]) : null;
                if (card != null)
                    maxTier = Mathf.Max(maxTier, card.tier);
            }
        }

        return CardTierColors.GetNameColor(maxTier);
    }

    private IEnumerator Shake()
    {
        RectTransform rect = packImage != null ? packImage.rectTransform : null;
        if (rect == null || shakeDuration <= 0f)
            yield break;

        Vector2 basePosition = rect.anchoredPosition;
        Vector3 baseScale = rect.localScale;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shakeDuration);
            float decay = shakeDecay.Evaluate(t);

            Vector2 offset = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * shakeStrength * decay;
            rect.anchoredPosition = basePosition + offset;
            rect.localScale = baseScale * (1f + shakeScaleBoost * Mathf.SmoothStep(0f, 1f, t));

            yield return null;
        }

        rect.anchoredPosition = basePosition;
    }

    private IEnumerator Flash(Color color)
    {
        if (flashImage == null || flashDuration <= 0f)
            yield break;

        flashImage.enabled = true;
        RectTransform rect = flashImage.rectTransform;
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flashDuration);
            float a = Mathf.Clamp01(flashCurve.Evaluate(t));

            // 절정에서 흰빛이 섞여 강렬한 코어를 만든다
            Color c = Color.Lerp(color, Color.white, flashWhiteHot * a);
            c.a = a;
            flashImage.color = c;

            // 빛이 팽창하며 터지는 느낌
            float s = Mathf.Lerp(flashScaleStart, flashScaleEnd, Mathf.SmoothStep(0f, 1f, t));
            rect.localScale = flashBaseScale * s;

            yield return null;
        }

        Color cleared = color;
        cleared.a = 0f;
        flashImage.color = cleared;
        rect.localScale = flashBaseScale;
    }

    private IEnumerator Dissolve(Color edgeColor)
    {
        RectTransform rect = packImage != null ? packImage.rectTransform : null;
        if (rect == null || dissolveDuration <= 0f)
            yield break;

        Material dissolveMaterial = CreateDissolveMaterial(edgeColor);
        Material originalMaterial = packImage.material;
        bool useShader = dissolveMaterial != null;
        if (useShader)
            packImage.material = dissolveMaterial;

        Vector2 basePosition = rect.anchoredPosition;
        Vector3 baseScale = rect.localScale;
        Color baseColor = packImage.color;
        float elapsed = 0f;

        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dissolveDuration);
            float d = Mathf.Clamp01(dissolveCurve.Evaluate(t));

            if (useShader)
                dissolveMaterial.SetFloat("_DissolveAmount", d);
            else
                packImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * (1f - d)); // 셰이더 미지원 폴백

            rect.anchoredPosition = basePosition + new Vector2(0f, dissolveRise * d);
            rect.localScale = baseScale * (1f - dissolveShrink * d);

            yield return null;
        }

        if (useShader)
        {
            packImage.material = originalMaterial;
            Destroy(dissolveMaterial);
        }

        rect.anchoredPosition = basePosition;
        rect.localScale = baseScale;
        packImage.color = baseColor;
    }

    private Material CreateDissolveMaterial(Color edgeColor)
    {
        if (dissolveShader == null)
            dissolveShader = Shader.Find(DissolveShaderName);

        if (dissolveShader == null || !dissolveShader.isSupported)
        {
            Debug.LogWarning($"[CardPackOpenVfx] '{DissolveShaderName}' 셰이더를 찾을 수 없습니다. 알파 페이드로 폴백합니다.");
            return null;
        }

        Material material = new Material(dissolveShader) { name = "CardPackDissolve_Runtime" };
        material.SetFloat("_DissolveAmount", 0f);
        material.SetFloat("_EdgeWidth", dissolveEdgeWidth);
        material.SetColor("_EdgeColor", edgeColor);
        material.SetFloat("_NoiseScale", Mathf.Max(0.01f, dissolveNoiseScale));
        material.SetFloat("_RightToLeftBias", dissolveDirectionBias);
        return material;
    }

    private void SetupPackSprite(CardPackData packData)
    {
        if (packImage == null)
            return;

        Sprite sprite = packData != null ? packData.GetCardPackSprite() : null;
        if (sprite != null)
        {
            packImage.sprite = sprite;
            packImage.preserveAspect = true;
        }

        packImage.enabled = true;
    }

    private void CapturePackBaseTransform()
    {
        if (packBaseCaptured || packImage == null)
            return;

        RectTransform rect = packImage.rectTransform;
        packBasePosition = rect.anchoredPosition;
        packBaseScale = rect.localScale;

        if (flashImage != null)
            flashBaseScale = flashImage.rectTransform.localScale;

        packBaseCaptured = true;
    }

    private void RestorePackBaseTransform()
    {
        if (!packBaseCaptured || packImage == null)
            return;

        RectTransform rect = packImage.rectTransform;
        rect.anchoredPosition = packBasePosition;
        rect.localScale = packBaseScale;

        if (flashImage != null)
            flashImage.rectTransform.localScale = flashBaseScale;

        Color c = packImage.color;
        c.a = 1f;
        packImage.color = c;
    }

    private void ClearFlash()
    {
        if (flashImage == null)
            return;

        Color c = flashImage.color;
        c.a = 0f;
        flashImage.color = c;
    }

    private void TriggerParticles(Color color)
    {
        if (particleRtObject != null)
            particleRtObject.SetActive(true);

        if (burstParticles == null)
            return;

        ParticleSystem.MainModule main = burstParticles.main;
        main.startColor = new Color(color.r * particleColorBoost, color.g * particleColorBoost, color.b * particleColorBoost, 1f);

        burstParticles.Clear(true);
        burstParticles.Play(true);
    }

    private void StopParticles()
    {
        if (burstParticles != null)
            burstParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (particleRtObject != null)
            particleRtObject.SetActive(false);
    }

    private void SetRootActive(bool active)
    {
        if (vfxRoot != null)
            vfxRoot.SetActive(active);
        else
            Debug.LogWarning("[CardPackOpenVfx] vfxRoot 가 비어 있습니다.");
    }
}
