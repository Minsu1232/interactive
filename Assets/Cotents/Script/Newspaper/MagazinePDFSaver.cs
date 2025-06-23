using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System;
using TMPro;

/// <summary>
/// 매거진을 PDF로 저장하는 매니저 (GlobalLoading 적용)
/// </summary>
public class MagazinePDFSaver : MonoBehaviour
{
    [Header("UI 참조")]
    public Canvas magazineCanvas;           // 매거진이 표시되는 캔버스
    public Button saveButton;               // PDF 저장 버튼
    public TextMeshProUGUI saveText;        // 저장 버튼 텍스트

    [Header("PDF 설정")]
    public int captureWidth = 1920;         // 캡처 해상도 폭
    public int captureHeight = 1080;        // 캡처 해상도 높이
    public bool openAfterSave = true;       // 저장 후 파일 열기

    [Header("디버그")]
    public bool enableDebugLog = true;

    void Start()
    {
        // 저장 버튼 이벤트 연결
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(StartSaveToPDF);
        }

        // 버튼 텍스트 다국어 적용
        if (saveText != null)
        {
            // 예시: CSVLocalizationManager가 프로젝트에 있다고 가정합니다.
            saveText.text = CSVLocalizationManager.Instance?.GetLocalizedText("ui_save") ?? "PDF 저장";
            
        }
    }

    /// <summary>
    /// PDF 저장 시작 (버튼 클릭 시 호출)
    /// </summary>
    public void StartSaveToPDF()
    {
        if (magazineCanvas == null)
        {
            Debug.LogError("❌ 매거진 캔버스가 설정되지 않았습니다!");
            return;
        }

        StartCoroutine(SaveMagazineToPDFCoroutine());
        saveButton.gameObject.SetActive(false); // 저장 중에는 버튼 비활성화
    }

    /// <summary>
    /// 실제 PDF 저장 코루틴 (GlobalLoading 적용)
    /// </summary>
    IEnumerator SaveMagazineToPDFCoroutine()
    {
        // 1. 로딩 UI 표시 (GlobalLoading은 프로젝트에 맞게 구현 필요)
        // GlobalLoading.ShowProcessing(); 

        yield return new WaitForEndOfFrame();

        // 2. 캔버스 스크린샷 캡처
        Texture2D screenshot = null;
        string savedPath = "";
        bool isSuccess = false;
        bool hasError = false;

        try
        {
            screenshot = CaptureCanvasAsTexture();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ 스크린샷 캡처 오류: {ex.Message}");
            hasError = true;
        }

        if (hasError || screenshot == null)
        {
            // GlobalLoading.Hide();
            yield break;
        }

        // 3. 상태 업데이트 (필요 시)
        // GlobalLoading.ShowSaving();
        yield return new WaitForSeconds(0.5f);

        // 4. PDF 생성 및 저장
        try
        {
            savedPath = SaveScreenshotAsPDF(screenshot);
            isSuccess = !string.IsNullOrEmpty(savedPath);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ PDF 저장 중 오류: {ex.Message}");
            isSuccess = false;
        }

        // 5. 메모리 정리
        if (screenshot != null)
        {
            Destroy(screenshot);
        }

        // 6. 결과 처리
        if (isSuccess)
        {
            if (enableDebugLog)
            {
                Debug.Log($"📄 PDF 저장 성공: {savedPath}");
            }

            if (openAfterSave)
            {
                yield return new WaitForSeconds(1f);
                OpenSavedFile(savedPath);
            }
        }

        // 7. 로딩 UI 숨기기
        yield return new WaitForSeconds(2f);
        // GlobalLoading.Hide();
    }

    /// <summary>
    /// 캔버스를 Texture2D로 캡처 (Screen Space - Overlay 지원)
    /// </summary>
    Texture2D CaptureCanvasAsTexture()
    {
        if (magazineCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return CaptureOverlayCanvas();
        }
        else
        {
            return CaptureWithCamera();
        }
    }

    /// <summary>
    /// Screen Space - Overlay Canvas 캡처
    /// </summary>
    Texture2D CaptureOverlayCanvas()
    {
        GameObject tempCameraObj = new GameObject("TempCaptureCamera");
        Camera tempCamera = tempCameraObj.AddComponent<Camera>();
        RenderMode originalMode = magazineCanvas.renderMode;
        Camera originalCamera = magazineCanvas.worldCamera;

        try
        {
            magazineCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            magazineCanvas.worldCamera = tempCamera;

            tempCamera.clearFlags = CameraClearFlags.SolidColor;
            tempCamera.backgroundColor = Color.white;
            tempCamera.cullingMask = 1 << magazineCanvas.gameObject.layer;
            tempCamera.orthographic = true;
            tempCamera.orthographicSize = Screen.height / 2f;
            tempCamera.nearClipPlane = 0.1f;
            tempCamera.farClipPlane = 100f;
            tempCamera.transform.position = new Vector3(Screen.width / 2f, Screen.height / 2f, -10f) - magazineCanvas.transform.position;

            RenderTexture renderTexture = new RenderTexture(captureWidth, captureHeight, 24, RenderTextureFormat.ARGB32);
            tempCamera.targetTexture = renderTexture;

            Canvas.ForceUpdateCanvases();
            tempCamera.Render();

            RenderTexture.active = renderTexture;
            Texture2D screenshot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
            screenshot.Apply();

            tempCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(renderTexture);

            return screenshot;
        }
        finally
        {
            magazineCanvas.renderMode = originalMode;
            magazineCanvas.worldCamera = originalCamera;
            if (tempCameraObj != null)
            {
                Destroy(tempCameraObj);
            }
        }
    }

    /// <summary>
    /// 기존 카메라 모드 캡처 (Screen Space - Camera, World Space용)
    /// </summary>
    Texture2D CaptureWithCamera()
    {
        Camera canvasCamera = magazineCanvas.worldCamera ?? Camera.main;
        if (canvasCamera == null)
        {
            Debug.LogError("캡처할 카메라를 찾을 수 없습니다.");
            return null;
        }

        RenderTexture renderTexture = new RenderTexture(captureWidth, captureHeight, 24);
        RenderTexture originalTarget = canvasCamera.targetTexture;

        try
        {
            canvasCamera.targetTexture = renderTexture;
            canvasCamera.Render();

            RenderTexture.active = renderTexture;
            Texture2D screenshot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
            screenshot.Apply();

            return screenshot;
        }
        finally
        {
            canvasCamera.targetTexture = originalTarget;
            RenderTexture.active = null;
            Destroy(renderTexture);
        }
    }

    /// <summary>
    /// 스크린샷을 PDF로 저장 (수정된 버전)
    /// </summary>
    string SaveScreenshotAsPDF(Texture2D screenshot)
    {
        try
        {
            string fileName = GeneratePDFFileName();
            string folderPath = GetSaveFolderPath();

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, fileName);

            // 1. 이미지를 PNG가 아닌 JPEG로 인코딩하여 /DCTDecode 필터와 일치시킵니다.
            byte[] imageBytes = screenshot.EncodeToJPG(90); // 퀄리티 90의 JPEG로 인코딩

            // 2. 안정적인 방식으로 PDF 바이트 배열을 생성합니다.
            byte[] pdfBytes = CreatePDFWithImage(imageBytes, screenshot.width, screenshot.height);

            File.WriteAllBytes(filePath, pdfBytes);

            return filePath;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ PDF 저장 오류: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 이미지를 포함하는 PDF 바이트 배열을 생성합니다.
    /// 문자열 조합 대신 스트림을 사용하여 각 객체의 위치를 정확히 계산하고 바이너리 데이터를 올바르게 씁니다.
    /// </summary>
    byte[] CreatePDFWithImage(byte[] imageBytes, int width, int height)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            // ASCII 인코딩을 사용하며, 스트림을 닫지 않도록 StreamWriter를 설정합니다.
            using (StreamWriter writer = new StreamWriter(stream, System.Text.Encoding.ASCII, 1024, true))
            {
                long[] xref = new long[6]; // 5개의 객체 + 0번 객체 = 6개 항목

                // PDF 헤더
                writer.Write("%PDF-1.4\n");
                writer.Write("%âãÏÓ\n"); // 바이너리 파일임을 명시
                writer.Flush();

                // 객체 1: Catalog (PDF 문서의 루트)
                xref[1] = stream.Position;
                writer.Write("1 0 obj\n");
                writer.Write("<< /Type /Catalog /Pages 2 0 R >>\n");
                writer.Write("endobj\n");
                writer.Flush();

                // 객체 2: Pages (페이지들의 목록)
                xref[2] = stream.Position;
                writer.Write("2 0 obj\n");
                writer.Write("<< /Type /Pages /Count 1 /Kids [3 0 R] >>\n");
                writer.Write("endobj\n");
                writer.Flush();

                // 객체 3: Page (실제 페이지 객체)
                xref[3] = stream.Position;
                writer.Write("3 0 obj\n");
                writer.Write($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {width} {height}] /Contents 4 0 R /Resources << /XObject << /Im1 5 0 R >> >> >>\n");
                writer.Write("endobj\n");
                writer.Flush();

                // 객체 4: Contents (페이지에 표시될 내용)
                string content = $"q\n{width} 0 0 {height} 0 0 cm\n/Im1 Do\nQ\n";
                xref[4] = stream.Position;
                writer.Write("4 0 obj\n");
                writer.Write($"<< /Length {content.Length} >>\n");
                writer.Write("stream\n");
                writer.Write(content);
                writer.Write("endstream\n");
                writer.Write("endobj\n");
                writer.Flush();

                // 객체 5: Image XObject (이미지 데이터)
                xref[5] = stream.Position;
                writer.Write("5 0 obj\n");
                writer.Write($"<< /Type /XObject /Subtype /Image /Width {width} /Height {height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {imageBytes.Length} >>\n");
                writer.Write("stream\n");
                writer.Flush(); // writer 버퍼를 비우고 스트림에 직접 씁니다.

                // 이미지의 원시 바이트 데이터를 PDF 스트림에 직접 씁니다. (Base64 인코딩 X)
                stream.Write(imageBytes, 0, imageBytes.Length);

                writer.Write("\nendstream\n");
                writer.Write("endobj\n");
                writer.Flush();

                // XRef 테이블 (각 객체의 파일 내 위치 정보)
                long startxref = stream.Position;
                writer.Write("xref\n");
                writer.Write("0 6\n"); // 0번부터 6개 객체
                writer.Write("0000000000 65535 f \n");
                writer.Write($"{xref[1]:D10} 00000 n \n");
                writer.Write($"{xref[2]:D10} 00000 n \n");
                writer.Write($"{xref[3]:D10} 00000 n \n");
                writer.Write($"{xref[4]:D10} 00000 n \n");
                writer.Write($"{xref[5]:D10} 00000 n \n");
                writer.Flush();

                // 트레일러
                writer.Write("trailer\n");
                writer.Write("<< /Size 6 /Root 1 0 R >>\n");
                writer.Write("startxref\n");
                writer.Write($"{startxref}\n");
                writer.Write("%%EOF\n");
                writer.Flush();
            }
            return stream.ToArray();
        }
    }

    /// <summary>
    /// PDF 파일명 생성
    /// </summary>
    string GeneratePDFFileName()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        return $"Investment_Magazine_{timestamp}.pdf";
    }

    /// <summary>
    /// 저장 폴더 경로 가져오기
    /// </summary>
    string GetSaveFolderPath()
    {
        // 바탕화면을 기본 경로로 사용
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        return Path.Combine(desktopPath, "InvestmentGame");
    }

    /// <summary>
    /// 저장된 파일 열기
    /// </summary>
    void OpenSavedFile(string filePath)
    {
        try
        {
            // 플랫폼에 맞는 파일 열기 명령어 실행
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // 윈도우에서는 경로에 \ 사용
            System.Diagnostics.Process.Start("explorer.exe", "/select," + filePath.Replace("/", "\\"));
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            System.Diagnostics.Process.Start("open", Path.GetDirectoryName(filePath));
#else
            // 기타 플랫폼에서는 기본 프로그램으로 파일 열기 시도
            Application.OpenURL("file://" + filePath);
#endif
        }
        catch (System.Exception ex)
        {
            if (enableDebugLog)
            {
                Debug.Log($"파일 열기 실패: {ex.Message}");
            }
        }
    }
}