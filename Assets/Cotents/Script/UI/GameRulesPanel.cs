using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// ���� �� �г��� �����ϴ� ��ũ��Ʈ
/// ���ö���¡ ���� �� �ִϸ��̼� ȿ�� ����
/// </summary>
public class GameRulesPanel : MonoBehaviour
{
    [Header("�г� UI ������Ʈ")]
    [SerializeField] private GameObject rulesPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("�ؽ�Ʈ ������Ʈ��")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI mainDescText;
    [SerializeField] private TextMeshProUGUI subDescText;
    [SerializeField] private TextMeshProUGUI bonusTitleText;
    [SerializeField] private TextMeshProUGUI gradeTitleText;
    [SerializeField] private TextMeshProUGUI tipText;

    [Header("��� �ؽ�Ʈ��")]
    [SerializeField] private TextMeshProUGUI geniusText;
    [SerializeField] private TextMeshProUGUI expertText;
    [SerializeField] private TextMeshProUGUI normalText;
    [SerializeField] private TextMeshProUGUI retryText;

    [Header("��ư")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI startButtonText;

    [Header("���ʽ� ī���")]
    [SerializeField] private TextMeshProUGUI[] bonusCardTexts = new TextMeshProUGUI[5];

    [Header("�ִϸ��̼� ����")]
    [SerializeField] private float fadeInDuration = 0.6f;
    [SerializeField] private float slideDistance = 30f;

    [Header("�̺�Ʈ")]
    public Action OnGameStart;
    public Action OnPanelClosed;

    private bool isVisible = false;
    private RectTransform panelRect;

    void Awake()
    {
        panelRect = rulesPanel.GetComponent<RectTransform>();

        // ��ư �̺�Ʈ ����
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        // �ʱ� ���� ����
        if (rulesPanel != null)
        {
            rulesPanel.SetActive(false);
        }
    }

    void Start()
    {
        // ���ö���¡ �ؽ�Ʈ ����
        SetupLocalizedTexts();

        // ��� ���� �̺�Ʈ ����
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }

        // ���ʽ� ī�� �ؽ�Ʈ ����
        SetupBonusCards();
    }

    void OnDestroy()
    {
        // �̺�Ʈ ���� ����
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    /// <summary>
    /// ��� �ؽ�Ʈ�� LocalizedText ������Ʈ ����
    /// </summary>
    void SetupLocalizedTexts()
    {
        // �� �ؽ�Ʈ�� LocalizedText �߰� �� Ű ����
        AddLocalizedText(titleText, "rules_title");
        AddLocalizedText(mainDescText, "rules_main_desc");
        AddLocalizedText(subDescText, "rules_sub_desc");
        AddLocalizedText(bonusTitleText, "rules_bonus_title");
        AddLocalizedText(gradeTitleText, "rules_grade_title");
        AddLocalizedText(tipText, "rules_tip_text");

        // ��� �ؽ�Ʈ��
        AddLocalizedText(geniusText, "rules_grade_genius");
        AddLocalizedText(expertText, "rules_grade_expert");
        AddLocalizedText(normalText, "rules_grade_normal");
        AddLocalizedText(retryText, "rules_grade_retry");

        // ��ư �ؽ�Ʈ
        AddLocalizedText(startButtonText, "rules_start_button");
    }

    /// <summary>
    /// ���ʽ� ī��� �ؽ�Ʈ ���� (GameManager�� ���ʽ��� ���)
    /// </summary>
    void SetupBonusCards()
    {
        if (bonusCardTexts.Length < 5) return;

        // GameManager���� ���� ���ʽ��� ��������
        float bonus5 = GameManager.Instance?.GetDiversificationBonusRate(5) ?? 20f;
        float bonus4 = GameManager.Instance?.GetDiversificationBonusRate(4) ?? 15f;
        float bonus3 = GameManager.Instance?.GetDiversificationBonusRate(3) ?? 10f;
        float bonus2 = GameManager.Instance?.GetDiversificationBonusRate(2) ?? 5f;
        float bonus1 = GameManager.Instance?.GetDiversificationBonusRate(1) ?? -10f;

        // �� ���� �ؽ�Ʈ ����
        Language currentLang = CSVLocalizationManager.Instance?.currentLanguage ?? Language.Korean;

        if (currentLang == Language.Korean)
        {
            bonusCardTexts[0].text = $"5�о�\n{bonus5:+0;-0}%";
            bonusCardTexts[1].text = $"4�о�\n{bonus4:+0;-0}%";
            bonusCardTexts[2].text = $"3�о�\n{bonus3:+0;-0}%";
            bonusCardTexts[3].text = $"2�о�\n{bonus2:+0;-0}%";
            bonusCardTexts[4].text = $"1�о�\n{bonus1:+0;-0}%";
        }
        else
        {
            bonusCardTexts[0].text = $"5 Sectors\n{bonus5:+0;-0}%";
            bonusCardTexts[1].text = $"4 Sectors\n{bonus4:+0;-0}%";
            bonusCardTexts[2].text = $"3 Sectors\n{bonus3:+0;-0}%";
            bonusCardTexts[3].text = $"2 Sectors\n{bonus2:+0;-0}%";
            bonusCardTexts[4].text = $"1 Sector\n{bonus1:+0;-0}%";
        }
    }

    /// <summary>
    /// �ؽ�Ʈ�� LocalizedText ������Ʈ �߰�
    /// </summary>
    void AddLocalizedText(TextMeshProUGUI textComponent, string key)
    {
        if (textComponent == null) return;

        LocalizedText localizedText = textComponent.GetComponent<LocalizedText>();
        if (localizedText == null)
        {
            localizedText = textComponent.gameObject.AddComponent<LocalizedText>();
        }

        localizedText.localizationKey = key;
        localizedText.UpdateText();
    }

    /// <summary>
    /// ��� ���� �� ȣ��
    /// </summary>
    void OnLanguageChanged(Language newLanguage)
    {
        // ���ʽ� ī�� �ؽ�Ʈ ������Ʈ
        SetupBonusCards();
    }

    /// <summary>
    /// �� �г� ǥ��
    /// </summary>
    public void ShowPanel()
    {
        if (isVisible) return;

        rulesPanel.SetActive(true);
        isVisible = true;

        // ���̵� �� �ִϸ��̼�
        StartCoroutine(FadeInAnimation());
    }

    /// <summary>
    /// �� �г� �����
    /// </summary>
    public void HidePanel()
    {
        if (!isVisible) return;

        // ���̵� �ƿ� �ִϸ��̼�
        StartCoroutine(FadeOutAnimation());
    }

    /// <summary>
    /// ���̵� �� �ִϸ��̼�
    /// </summary>
    System.Collections.IEnumerator FadeInAnimation()
    {
        // �ʱ� ���� ����
        panelCanvasGroup.alpha = 0f;
        panelRect.anchoredPosition = new Vector2(0, -slideDistance);

        float elapsedTime = 0f;
        Vector2 startPos = panelRect.anchoredPosition;
        Vector2 targetPos = Vector2.zero;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeInDuration;

            // Ease out �
            progress = 1f - Mathf.Pow(1f - progress, 3f);

            // ���� �� ��ġ ������Ʈ
            panelCanvasGroup.alpha = progress;
            panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, progress);

            yield return null;
        }

        // ���� ���� ����
        panelCanvasGroup.alpha = 1f;
        panelRect.anchoredPosition = targetPos;
    }

    /// <summary>
    /// ���̵� �ƿ� �ִϸ��̼�
    /// </summary>
    System.Collections.IEnumerator FadeOutAnimation()
    {
        float elapsedTime = 0f;
        Vector2 startPos = panelRect.anchoredPosition;
        Vector2 targetPos = new Vector2(0, slideDistance);

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeInDuration;

            // Ease in �
            progress = Mathf.Pow(progress, 3f);

            // ���� �� ��ġ ������Ʈ
            panelCanvasGroup.alpha = 1f - progress;
            panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, progress);

            yield return null;
        }

        // ���� ���� ����
        panelCanvasGroup.alpha = 0f;
        panelRect.anchoredPosition = targetPos;
        rulesPanel.SetActive(false);
        isVisible = false;

        // �г� ���� �̺�Ʈ �߻�
        OnPanelClosed?.Invoke();
    }

    /// <summary>
    /// ���� ��ư Ŭ�� �� ȣ��
    /// </summary>
    void OnStartButtonClicked()
    {
        // ���� ���� �̺�Ʈ �߻�
        OnGameStart?.Invoke();

        // �г� �����
        HidePanel();
    }

    /// <summary>
    /// �ݱ� ��ư Ŭ�� �� ȣ��
    /// </summary>
    void OnCloseButtonClicked()
    {
        HidePanel();
    }

    /// <summary>
    /// �ܺο��� �г� ���
    /// </summary>
    public void TogglePanel()
    {
        if (isVisible)
        {
            HidePanel();
        }
        else
        {
            ShowPanel();
        }
    }

    /// <summary>
    /// �г��� ǥ�� ������ Ȯ��
    /// </summary>
    public bool IsVisible => isVisible;
}