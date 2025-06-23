using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FadeOutIn : MonoBehaviour
{
    //�� ��ȯ�� ��Ʈ���� �̿��� ���̵� �ƿ�/�� ȿ��    

    [Header("���̵� �ƿ�/�� ����")]
    public float fadeDuration = 1.0f; // ���̵� �ƿ�/�� �ð�
    public float fadeDelay = 0.5f; // ���̵� �ƿ� �� ������ �ð�
    public float fadeAlpha = 1.0f; // ���̵� �ƿ�/�� ���� ��

    [Header("�̹���")]
    [SerializeField] Image fadeImage; // ���̵� ȿ���� ������ �̹���

    [Header("���̸�")]
    [SerializeField] string sceneName = "GameScene"; // ��ȯ�� �� �̸�

    [Header("��������")]
    public bool isSceneTransition = false; // �� ��ȯ ����



    void Start()
    {
        fadeImage.DOFade(fadeAlpha, fadeDuration).SetDelay(fadeDelay).OnComplete(() =>
        {
            if (isSceneTransition)
            {
                SceneManager.LoadScene(sceneName); // �� ��ȯ
            }

            Debug.Log("���̵� �ƿ� �Ϸ�");
        });
    }

    // Update is called once per frame
    void Update()
    {

    }
}
