using UnityEngine;

public class GameManagerSub : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Fix: Use a lambda expression to correctly subscribe to the event
        GameManager.Instance.OnGameStateChanged += StartGame;
    }

    // 이벤트 핸들러 - 게임 상태 변경 시 호출
    private void StartGame(GameState gameState)
    {
        // 게임 상태가 시작 상태로 변경되면 게임을 시작하는 로직을 여기에 작성합니다.
        if (gameState == GameState.Ready)
        {
            GameManager.Instance.StartGame();
            // 예: 씬 전환, 초기화 작업 등
        }
    }
    private void OnDestroy()
    {
        // Fix: Unsubscribe from the event to prevent memory leaks
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= StartGame;
        }
    }
}
