using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using UnityEngine.SceneManagement;

public class MoneyRainEffect : MonoBehaviour
{
    [Header("설정")]
    public RectTransform canvasRect;  // 캔버스
    public GameObject textPrefab;     // 텍스트 프리팹 (또는 동적 생성)
    public string gameSceneName; // 게임 씬 이름
   public void OnGameStart()
    {
        StartCoroutine(MoneyRainAndLoadScene());
    }

    IEnumerator MoneyRainAndLoadScene()
    {
        // 돈 이모지들 우수수 떨어뜨리기
        for (int i = 0; i < 20; i++)
        {
            CreateFallingMoney();
            yield return new WaitForSeconds(0.1f); // 0.1초 간격으로 생성
        }

        yield return new WaitForSeconds(2f); // 떨어지는 걸 잠깐 구경

        // 씬 전환
        SceneManager.LoadScene(gameSceneName);
    }

    void CreateFallingMoney()
    {
        // 텍스트 오브젝트 생성
        GameObject moneyText = new GameObject("MoneyText");
        moneyText.transform.SetParent(canvasRect, false);

        TextMeshProUGUI text = moneyText.AddComponent<TextMeshProUGUI>();
        text.text = "💰💵💸🤑💴💶💷"; // 돈 이모지들
        text.fontSize = Random.Range(40f, 80f); // 크기 랜덤
        text.color = Color.yellow;

        RectTransform rect = text.rectTransform;

        // 시작 위치 (화면 위쪽 랜덤)
        rect.anchoredPosition = new Vector2(
            Random.Range(-Screen.width / 2, Screen.width / 2),
            Screen.height / 2 + 100
        );

        // 떨어지는 애니메이션
        rect.DOAnchorPosY(-Screen.height / 2 - 100, Random.Range(1.5f, 3f))
            .SetEase(Ease.InQuad)
            .OnComplete(() => Destroy(moneyText));

        // 살짝 회전도 추가
        rect.DORotate(new Vector3(0, 0, Random.Range(-360f, 360f)), Random.Range(2f, 4f), RotateMode.FastBeyond360);
    }
}