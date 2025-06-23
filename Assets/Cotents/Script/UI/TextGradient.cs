using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Text ������Ʈ ���� �׶���Ʈ ��ũ��Ʈ
/// ���� UIGradient�� ������ ���
/// </summary>
[AddComponentMenu("UI/Effects/Text Gradient")]
public class TextGradient : BaseMeshEffect
{
    [Header("�ؽ�Ʈ �׶���Ʈ ����")]
    [SerializeField] private Color topColor = new Color(0.66f, 0.33f, 0.97f, 1f); // #a855f7
    [SerializeField] private Color bottomColor = new Color(0.02f, 0.71f, 0.83f, 1f); // #06b6d4
    [SerializeField] private bool animateColors = true;
    [SerializeField] private float animationSpeed = 2f;

    private float animTime = 0f;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        List<UIVertex> vertexList = new List<UIVertex>();
        vh.GetUIVertexStream(vertexList);

        ApplyTextGradient(vertexList);

        vh.Clear();
        vh.AddUIVertexTriangleStream(vertexList);
    }

    void ApplyTextGradient(List<UIVertex> vertexList)
    {
        if (vertexList.Count == 0) return;

        // Y ��ǥ ���� ã��
        float minY = vertexList[0].position.y;
        float maxY = vertexList[0].position.y;

        for (int i = 1; i < vertexList.Count; i++)
        {
            if (vertexList[i].position.y < minY) minY = vertexList[i].position.y;
            if (vertexList[i].position.y > maxY) maxY = vertexList[i].position.y;
        }

        float height = maxY - minY;

        // �� ���ؽ��� �׶���Ʈ ����
        for (int i = 0; i < vertexList.Count; i++)
        {
            UIVertex vertex = vertexList[i];

            // Y ��ġ�� ���� �׶���Ʈ ���
            float t = height > 0 ? (vertex.position.y - minY) / height : 0f;

            // �ִϸ��̼� ����x
            if (animateColors)
            {
                float wave = Mathf.Sin(animTime * animationSpeed + t * Mathf.PI) * 0.2f + 0.8f;
                t = Mathf.Clamp01(t * wave);
            }

            Color gradientColor = Color.Lerp(bottomColor, topColor, t);
            vertex.color = gradientColor;
            vertexList[i] = vertex;
        }
    }

    void Update()
    {
        if (animateColors)
        {
            animTime += Time.deltaTime;
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        if (graphic != null)
        {
            graphic.SetVerticesDirty();
        }
    }
#endif
}