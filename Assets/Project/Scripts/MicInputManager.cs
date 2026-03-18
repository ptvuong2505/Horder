using UnityEngine;

/// <summary>
/// MicInputManager
/// Ghi âm liên tục từ Microphone để lấy độ lớn âm thanh (RMS).
/// Cung cấp hệ số nhân tốc độ bắn (VolumeBoost) dựa trên độ lớn âm lượng.
/// </summary>
public class MicInputManager : MonoBehaviour
{
    public static MicInputManager Instance;

    [Header("Mic Settings")]
    [Tooltip("Tên thiết bị Microphone. Để trống sẽ dùng thiết bị mặc định.")]
    public string micDeviceName = null;
    [Tooltip("Số lượng mẫu âm thanh để phân tích mỗi frame")]
    public int sampleWindow = 2048;

    [Header("Boost Settings")]
    [Tooltip("Ngưỡng âm lượng tối thiểu để bắt đầu nhận diện (lọc tiếng ồn nhẹ)")]
    public float thresholdVolume = 0.002f;
    [Tooltip("Hệ số nhân tối đa cho tốc độ bắn khi hét to nhất")]
    public float maxBoostMultiplier = 5f;
    [Tooltip("Hệ số khuếch đại độ nhạy mic nội bộ")]
    public float sensitivityMultiplier = 400f;

    private AudioClip microphoneClip;
    private bool isInitialized = false;
    private float smoothedLoudness = 0f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitMic();
    }

    void InitMic()
    {
        if (Microphone.devices.Length > 0)
        {
            if (string.IsNullOrEmpty(micDeviceName))
            {
                micDeviceName = Microphone.devices[0]; // Mặc định mic đầu tiên
            }

            // Ghi âm vòng lặp 1 giây, tần số 44100Hz
            microphoneClip = Microphone.Start(micDeviceName, true, 1, 44100);
            isInitialized = true;
            Debug.Log($"[MicInputManager] Đã bật Microphone: {micDeviceName}");
        }
        else
        {
            Debug.LogWarning("[MicInputManager] Không tìm thấy Microphone nào trên thiết bị!");
            isInitialized = false;
        }
    }

    void OnDisable()
    {
        if (isInitialized)
        {
            Microphone.End(micDeviceName);
            isInitialized = false;
        }
    }

    /// <summary>
    /// Lấy độ lớn của âm thanh (Loudness / RMS) trong frame hiện tại.
    /// </summary>
    public float GetLoudness()
    {
        if (!isInitialized || microphoneClip == null) return 0f;

        int micPosition = Microphone.GetPosition(micDeviceName);
        if (micPosition < 0 || micPosition == 0) return 0f;

        float[] waveData = new float[sampleWindow];
        // Đảm bảo không đọc quá giới hạn của clip bằng cách dời position lùi lại 1 chút bằng size sampleWindow
        int startPosition = micPosition - sampleWindow;
        if (startPosition < 0) startPosition = 0;

        microphoneClip.GetData(waveData, startPosition);

        // Tính RMS (Root Mean Square)
        float totalSquare = 0f;
        for (int i = 0; i < sampleWindow; i++)
        {
            totalSquare += waveData[i] * waveData[i];
        }

        float rms = Mathf.Sqrt(totalSquare / sampleWindow);
        
        // Cập nhật giá trị làm mượt (Smoothing) để biểu đồ sóng âm không bị giật lác
        smoothedLoudness = Mathf.Lerp(smoothedLoudness, rms, Time.deltaTime * 15f);
        return smoothedLoudness;
    }

    /// <summary>
    /// Trả về hệ số nhân tốc độ đạn dựa vào độ lớn âm thanh.
    /// Giá trị trả về từ 1f (mặc định) đến maxBoostMultiplier.
    /// </summary>
    public float GetVolumeBoost()
    {
        float loudness = GetLoudness();
        
        // Lọc tiếng ồn quá nhỏ
        if (loudness < thresholdVolume)
        {
            return 1f; // Hệ số mặc định, không tăng tốc
        }

        // Tính toán boost multiplier (ví dụ loudness * sensitivityMultiplier + 1f)
        float calculatedBoost = 1f + (loudness * sensitivityMultiplier);

        // Giới hạn trong khoảng [1f, maxBoostMultiplier]
        return Mathf.Clamp(calculatedBoost, 1f, maxBoostMultiplier);
    }
}
