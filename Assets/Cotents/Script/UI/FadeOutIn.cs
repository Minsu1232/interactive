using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FadeOutIn : MonoBehaviour
{
    //씬 전환시 두트윈을 이용한 페이드 아웃/인 효과    

    [Header("페이드 아웃/인 설정")]
    public float fadeDuration = 1.0f; // 페이드 아웃/인 시간
    public float fadeDelay = 0.5f; // 페이드 아웃 후 딜레이 시간
    public float fadeAlpha = 1.0f; // 페이드 아웃/인 알파 값

    [Header("이미지")]
    [SerializeField] Image fadeImage; // 페이드 효과를 적용할 이미지

    [Header("씬이름")]
    [SerializeField] string sceneName = "GameScene"; // 전환할 씬 이름

    [Header("할지말지")]
    public bool isSceneTransition = false; // 씬 전환 여부



    void Start()
    {
        fadeImage.DOFade(fadeAlpha, fadeDuration).SetDelay(fadeDelay).OnComplete(() =>
        {
            if (isSceneTransition)
            {
                SceneManager.LoadScene(sceneName); // 씬 전환
            }

            Debug.Log("페이드 아웃 완료");
        });
    }

    // Update is called once per frame
    void Update()
    {

    }
}
