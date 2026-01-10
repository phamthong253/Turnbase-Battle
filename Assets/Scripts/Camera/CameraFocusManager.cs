using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;

public class CameraFocusManager : MonoBehaviour
{
    public static CameraFocusManager Instance;

    [Header("Cameras")]
    public CinemachineCamera overviewCamera;
    public CinemachineCamera actionCamera;

    [Header("Main Camera Ref")]
    public Camera mainUnityCamera;

    [Header("Helper")]
    public CinemachineTargetGroup targetGroup;
    public UnityEngine.Rendering.Universal.Light2D cinematicLight;
    public UnityEngine.Rendering.Universal.Light2D mainWorldLight;

    [Tooltip("Kéo SpeedLinesContainer vào đây")]
    public GameObject speedLineEffect;
    public SpriteRenderer blackBackDrop;

    [Header("Settings")]
    [SerializeField] public float blendInTime = 1.5f;
    public string focusLayerName = "SkillFocus";

    [Tooltip("Kéo cái ColorOverlay (Sprite con của Camera) vào đây")]
    public SpriteRenderer colorOverlay;

    [Tooltip("Màu nền mặc định (Đen nhạt/Xám đậm). Alpha nên để là 1.")]
    public Color defaultBackdropColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    [Header("Panel Skill Name Settings")]
    public GameObject skillNamePanel;
    public TextMeshProUGUI skillNameText;
    public float textSlideDuration = 0.5f;
    public float startXOffset = -1500f; // Vị trí bắt đầu (bên trái màn hình)
    public float endXPos = 0f;


    // ... (Các biến nội bộ) ...
    private UnitController currentCaster;
    private List<UnitController> currentTargets = new List<UnitController>();
    private int focusLayerID;
    private Dictionary<Transform, int> originalObjectLayers = new Dictionary<Transform, int>();
    private int originalCullingMask;
    private CameraClearFlags originalClearFlags;
    private Color originalBackgroundColor;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (mainUnityCamera == null) mainUnityCamera = Camera.main;
        if (overviewCamera != null) overviewCamera.Priority = 10;
        if (actionCamera != null) actionCamera.Priority = 0;

        focusLayerID = LayerMask.NameToLayer(focusLayerName);

        // Setup trạng thái ban đầu
        if (blackBackDrop != null)
        {
            blackBackDrop.gameObject.SetActive(false);
            Color c = defaultBackdropColor;
            c.a = 0;
            blackBackDrop.color = c;
        }

        if (colorOverlay != null)
        {
            colorOverlay.gameObject.SetActive(false);
            Color c = colorOverlay.color;
            c.a = 0;
            colorOverlay.color = c;
        }
        if (skillNamePanel != null)
        {
            skillNamePanel.SetActive(false);
            // Đảm bảo có CanvasGroup để xử lý fade nếu cần
            if (skillNamePanel.GetComponent<CanvasGroup>() == null)
                skillNamePanel.AddComponent<CanvasGroup>();
        }
    }

    public IEnumerator FocusOnSkillRoutine(UnitController caster, List<UnitController> targets)
    {
        if (actionCamera == null || caster == null || mainUnityCamera == null || targetGroup == null)
            yield break;

        currentCaster = caster;
        currentTargets.Clear();
        if (targets != null) currentTargets.AddRange(targets);

        // --- 1. SETUP TARGET GROUP ---
        List<CinemachineTargetGroup.Target> groupMembers = new List<CinemachineTargetGroup.Target>();
        groupMembers.Add(new CinemachineTargetGroup.Target { Object = caster.transform, Weight = 1.5f, Radius = 1f });
        foreach (var t in currentTargets)
        {
            if (t != null) groupMembers.Add(new CinemachineTargetGroup.Target { Object = t.transform, Weight = 1f, Radius = 1f });
        }
        targetGroup.Targets = groupMembers;

        // --- 2. CHUYỂN LAYER VÀ ÁP DỤNG THEME ---
        originalObjectLayers.Clear();
        ChangeLayerRecursive(caster.transform, focusLayerID);
        foreach (var t in currentTargets)
        {
            if (t != null) ChangeLayerRecursive(t.transform, focusLayerID);
        }

        ApplyUnitTheme(caster); // Hàm này set màu sắc

        // --- 3. SETUP MAIN CAMERA ---
        originalCullingMask = mainUnityCamera.cullingMask;
        originalClearFlags = mainUnityCamera.clearFlags;
        originalBackgroundColor = mainUnityCamera.backgroundColor;
        mainUnityCamera.cullingMask = 1 << focusLayerID;
        mainUnityCamera.clearFlags = CameraClearFlags.SolidColor;
        mainUnityCamera.backgroundColor = blackBackDrop.color;

        // --- 4. BẬT ĐÈN & CAMERA ---
        if (cinematicLight != null)
        {
            cinematicLight.gameObject.SetActive(true);
            cinematicLight.gameObject.layer = focusLayerID;
        }
        if (mainWorldLight != null) mainWorldLight.gameObject.SetActive(false);

        // --- 5. HIỆU ỨNG VÀO (FADE IN) ---
        // Backdrop
        if (blackBackDrop != null)
        {
            blackBackDrop.gameObject.SetActive(true);
            blackBackDrop.DOFade(1f, blendInTime).From(0f).SetEase(Ease.OutQuad);
        }

        // SpeedLine
        if (speedLineEffect != null)
        {
            speedLineEffect.SetActive(true);
            ChangeLayerRecursive(speedLineEffect.transform, focusLayerID);
            speedLineEffect.transform.localScale = Vector3.one;
            speedLineEffect.transform.DOScaleY(1f, blendInTime).From(0f).SetEase(Ease.OutBack);
        }

        // Color Overlay (Glare)
        if (colorOverlay != null)
        {
            colorOverlay.gameObject.SetActive(true);
            ChangeLayerRecursive(colorOverlay.transform, focusLayerID);
            // Đảm bảo Alpha bắt đầu từ 0
            colorOverlay.DOFade(1f, blendInTime).From(0f).SetEase(Ease.Linear);
        }
        if (skillNamePanel != null && caster.skillSO != null)
        {
            skillNamePanel.SetActive(true);

            // 1. Cập nhật nội dung text
            if (skillNameText != null)
                skillNameText.text = caster.skillSO.skillName;

            // 2. Setup vị trí ban đầu (Bay từ bên trái sang)
            RectTransform rect = skillNamePanel.GetComponent<RectTransform>();
            CanvasGroup cg = skillNamePanel.GetComponent<CanvasGroup>();

            // Đặt vị trí X ra ngoài màn hình bên trái
            rect.anchoredPosition = new Vector2(startXOffset, rect.anchoredPosition.y);
            cg.alpha = 0; // Bắt đầu tàng hình

            // 3. Thực hiện Animation
            Sequence textSeq = DOTween.Sequence();

            // Fade In nhanh
            textSeq.Join(cg.DOFade(1f, 0.2f));
            // Bay vào vị trí đích (Sượt ngang) - Dùng Ease OutBack để có lực quán tính
            textSeq.Join(rect.DOAnchorPosX(endXPos, textSlideDuration).SetEase(Ease.OutBack));
        }

        actionCamera.Follow = targetGroup.transform;
        actionCamera.LookAt = null;
        actionCamera.Priority = 100;

        yield return new WaitForSeconds(blendInTime);
    }

    public void ResetFocusDelayed(float delay) { StartCoroutine(ResetFocusRoutine(delay)); }
    private IEnumerator ResetFocusRoutine(float delay) { yield return new WaitForSeconds(delay); ResetFocus(); }

    // --- HÀM NÀY ĐƯỢC VIẾT LẠI ---
    public void ResetFocus()
    {
        if (actionCamera == null) return;

        // Trả Camera về vị trí cũ (Game play)
        actionCamera.Priority = 0;
        actionCamera.Follow = null;

        // --- DÙNG SEQUENCE ĐỂ ĐỒNG BỘ HIỆU ỨNG TẮT ---
        Sequence endSeq = DOTween.Sequence();

        // 1. Thêm các hiệu ứng Fade Out vào Sequence (Dùng Join để chạy cùng lúc)
        if (colorOverlay != null)
            endSeq.Join(colorOverlay.material.DOFloat(0f,"_GlobalOpacity", 0.1f));

        if (blackBackDrop != null)
            endSeq.Join(blackBackDrop.DOFade(0f, 0.3f));

        if (speedLineEffect != null)
            endSeq.Join(speedLineEffect.transform.DOScaleY(0f, 0.3f));
        if (skillNamePanel != null)
        {
            CanvasGroup cg = skillNamePanel.GetComponent<CanvasGroup>();
            RectTransform rect = skillNamePanel.GetComponent<RectTransform>();

            // Cách 1: Bay tiếp sang phải rồi biến mất (Sượt qua luôn)
            // endSeq.Join(rect.DOAnchorPosX(Mathf.Abs(startXOffset), 0.3f).SetEase(Ease.InQuad));

            // Cách 2: Mờ dần tại chỗ (Gọn gàng hơn) -> Tôi chọn cách này
            endSeq.Join(cg.DOFade(0f, 0.3f));
        }


        // 2. Khi TẤT CẢ hiệu ứng trên chạy xong
        endSeq.OnComplete(() =>
        {
            // Tắt GameObject
            if (colorOverlay != null) colorOverlay.gameObject.SetActive(false);
            if (blackBackDrop != null) blackBackDrop.gameObject.SetActive(false);
            if (speedLineEffect != null) speedLineEffect.SetActive(false);
            if (cinematicLight != null) cinematicLight.gameObject.SetActive(false);
            if (skillNamePanel != null) skillNamePanel.SetActive(false);


            // QUAN TRỌNG: Trả lại Culling Mask cho Camera SAU KHI Fade xong
            // Nếu trả trước, hiệu ứng biến mất ngay lập tức (pop) chứ ko mờ đi


            // Trả lại Layer cho Unit sau cùng

            if (currentCaster != null) RestoreLayersRecursive(currentCaster.transform);
            foreach (var t in currentTargets) { if (t != null) RestoreLayersRecursive(t.transform); }

            currentCaster = null;
            currentTargets.Clear();
        });
        if (mainUnityCamera != null)
        {
            mainUnityCamera.cullingMask = originalCullingMask;
            // Trả lại Skybox hoặc cài đặt cũ nếu cần
            mainUnityCamera.clearFlags = originalClearFlags;
            mainUnityCamera.backgroundColor = originalBackgroundColor;
        }
    }

    // --- HÀM APPLY THEME ---
    private void ApplyUnitTheme(UnitController caster)
    {
        if (caster == null || caster.unitSO == null) return;

        // 1. Backdrop dùng màu mặc định xám đậm
        if (blackBackDrop != null)
        {
            Color themeColor = defaultBackdropColor;
            themeColor.a = 0;
            blackBackDrop.color = themeColor;
        }

        // 2. SpeedLine dùng màu SO
        if (speedLineEffect != null)
        {
            Color lineColor = caster.unitSO.cinematicSpeedLineColor;
            ParticleSystem[] particles = speedLineEffect.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                var main = ps.main;
                main.startColor = lineColor;
            }
        }

        // 3. Glare Overlay
        if (colorOverlay != null)
        {
            Material mat = colorOverlay.material;
            Color mainColor = caster.unitSO.cinematicBackdropColor;

            // Logic màu dựa trên Shader Graph (Lerp 3 màu)
            Color topCol = mainColor * 1.2f;    // Trên sáng
            Color bottomCol = mainColor;        // Dưới bình thường

            // Màu giữa nên tint nhẹ theo màu gốc hoặc để trắng mờ
            // Nếu Shader đã xử lý Alpha Mask ở giữa thì màu này ít quan trọng hơn, 
            // nhưng nên để cùng tone để phần chuyển tiếp mượt.
            Color middleCol = mainColor;

            mat.SetColor("_MiddleColor", middleCol);
            mat.SetColor("_TopColor", topCol);
            mat.SetColor("_BottomColor", bottomCol);
            mat.SetFloat("_GlobalOpacity", 0.55f);

            Color c = colorOverlay.color;
            c.a = 0;
            colorOverlay.color = c;
        }
    }

    public void ForceLayerToFocus(GameObject obj)
    {
        if (actionCamera != null && actionCamera.Priority > 0)
        {
            ChangeLayerRecursive(obj.transform, focusLayerID);
        }
    }

    void ChangeLayerRecursive(Transform trans, int newLayer)
    {
        if (!originalObjectLayers.ContainsKey(trans)) originalObjectLayers.Add(trans, trans.gameObject.layer);
        trans.gameObject.layer = newLayer;
        foreach (Transform child in trans) ChangeLayerRecursive(child, newLayer);
    }

    void RestoreLayersRecursive(Transform trans)
    {
        if (originalObjectLayers.ContainsKey(trans)) trans.gameObject.layer = originalObjectLayers[trans];
        foreach (Transform child in trans) RestoreLayersRecursive(child);
    }
}