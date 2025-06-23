using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI 요소에 그라디언트 효과를 적용하는 컴포넌트
/// Image나 Text 컴포넌트에 추가하여 사용
/// </summary>
[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
    [Header("그라디언트 설정")]
    [SerializeField] public GradientType gradientType = GradientType.Vertical;
    [SerializeField] public Color topColor = Color.white;
    [SerializeField] public Color bottomColor = Color.black;
    [SerializeField] public Color leftColor = Color.white;
    [SerializeField] public Color rightColor = Color.black;

    [Header("다중 색상 그라디언트")]
    [SerializeField] private bool useMultipleColors = false;
    [SerializeField] private Color[] gradientColors = new Color[4] { Color.red, Color.yellow, Color.green, Color.blue };

    [Header("애니메이션 설정")]
    [SerializeField] private bool animateGradient = false;
    [SerializeField] private float animationSpeed = 1f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    // 그라디언트 타입 정의
    public enum GradientType
    {
        Vertical,       // 수직 그라디언트
        Horizontal,     // 수평 그라디언트
        Diagonal,       // 대각선 그라디언트
        Radial,         // 원형 그라디언트
        FourCorner      // 네 모서리 그라디언트
    }

    // 애니메이션 시간 추적 변수
    private float animationTime = 0f;

    // 메시 수정 메인 메서드
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

    // 애니메이션 시간 업데이트 메서드
    private void UpdateAnimationTime()
    {
        if (animateGradient)
        {
            animationTime += Time.deltaTime * animationSpeed;
            if (animationTime > 1f)
                animationTime = 0f;
        }
    }

    // 그라디언트 타입에 따라 색상 적용하는 메서드
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

    // 수직 그라디언트 적용 메서드
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

    // 수평 그라디언트 적용 메서드
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

    // 대각선 그라디언트 적용 메서드
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

    // 원형 그라디언트 적용 메서드
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

    // 네 모서리 그라디언트 적용 메서드
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

    // 수직 경계 계산 메서드
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

    // 수평 경계 계산 메서드
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

    // 정규화된 위치 계산 메서드
    private float CalculateNormalizedPosition(float value, float min, float max)
    {
        return Mathf.InverseLerp(min, max, value);
    }

    // 애니메이션 적용 메서드
    private float ApplyAnimation(float normalizedValue)
    {
        return (normalizedValue + animationCurve.Evaluate(animationTime)) % 1f;
    }

    // 그라디언트 색상 계산 메서드
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

    // 다중 색상 그라디언트 계산 메서드
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

    // 네 모서리 색상 계산 메서드
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

    // 모서리 색상 가져오기 메서드
    private Color GetCornerColor(int index, Color defaultColor)
    {
        if (gradientColors.Length > index)
        {
            return gradientColors[index];
        }
        return defaultColor;
    }

    // 애니메이션을 색상에 적용하는 메서드
    private Color ApplyAnimationToColor(Color color)
    {
        float animValue = animationCurve.Evaluate(animationTime);
        return Color.Lerp(color, Color.white, animValue * 0.3f);
    }

    // 중심점 계산 메서드
    private Vector2 CalculateCenter()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            return rectTransform.rect.center;
        }
        return Vector2.zero;
    }

    // 최대 거리 계산 메서드
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

    // 애니메이션 업데이트 (MonoBehaviour의 Update 메서드)
    void Update()
    {
        if (animateGradient)
        {
            // 메시를 다시 그리도록 강제
            if (graphic != null)
            {
                graphic.SetVerticesDirty();
            }
        }
    }

    // 에디터에서 값 변경 시 실시간 업데이트
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