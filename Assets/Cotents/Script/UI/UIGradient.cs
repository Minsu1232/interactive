using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI ��ҿ� �׶���Ʈ ȿ���� �����ϴ� ������Ʈ
/// Image�� Text ������Ʈ�� �߰��Ͽ� ���
/// </summary>
[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
    [Header("�׶���Ʈ ����")]
    [SerializeField] public GradientType gradientType = GradientType.Vertical;
    [SerializeField] public Color topColor = Color.white;
    [SerializeField] public Color bottomColor = Color.black;
    [SerializeField] public Color leftColor = Color.white;
    [SerializeField] public Color rightColor = Color.black;

    [Header("���� ���� �׶���Ʈ")]
    [SerializeField] private bool useMultipleColors = false;
    [SerializeField] private Color[] gradientColors = new Color[4] { Color.red, Color.yellow, Color.green, Color.blue };

    [Header("�ִϸ��̼� ����")]
    [SerializeField] private bool animateGradient = false;
    [SerializeField] private float animationSpeed = 1f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    // �׶���Ʈ Ÿ�� ����
    public enum GradientType
    {
        Vertical,       // ���� �׶���Ʈ
        Horizontal,     // ���� �׶���Ʈ
        Diagonal,       // �밢�� �׶���Ʈ
        Radial,         // ���� �׶���Ʈ
        FourCorner      // �� �𼭸� �׶���Ʈ
    }

    // �ִϸ��̼� �ð� ���� ����
    private float animationTime = 0f;

    // �޽� ���� ���� �޼���
    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        List<UIVertex> vertices = new List<UIVertex>();
        vh.GetUIVertexStream(vertices);

        UpdateAnimationTime();
        ApplyGradientToVertices(vertices);

        vh.Clear();
        vh.AddUIVertexTriangleStream(vertices);
    }

    // �ִϸ��̼� �ð� ������Ʈ �޼���
    private void UpdateAnimationTime()
    {
        if (animateGradient)
        {
            animationTime += Time.deltaTime * animationSpeed;
            if (animationTime > 1f)
                animationTime = 0f;
        }
    }

    // �׶���Ʈ Ÿ�Կ� ���� ���� �����ϴ� �޼���
    private void ApplyGradientToVertices(List<UIVertex> vertices)
    {
        switch (gradientType)
        {
            case GradientType.Vertical:
                ApplyVerticalGradient(vertices);
                break;
            case GradientType.Horizontal:
                ApplyHorizontalGradient(vertices);
                break;
            case GradientType.Diagonal:
                ApplyDiagonalGradient(vertices);
                break;
            case GradientType.Radial:
                ApplyRadialGradient(vertices);
                break;
            case GradientType.FourCorner:
                ApplyFourCornerGradient(vertices);
                break;
        }
    }

    // ���� �׶���Ʈ ���� �޼���
    private void ApplyVerticalGradient(List<UIVertex> vertices)
    {
        Vector2 bounds = CalculateVerticalBounds(vertices);
        float minY = bounds.x;
        float maxY = bounds.y;

        for (int i = 0; i < vertices.Count; i++)
        {
            UIVertex vertex = vertices[i];
            float normalizedY = CalculateNormalizedPosition(vertex.position.y, minY, maxY);

            if (animateGradient)
            {
                normalizedY = ApplyAnimation(normalizedY);
            }

            Color targetColor = CalculateGradientColor(normalizedY, bottomColor, topColor);
            vertex.color = Color32.Lerp(vertex.color, targetColor, 1f);
            vertices[i] = vertex;
        }
    }

    // ���� �׶���Ʈ ���� �޼���
    private void ApplyHorizontalGradient(List<UIVertex> vertices)
    {
        Vector2 bounds = CalculateHorizontalBounds(vertices);
        float minX = bounds.x;
        float maxX = bounds.y;

        for (int i = 0; i < vertices.Count; i++)
        {
            UIVertex vertex = vertices[i];
            float normalizedX = CalculateNormalizedPosition(vertex.position.x, minX, maxX);

            if (animateGradient)
            {
                normalizedX = ApplyAnimation(normalizedX);
            }

            Color targetColor = CalculateGradientColor(normalizedX, leftColor, rightColor);
            vertex.color = Color32.Lerp(vertex.color, targetColor, 1f);
            vertices[i] = vertex;
        }
    }

    // �밢�� �׶���Ʈ ���� �޼���
    private void ApplyDiagonalGradient(List<UIVertex> vertices)
    {
        Vector2 horizontalBounds = CalculateHorizontalBounds(vertices);
        Vector2 verticalBounds = CalculateVerticalBounds(vertices);

        float minX = horizontalBounds.x;
        float maxX = horizontalBounds.y;
        float minY = verticalBounds.x;
        float maxY = verticalBounds.y;

        for (int i = 0; i < vertices.Count; i++)
        {
            UIVertex vertex = vertices[i];
            float normalizedX = CalculateNormalizedPosition(vertex.position.x, minX, maxX);
            float normalizedY = CalculateNormalizedPosition(vertex.position.y, minY, maxY);
            float diagonal = (normalizedX + normalizedY) * 0.5f;

            if (animateGradient)
            {
                diagonal = ApplyAnimation(diagonal);
            }

            Color targetColor = CalculateGradientColor(diagonal, bottomColor, topColor);
            vertex.color = Color32.Lerp(vertex.color, targetColor, 1f);
            vertices[i] = vertex;
        }
    }

    // ���� �׶���Ʈ ���� �޼���
    private void ApplyRadialGradient(List<UIVertex> vertices)
    {
        Vector2 center = CalculateCenter();
        float maxDistance = CalculateMaxDistance(vertices, center);

        for (int i = 0; i < vertices.Count; i++)
        {
            UIVertex vertex = vertices[i];
            Vector2 vertexPos = new Vector2(vertex.position.x, vertex.position.y);
            float distance = Vector2.Distance(vertexPos, center);
            float normalizedDistance = CalculateNormalizedPosition(distance, 0, maxDistance);

            if (animateGradient)
            {
                normalizedDistance = ApplyAnimation(normalizedDistance);
            }

            Color targetColor = CalculateGradientColor(normalizedDistance, topColor, bottomColor);
            vertex.color = Color32.Lerp(vertex.color, targetColor, 1f);
            vertices[i] = vertex;
        }
    }

    // �� �𼭸� �׶���Ʈ ���� �޼���
    private void ApplyFourCornerGradient(List<UIVertex> vertices)
    {
        Vector2 horizontalBounds = CalculateHorizontalBounds(vertices);
        Vector2 verticalBounds = CalculateVerticalBounds(vertices);

        float minX = horizontalBounds.x;
        float maxX = horizontalBounds.y;
        float minY = verticalBounds.x;
        float maxY = verticalBounds.y;

        for (int i = 0; i < vertices.Count; i++)
        {
            UIVertex vertex = vertices[i];
            float normalizedX = CalculateNormalizedPosition(vertex.position.x, minX, maxX);
            float normalizedY = CalculateNormalizedPosition(vertex.position.y, minY, maxY);

            Color finalColor = CalculateFourCornerColor(normalizedX, normalizedY);

            if (animateGradient)
            {
                finalColor = ApplyAnimationToColor(finalColor);
            }

            vertex.color = Color32.Lerp(vertex.color, finalColor, 1f);
            vertices[i] = vertex;
        }
    }

    // ���� ��� ��� �޼���
    private Vector2 CalculateVerticalBounds(List<UIVertex> vertices)
    {
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < vertices.Count; i++)
        {
            minY = Mathf.Min(minY, vertices[i].position.y);
            maxY = Mathf.Max(maxY, vertices[i].position.y);
        }

        return new Vector2(minY, maxY);
    }

    // ���� ��� ��� �޼���
    private Vector2 CalculateHorizontalBounds(List<UIVertex> vertices)
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;

        for (int i = 0; i < vertices.Count; i++)
        {
            minX = Mathf.Min(minX, vertices[i].position.x);
            maxX = Mathf.Max(maxX, vertices[i].position.x);
        }

        return new Vector2(minX, maxX);
    }

    // ����ȭ�� ��ġ ��� �޼���
    private float CalculateNormalizedPosition(float value, float min, float max)
    {
        return Mathf.InverseLerp(min, max, value);
    }

    // �ִϸ��̼� ���� �޼���
    private float ApplyAnimation(float normalizedValue)
    {
        return (normalizedValue + animationCurve.Evaluate(animationTime)) % 1f;
    }

    // �׶���Ʈ ���� ��� �޼���
    private Color CalculateGradientColor(float t, Color colorA, Color colorB)
    {
        if (useMultipleColors)
        {
            return CalculateMultipleColorGradient(t);
        }
        else
        {
            return Color.Lerp(colorA, colorB, t);
        }
    }

    // ���� ���� �׶���Ʈ ��� �޼���
    private Color CalculateMultipleColorGradient(float t)
    {
        if (gradientColors.Length == 0) return Color.white;
        if (gradientColors.Length == 1) return gradientColors[0];

        float scaledT = t * (gradientColors.Length - 1);
        int index = Mathf.FloorToInt(scaledT);
        float localT = scaledT - index;

        index = Mathf.Clamp(index, 0, gradientColors.Length - 2);
        return Color.Lerp(gradientColors[index], gradientColors[index + 1], localT);
    }

    // �� �𼭸� ���� ��� �޼���
    private Color CalculateFourCornerColor(float normalizedX, float normalizedY)
    {
        Color bottomLeftColor = GetCornerColor(0, bottomColor);
        Color bottomRightColor = GetCornerColor(1, bottomColor);
        Color topLeftColor = GetCornerColor(2, topColor);
        Color topRightColor = GetCornerColor(3, topColor);

        Color bottomInterpolated = Color.Lerp(bottomLeftColor, bottomRightColor, normalizedX);
        Color topInterpolated = Color.Lerp(topLeftColor, topRightColor, normalizedX);

        return Color.Lerp(bottomInterpolated, topInterpolated, normalizedY);
    }

    // �𼭸� ���� �������� �޼���
    private Color GetCornerColor(int index, Color defaultColor)
    {
        if (gradientColors.Length > index)
        {
            return gradientColors[index];
        }
        return defaultColor;
    }

    // �ִϸ��̼��� ���� �����ϴ� �޼���
    private Color ApplyAnimationToColor(Color color)
    {
        float animValue = animationCurve.Evaluate(animationTime);
        return Color.Lerp(color, Color.white, animValue * 0.3f);
    }

    // �߽��� ��� �޼���
    private Vector2 CalculateCenter()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            return rectTransform.rect.center;
        }
        return Vector2.zero;
    }

    // �ִ� �Ÿ� ��� �޼���
    private float CalculateMaxDistance(List<UIVertex> vertices, Vector2 center)
    {
        float maxDistance = 0f;
        for (int i = 0; i < vertices.Count; i++)
        {
            Vector2 vertexPos = new Vector2(vertices[i].position.x, vertices[i].position.y);
            float distance = Vector2.Distance(vertexPos, center);
            if (distance > maxDistance)
                maxDistance = distance;
        }
        return maxDistance;
    }

    // �ִϸ��̼� ������Ʈ (MonoBehaviour�� Update �޼���)
    void Update()
    {
        if (animateGradient)
        {
            // �޽ø� �ٽ� �׸����� ����
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }
    }

    // �����Ϳ��� �� ���� �� �ǽð� ������Ʈ
#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (Application.isPlaying && graphic != null)
        {
            graphic.SetVerticesDirty();
        }
    }
#endif
}