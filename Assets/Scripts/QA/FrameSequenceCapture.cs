using System.Collections;
using System.IO;
using UnityEngine;

// P9 QA 전용: 원속 연속 비교 영상용 프레임 저장. 게임플레이 코드는 이 컴포넌트를 참조하지 않는다.
// Time.captureFramerate로 게임 시간을 고정 간격으로 진행시키고, 매 프레임 끝에 실제 화면(GameView 백버퍼)을 JPG로 남긴다.
// 2026-07-17 크래시(Play 중 RenderTexture로 Camera.Render 직접 호출)를 피하려고 ScreenCapture만 쓴다.
public sealed class FrameSequenceCapture : MonoBehaviour
{
    public string folder;
    public int quality = 82;
    public bool capturing;
    public int Frames { get; private set; }
    public float LastEncodeMs { get; private set; }
    public System.Action OnCapturedFrame;

    IEnumerator Start()
    {
        var endOfFrame = new WaitForEndOfFrame();
        while (true)
        {
            yield return endOfFrame;
            if (!capturing || string.IsNullOrEmpty(folder)) continue;
            OnCapturedFrame?.Invoke();
            var started = System.Diagnostics.Stopwatch.StartNew();
            var frame = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(folder, $"f{Frames:00000}.jpg"), frame.EncodeToJPG(quality));
            Destroy(frame);
            Frames++;
            LastEncodeMs = (float)started.Elapsed.TotalMilliseconds;
        }
    }
}

// P9 QA 전용: 모션 검수용 근접 3/4 시점. 제품 카메라 권위(CameraController)를 잠시 끄고 같은 메인 카메라를 플레이어 옆에 둔다.
public sealed class QaCloseFollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 localOffset = new Vector3(2.2f, 1.45f, 2.6f); // 플레이어 기준 오른쪽 앞(3/4)
    public float lookHeight = .95f;
    public bool followYaw;

    void LateUpdate()
    {
        if (target == null) return;
        Quaternion basis = followYaw ? Quaternion.Euler(0f, target.eulerAngles.y, 0f) : Quaternion.identity;
        transform.position = target.position + basis * localOffset;
        transform.rotation = Quaternion.LookRotation(target.position + Vector3.up * lookHeight - transform.position);
    }
}
