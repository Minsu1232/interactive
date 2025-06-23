using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ��� ���� ��ư ��Ʈ�ѷ� - �г� ��/���� ���
/// ���� ����ȭ�� ���� ���� ��ȯ ��� �г� Ȱ��ȭ/��Ȱ��ȭ ���
/// </summary>
public class LanguageButtonController : MonoBehaviour
{
    [Header("��ư ����")]
    [SerializeField] private Language buttonLanguage; // �� ��ư�� ����ϴ� ���

    [Header("UI ������Ʈ")]
    [SerializeField] private Button button;
    [SerializeField] private GameObject activePanel; // ���õ� ������ �׶���Ʈ �г�
    [SerializeField] private GameObject inactivePanel; // ���õ��� ���� ������ �г�
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("�ؽ�Ʈ ����")]
    [SerializeField] private Color activeTextColor = Color.white;
    [SerializeField] private Color inactiveTextColor = new Color(1f, 1f, 1f, 0.7f);

    [Header("�ִϸ��̼� ����")]
    [SerializeField] private bool enableClickAnimation = true;
    [SerializeField] private bool enableHoverEffect = true;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float clickScale = 0.95f;

    private bool isCurrentLanguage = false;
    private Vector3 originalScale;

    void Start()
    {
        // �ʱ� ����
        originalScale = transform.localScale;

        // ��ư Ŭ�� �̺�Ʈ ����
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }

        // ���ö���¡ �Ŵ��� �̺�Ʈ ����
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;

            // ���� ��� Ȯ���Ͽ� �ʱ� ���� ����
            UpdateButtonState(CSVLocalizationManager.Instance.currentLanguage);
        }
        else
        {
            // ���ö���¡ �Ŵ����� ���� ������ �⺻ ���(�ѱ���)�� �ʱ�ȭ
            UpdateButtonState(Language.Korean);
        }

        // ȣ�� ȿ�� ����
        if (enableHoverEffect)
        {
            SetupHoverEffects();
        }
    }

    void OnDestroy()
    {
        // �̺�Ʈ ���� ����
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    // ��ư Ŭ�� ó��
    void OnButtonClick()
    {
        if (CSVLocalizationManager.Instance != null)
        {
            CSVLocalizationManager.Instance.SetLanguage(buttonLanguage);
        }

        // Ŭ�� �ִϸ��̼� ȿ��
        if (enableClickAnimation)
        {
            StartCoroutine(ClickAnimation());
        }
    }

    // ��� ���� �̺�Ʈ ó��
    void OnLanguageChanged(Language newLanguage)
    {
        UpdateButtonState(newLanguage);
    }

    // ��ư ���� ������Ʈ (�г� ��/����)
    void UpdateButtonState(Language currentLanguage)
    {
        bool shouldBeActive = (currentLanguage == buttonLanguage);

        if (isCurrentLanguage == shouldBeActive) return; // ���°� �����ϸ� ����

        isCurrentLanguage = shouldBeActive;

        // �г� ���� ��ȯ
        if (shouldBeActive)
        {
            // Ȱ�� ����: �׶���Ʈ �г� �ѱ�
            if (activePanel != null) activePanel.SetActive(true);
            if (inactivePanel != null) inactivePanel.SetActive(false);

            // �ؽ�Ʈ ���� ����
            if (buttonText != null) buttonText.color = activeTextColor;
        }
        else
        {
            // ��Ȱ�� ����: �Ϲ� �г� �ѱ�
            if (activePanel != null) activePanel.SetActive(false);
            if (inactivePanel != null) inactivePanel.SetActive(true);

            // �ؽ�Ʈ ���� ����
            if (buttonText != null) buttonText.color = inactiveTextColor;
        }
    }

    // ȣ�� ȿ�� ����
    void SetupHoverEffects()
    {
        // EventTrigger�� ����Ͽ� ���콺 �̺�Ʈ ó��
        var eventTrigger = gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }

        // ���콺 ����
        var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
        pointerEnter.callback.AddListener((data) => { OnPointerEnter(); });
        eventTrigger.triggers.Add(pointerEnter);

        // ���콺 ������
        var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
        pointerExit.callback.AddListener((data) => { OnPointerExit(); });
        eventTrigger.triggers.Add(pointerExit);
    }

    // ���콺 ���� ��
    void OnPointerEnter()
    {
        if (!isCurrentLanguage) // ��Ȱ�� ��ư�� ȣ�� ȿ��
        {
            StartCoroutine(ScaleAnimation(hoverScale, 0.1f));
        }
    }

    // ���콺 ������ ��
    void OnPointerExit()
    {
        StartCoroutine(ScaleAnimation(1f, 0.1f));
    }

    // Ŭ�� �ִϸ��̼�
    System.Collections.IEnumerator ClickAnimation()
    {
        // ��¦ �پ����ٰ� �������
        yield return StartCoroutine(ScaleAnimation(clickScale, 0.05f));
        yield return StartCoroutine(ScaleAnimation(1f, 0.1f));
    }

    // ������ �ִϸ��̼�
    System.Collections.IEnumerator ScaleAnimation(float targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = originalScale * targetScale;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        transform.localScale = endScale;
    }

    // ������ ���� ���� (����׿�)
    [ContextMenu("Ȱ�� ���·� ����")]
    public void SetActiveState()
    {
        UpdateButtonState(buttonLanguage);
    }

    [ContextMenu("��Ȱ�� ���·� ����")]
    public void SetInactiveState()
    {
        Language otherLanguage = (buttonLanguage == Language.Korean) ? Language.English : Language.Korean;
        UpdateButtonState(otherLanguage);
    }

    // ���� ���� Ȯ��
    [ContextMenu("���� ���� Ȯ��")]
    void PrintCurrentState()
    {
        Debug.Log($"��ư ���: {buttonLanguage}");
        Debug.Log($"���� Ȱ�� ����: {isCurrentLanguage}");
        Debug.Log($"Ȱ�� �г� ����: {(activePanel != null ? activePanel.activeSelf : "null")}");
        Debug.Log($"��Ȱ�� �г� ����: {(inactivePanel != null ? inactivePanel.activeSelf : "null")}");
    }
}