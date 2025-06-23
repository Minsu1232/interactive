using UnityEngine;

public class GameManagerSub : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Fix: Use a lambda expression to correctly subscribe to the event
        GameManager.Instance.OnGameStateChanged += StartGame;
    }

    // �̺�Ʈ �ڵ鷯 - ���� ���� ���� �� ȣ��
    private void StartGame(GameState gameState)
    {
        // ���� ���°� ���� ���·� ����Ǹ� ������ �����ϴ� ������ ���⿡ �ۼ��մϴ�.
        if (gameState == GameState.Ready)
        {
            GameManager.Instance.StartGame();
            // ��: �� ��ȯ, �ʱ�ȭ �۾� ��
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
