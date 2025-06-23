using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.IO;

public class ComfyUIClient : MonoBehaviour
{
    public static ComfyUIClient Instance { get; private set; }

   
    [Header("ComfyUI Settings")]
    public string serverURL = "http://127.0.0.1:8188";
    public string outputPath = "Assets/Contents/Image/";  // ✅ 경로 변경

    [Header("Model Configurations")]
    [SerializeField] private IconModelConfig iconConfig;
    [SerializeField] private LifestyleModelConfig lifestyleConfig;

    // 아이콘용 모델 설정
    [System.Serializable]
    public class IconModelConfig
    {
        [Header("Icon Generation Settings")]
        public string checkpointName = "sd_xl_base_1.0.safetensors";
        public int imageWidth = 512;
        public int imageHeight = 512;
        public int steps = 15; // 아이콘은 빠른 생성
        public float cfg = 6.0f;

        [Header("Style Settings")]
        [TextArea(3, 5)]
        public string baseStylePrompt = "simple 2D icon, minimalist design, clean flat icon, professional business symbol, vector style, no text, pure white background, isolated on white";

        [TextArea(2, 4)]
        public string negativePrompt = "colored background, blue background, gray background, gradient background, textured background, patterned background, 3d, realistic, photographic, complex details, shadows, text, letters, words, watermark, colored backdrop, non-white background";

        [Header("Consistency Settings")]
        public bool useFixedSeed = true; // 고정 시드 사용
    }

    // 라이프스타일 배경용 모델 설정  
    [System.Serializable]
    public class LifestyleModelConfig
    {
        [Header("Lifestyle Generation Settings")]
        public string checkpointName = "sd_xl_base_1.0.safetensors";
        public int imageWidth = 1024;
        public int imageHeight = 1024;
        public int steps = 25; // 배경은 품질 중시
        public float cfg = 7.5f;

        [Header("Style Settings")]
        [TextArea(3, 5)]
        public string baseStylePrompt = "clean realistic interior photography, modern architectural visualization, soft natural lighting, professional interior design, minimal clutter, contemporary style";

        [TextArea(2, 4)]
        public string negativePrompt = "people, humans, faces, text, signs, cluttered, messy, dark, poor lighting, low quality, blurry";

        [Header("Fixed vs Dynamic")]
        public bool generateFixedBackups = true; // 안전빵용 4종 사전 제작
        public bool enableRealTimeGeneration = true; // 실시간 생성 기능
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDirectories();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // InitializeDirectories() 메서드 수정
    private void InitializeDirectories()
    {
        // ✅ Assets 폴더 내 경로로 변경
        string fullPath = Path.Combine(Application.dataPath, "Contents", "Image");
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            Directory.CreateDirectory(Path.Combine(fullPath, "Generated")); // 생성된 이미지용
            Directory.CreateDirectory(Path.Combine(fullPath, "Fallback"));  // 폴백 이미지용
        }
    }

    /// <summary>
    /// 기업 아이콘 생성 (LoRA 사용, 고정 시드)
    /// </summary>
    public IEnumerator GenerateStockIcon(string iconPrompt, string fileName, System.Action<bool, string> onComplete = null)
    {
        // 완전한 프롬프트 조합
        string fullPrompt = $"{iconPrompt}, {iconConfig.baseStylePrompt}";

        // 고정 시드 생성 (같은 프롬프트 = 같은 결과)
        int fixedSeed = iconConfig.useFixedSeed ? GetConsistentSeed(iconPrompt) : UnityEngine.Random.Range(1, 2147483647);

        Debug.Log($"🏢 Generating icon with LoRA and fixed seed {fixedSeed}: {iconPrompt}");

        yield return StartCoroutine(GenerateImageWithLoRA(
            fullPrompt,
            iconConfig.negativePrompt,
            fileName,
            "Icons",
            iconConfig.imageWidth,
            iconConfig.imageHeight,
            iconConfig.steps,
            iconConfig.cfg,
            fixedSeed,
            iconConfig.checkpointName,
            "iconsXL.safetensors", // 실제 LoRA 파일명으로 수정
            0.8f, // 안정성을 위해 강도 낮춤
            onComplete
        ));
    }

    /// <summary>
    /// 라이프스타일 배경 생성 (Base 모델만, 안전빵용 고정)
    /// </summary>
    public IEnumerator GenerateLifestyleBackground(string lifestylePrompt, string fileName, System.Action<bool, string> onComplete = null)
    {
        string fullPrompt = $"{lifestylePrompt}, {lifestyleConfig.baseStylePrompt}";

        // 라이프스타일도 일관성을 위해 고정 시드 사용
        int fixedSeed = GetConsistentSeed(lifestylePrompt);

        Debug.Log($"🏠 Generating lifestyle background (Base Only): {lifestylePrompt}");

        yield return StartCoroutine(GenerateImageBaseOnly(
            fullPrompt,
            lifestyleConfig.negativePrompt,
            fileName,
            "Lifestyle",
            lifestyleConfig.imageWidth,
            lifestyleConfig.imageHeight,
            lifestyleConfig.steps,
            lifestyleConfig.cfg,
            fixedSeed,
            lifestyleConfig.checkpointName,
            onComplete
        ));
    }

    /// <summary>
    /// 실시간 개인화 라이프스타일 생성 (Base 모델만, 사용자 결과 기반)
    /// </summary>
    public IEnumerator GeneratePersonalizedLifestyle(string baseLifestyle, Dictionary<string, object> userStats, string fileName, System.Action<bool, string> onComplete = null)
    {
        // 사용자 통계를 바탕으로 프롬프트 개인화
        string personalizedPrompt = CreatePersonalizedPrompt(baseLifestyle, userStats);
        string fullPrompt = $"{personalizedPrompt}, {lifestyleConfig.baseStylePrompt}";

        // 실시간 생성은 랜덤 시드 (매번 다른 결과)
        int randomSeed = UnityEngine.Random.Range(1, 2147483647);

        Debug.Log($"🎯 Generating personalized lifestyle (Base Only): {personalizedPrompt}");

        yield return StartCoroutine(GenerateImageBaseOnly(
            fullPrompt,
            lifestyleConfig.negativePrompt,
            fileName,
            "Lifestyle",
            lifestyleConfig.imageWidth,
            lifestyleConfig.imageHeight,
            lifestyleConfig.steps,
            lifestyleConfig.cfg,
            randomSeed,
            lifestyleConfig.checkpointName,
            onComplete
        ));
    }

    /// <summary>
    /// 사용자 통계 기반 개인화 프롬프트 생성
    /// </summary>
    private string CreatePersonalizedPrompt(string baseLifestyle, Dictionary<string, object> userStats)
    {
        string prompt = baseLifestyle;

        // 투자 섹터 선호도 반영
        if (userStats.ContainsKey("preferredSector"))
        {
            string sector = userStats["preferredSector"].ToString();
            switch (sector)
            {
                case "tech":
                    prompt += ", modern smart home technology, digital lifestyle elements";
                    break;
                case "ev":
                    prompt += ", eco-friendly sustainable design, green living elements";
                    break;
                case "crypto":
                    prompt += ", futuristic high-tech aesthetic, digital age styling";
                    break;
                default:
                    prompt += ", classic elegant traditional styling";
                    break;
            }
        }

        // 수익률 기반 추가 요소
        if (userStats.ContainsKey("finalReturn"))
        {
            float returnRate = Convert.ToSingle(userStats["finalReturn"]);
            if (returnRate > 1.5f) // 150% 이상
            {
                prompt += ", luxury premium details, success indicators";
            }
            else if (returnRate < 0.8f) // 80% 미만
            {
                prompt += ", cozy modest comfortable atmosphere";
            }
        }

        return prompt;
    }

    /// <summary>
    /// LoRA 포함 이미지 생성 메소드 (아이콘용)
    /// </summary>
    private IEnumerator GenerateImageWithLoRA(
        string prompt,
        string negativePrompt,
        string fileName,
        string subfolder,
        int width,
        int height,
        int steps,
        float cfg,
        int seed,
        string checkpointName,
        string loraFileName,
        float loraStrength,
        System.Action<bool, string> onComplete)
    {
        // LoRA 포함 워크플로우 생성
        var workflow = CreateLoRAWorkflow(prompt, negativePrompt, width, height, steps, cfg, seed, checkpointName, loraFileName, loraStrength);

        yield return StartCoroutine(ExecuteWorkflow(workflow, fileName, subfolder, onComplete));
    }

    /// <summary>
    /// Base 모델만 사용하는 이미지 생성 메소드 (라이프스타일용)
    /// </summary>
    private IEnumerator GenerateImageBaseOnly(
        string prompt,
        string negativePrompt,
        string fileName,
        string subfolder,
        int width,
        int height,
        int steps,
        float cfg,
        int seed,
        string checkpointName,
        System.Action<bool, string> onComplete)
    {
        // Base 모델만 사용하는 워크플로우 생성
        var workflow = CreateBaseOnlyWorkflow(prompt, negativePrompt, width, height, steps, cfg, seed, checkpointName);

        yield return StartCoroutine(ExecuteWorkflow(workflow, fileName, subfolder, onComplete));
    }

    /// <summary>
    /// 공통 워크플로우 실행 메소드
    /// </summary>
    private IEnumerator ExecuteWorkflow(Dictionary<string, ComfyUINode> workflow, string fileName, string subfolder, System.Action<bool, string> onComplete)
    {
        // API 요청
        var apiRequest = new Dictionary<string, object>();
        apiRequest["prompt"] = workflow;
        apiRequest["client_id"] = System.Guid.NewGuid().ToString();

        string jsonData = JsonConvert.SerializeObject(apiRequest);

        using (UnityWebRequest request = new UnityWebRequest($"{serverURL}/prompt", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"ComfyUI request failed: {request.error}");
                onComplete?.Invoke(false, request.error);
                yield break;
            }

            var response = JsonConvert.DeserializeObject<Dictionary<string, object>>(request.downloadHandler.text);
            if (response.ContainsKey("prompt_id"))
            {
                string promptId = response["prompt_id"].ToString();
                yield return StartCoroutine(WaitAndDownloadImage(promptId, fileName, subfolder, onComplete));
            }
            else
            {
                onComplete?.Invoke(false, "Failed to queue prompt");
            }
        }
    }

    /// <summary>
    /// LoRA 포함 SDXL 워크플로우 생성 (아이콘용)
    /// </summary>
    private Dictionary<string, ComfyUINode> CreateLoRAWorkflow(
        string prompt,
        string negativePrompt,
        int width,
        int height,
        int steps,
        float cfg,
        int seed,
        string checkpointName,
        string loraFileName,
        float loraStrength)
    {
        var workflow = new Dictionary<string, ComfyUINode>();

        // 1. Load Checkpoint
        var loadCheckpoint = new ComfyUINode("CheckpointLoaderSimple");
        loadCheckpoint.inputs["ckpt_name"] = checkpointName;
        workflow["4"] = loadCheckpoint;

        // 2. Load LoRA
        var loadLoRA = new ComfyUINode("LoraLoader");
        loadLoRA.inputs["model"] = new object[] { "4", 0 };
        loadLoRA.inputs["clip"] = new object[] { "4", 1 };
        loadLoRA.inputs["lora_name"] = loraFileName;
        loadLoRA.inputs["strength_model"] = loraStrength;
        loadLoRA.inputs["strength_clip"] = loraStrength;
        workflow["10"] = loadLoRA;

        // 3. Positive CLIP Text Encode (LoRA 적용된 CLIP 사용)
        var clipTextEncode = new ComfyUINode("CLIPTextEncode");
        clipTextEncode.inputs["text"] = prompt;
        clipTextEncode.inputs["clip"] = new object[] { "10", 1 }; // LoRA CLIP
        workflow["6"] = clipTextEncode;

        // 4. Negative CLIP Text Encode (LoRA 적용된 CLIP 사용)
        var clipTextEncodeNeg = new ComfyUINode("CLIPTextEncode");
        clipTextEncodeNeg.inputs["text"] = negativePrompt;
        clipTextEncodeNeg.inputs["clip"] = new object[] { "10", 1 }; // LoRA CLIP
        workflow["7"] = clipTextEncodeNeg;

        // 5. Empty Latent Image
        var emptyLatent = new ComfyUINode("EmptyLatentImage");
        emptyLatent.inputs["width"] = width;
        emptyLatent.inputs["height"] = height;
        emptyLatent.inputs["batch_size"] = 1;
        workflow["5"] = emptyLatent;

        // 6. KSampler (LoRA 적용된 모델 사용)
        var ksampler = new ComfyUINode("KSampler");
        ksampler.inputs["seed"] = seed;
        ksampler.inputs["steps"] = steps;
        ksampler.inputs["cfg"] = cfg;
        ksampler.inputs["sampler_name"] = "dpmpp_2m";
        ksampler.inputs["scheduler"] = "karras";
        ksampler.inputs["denoise"] = 1.0f;
        ksampler.inputs["model"] = new object[] { "10", 0 }; // LoRA Model
        ksampler.inputs["positive"] = new object[] { "6", 0 };
        ksampler.inputs["negative"] = new object[] { "7", 0 };
        ksampler.inputs["latent_image"] = new object[] { "5", 0 };
        workflow["3"] = ksampler;

        // 7. VAE Decode
        var vaeDecode = new ComfyUINode("VAEDecode");
        vaeDecode.inputs["samples"] = new object[] { "3", 0 };
        vaeDecode.inputs["vae"] = new object[] { "4", 2 }; // Base VAE
        workflow["8"] = vaeDecode;

        // 8. Save Image
        var saveImage = new ComfyUINode("SaveImage");
        saveImage.inputs["filename_prefix"] = "Icon_LoRA";
        saveImage.inputs["images"] = new object[] { "8", 0 };
        workflow["9"] = saveImage;

        return workflow;
    }

    /// <summary>
    /// Base 모델만 사용하는 SDXL 워크플로우 생성 (라이프스타일용)
    /// </summary>
    private Dictionary<string, ComfyUINode> CreateBaseOnlyWorkflow(
        string prompt,
        string negativePrompt,
        int width,
        int height,
        int steps,
        float cfg,
        int seed,
        string checkpointName)
    {
        var workflow = new Dictionary<string, ComfyUINode>();

        // 1. Load Checkpoint
        var loadCheckpoint = new ComfyUINode("CheckpointLoaderSimple");
        loadCheckpoint.inputs["ckpt_name"] = checkpointName;
        workflow["4"] = loadCheckpoint;

        // 2. Positive CLIP Text Encode (Base CLIP)
        var clipTextEncode = new ComfyUINode("CLIPTextEncode");
        clipTextEncode.inputs["text"] = prompt;
        clipTextEncode.inputs["clip"] = new object[] { "4", 1 }; // Base CLIP
        workflow["6"] = clipTextEncode;

        // 3. Negative CLIP Text Encode (Base CLIP)
        var clipTextEncodeNeg = new ComfyUINode("CLIPTextEncode");
        clipTextEncodeNeg.inputs["text"] = negativePrompt;
        clipTextEncodeNeg.inputs["clip"] = new object[] { "4", 1 }; // Base CLIP
        workflow["7"] = clipTextEncodeNeg;

        // 4. Empty Latent Image
        var emptyLatent = new ComfyUINode("EmptyLatentImage");
        emptyLatent.inputs["width"] = width;
        emptyLatent.inputs["height"] = height;
        emptyLatent.inputs["batch_size"] = 1;
        workflow["5"] = emptyLatent;

        // 5. KSampler (Base Model)
        var ksampler = new ComfyUINode("KSampler");
        ksampler.inputs["seed"] = seed;
        ksampler.inputs["steps"] = steps;
        ksampler.inputs["cfg"] = cfg;
        ksampler.inputs["sampler_name"] = "dpmpp_2m";
        ksampler.inputs["scheduler"] = "karras";
        ksampler.inputs["denoise"] = 1.0f;
        ksampler.inputs["model"] = new object[] { "4", 0 }; // Base Model
        ksampler.inputs["positive"] = new object[] { "6", 0 };
        ksampler.inputs["negative"] = new object[] { "7", 0 };
        ksampler.inputs["latent_image"] = new object[] { "5", 0 };
        workflow["3"] = ksampler;

        // 6. VAE Decode
        var vaeDecode = new ComfyUINode("VAEDecode");
        vaeDecode.inputs["samples"] = new object[] { "3", 0 };
        vaeDecode.inputs["vae"] = new object[] { "4", 2 };
        workflow["8"] = vaeDecode;

        // 7. Save Image
        var saveImage = new ComfyUINode("SaveImage");
        saveImage.inputs["filename_prefix"] = "Lifestyle_Base";
        saveImage.inputs["images"] = new object[] { "8", 0 };
        workflow["9"] = saveImage;

        return workflow;
    }

    /// <summary>
    /// 프롬프트 기반 일관성 있는 시드 생성
    /// </summary>
    private int GetConsistentSeed(string prompt)
    {
        int hash = prompt.GetHashCode();
        if (hash < 0) hash = -hash;
        hash = hash % 2147483647;
        return hash;
    }

    /// <summary>
    /// 이미지 생성 완료 대기 및 다운로드 (기존 코드 유지)
    /// </summary>
    private IEnumerator WaitAndDownloadImage(string promptId, string fileName, string subfolder, System.Action<bool, string> onComplete)
    {
        float waitTime = 0f;
        float maxWaitTime = 60f;

        while (waitTime < maxWaitTime)
        {
            yield return new WaitForSeconds(2f);
            waitTime += 2f;

            using (UnityWebRequest historyRequest = UnityWebRequest.Get($"{serverURL}/history/{promptId}"))
            {
                yield return historyRequest.SendWebRequest();

                if (historyRequest.result == UnityWebRequest.Result.Success)
                {
                    var historyData = JsonConvert.DeserializeObject<Dictionary<string, object>>(historyRequest.downloadHandler.text);

                    if (historyData.ContainsKey(promptId))
                    {
                        var promptData = JsonConvert.DeserializeObject<Dictionary<string, object>>(historyData[promptId].ToString());

                        if (promptData.ContainsKey("outputs"))
                        {
                            yield return StartCoroutine(DownloadGeneratedImage(promptId, fileName, subfolder, onComplete));
                            yield break;
                        }
                    }
                }
            }
        }

        Debug.LogError("Image generation timeout");
        onComplete?.Invoke(false, "Timeout");
    }

    /// <summary>
    /// 생성된 이미지 다운로드 (기존 코드 유지)
    /// </summary>
    private IEnumerator DownloadGeneratedImage(string promptId, string fileName, string subfolder, System.Action<bool, string> onComplete)
    {
        using (UnityWebRequest historyRequest = UnityWebRequest.Get($"{serverURL}/history/{promptId}"))
        {
            yield return historyRequest.SendWebRequest();

            if (historyRequest.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(false, historyRequest.error);
                yield break;
            }

            string historyJson = historyRequest.downloadHandler.text;
            var actualFileNames = ExtractFileNamesFromHistory(historyJson, promptId);

            if (actualFileNames.Count > 0)
            {
                foreach (string actualFileName in actualFileNames)
                {
                    string imageUrl = $"{serverURL}/view?filename={actualFileName}";

                    using (UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(imageUrl))
                    {
                        yield return imageRequest.SendWebRequest();

                        if (imageRequest.result == UnityWebRequest.Result.Success)
                        {
                            DownloadHandlerTexture textureHandler = imageRequest.downloadHandler as DownloadHandlerTexture;
                            if (textureHandler != null)
                            {
                                Texture2D texture = textureHandler.texture;

                                if (texture != null && texture.width > 1 && texture.height > 1)
                                {
                                    byte[] pngData = texture.EncodeToPNG();
                                    string savePath = Path.Combine(Application.dataPath, "Contents", "Image", "Generated", $"{fileName}.png");

                                    string directory = Path.GetDirectoryName(savePath);
                                    if (!Directory.Exists(directory))
                                    {
                                        Directory.CreateDirectory(directory);
                                    }

                                    File.WriteAllBytes(savePath, pngData);
                                    Debug.Log($"✅ Image saved: {savePath}");

                                    onComplete?.Invoke(true, savePath);
                                    yield break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                onComplete?.Invoke(false, "No valid image files found");
            }
        }
    }

    /// <summary>
    /// History JSON에서 파일명 추출 (기존 코드 유지)
    /// </summary>
    private List<string> ExtractFileNamesFromHistory(string historyJson, string promptId)
    {
        var fileNames = new List<string>();

        try
        {
            var historyData = JsonConvert.DeserializeObject<Dictionary<string, object>>(historyJson);

            if (historyData.ContainsKey(promptId))
            {
                var promptData = JsonConvert.DeserializeObject<Dictionary<string, object>>(historyData[promptId].ToString());

                if (promptData.ContainsKey("outputs"))
                {
                    var outputs = JsonConvert.DeserializeObject<Dictionary<string, object>>(promptData["outputs"].ToString());

                    foreach (var outputPair in outputs)
                    {
                        try
                        {
                            var nodeOutput = JsonConvert.DeserializeObject<Dictionary<string, object>>(outputPair.Value.ToString());

                            if (nodeOutput.ContainsKey("images"))
                            {
                                var images = JsonConvert.DeserializeObject<object[]>(nodeOutput["images"].ToString());

                                foreach (var imageObj in images)
                                {
                                    var imageInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(imageObj.ToString());

                                    if (imageInfo.ContainsKey("filename"))
                                    {
                                        string filename = imageInfo["filename"].ToString();
                                        fileNames.Add(filename);
                                    }
                                }
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.Log($"⚠️ Skipping output node {outputPair.Key}: {e.Message}");
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ JSON parsing error: {e.Message}");
        }

        return fileNames;
    }

    public IEnumerator TestConnection(System.Action<bool> onComplete)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{serverURL}/system_stats"))
        {
            yield return request.SendWebRequest();
            bool isConnected = request.result == UnityWebRequest.Result.Success;
            onComplete?.Invoke(isConnected);
        }
    }

    [System.Serializable]
    public class ComfyUINode
    {
        public Dictionary<string, object> inputs;
        public string class_type;

        public ComfyUINode(string classType)
        {
            class_type = classType;
            inputs = new Dictionary<string, object>();
        }
    }
}