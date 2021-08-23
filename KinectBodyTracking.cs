using UnityEngine;
// AzureKinect SDK
using Microsoft.Azure.Kinect.Sensor;
using Microsoft.Azure.Kinect.BodyTracking;
// 비동기 처리
using System.Threading.Tasks;

public class KinectBodyTracking : MonoBehaviour
{
    // Kinect 변수
    Device kinect;

    // 컬러 텍스쳐
    Texture2D kinectColorTexture;

    [SerializeField]
    UnityEngine.UI.RawImage rawColorImg;

    // 트래커
    Tracker tracker;

    [SerializeField]
    GameObject leftHand;    // 왼손

    [SerializeField]
    GameObject rightHand;   // 오른손
    
    private void Start()
    {
        // Kinect 초기화
        InitKinect();

        // Kinect 데이터 가져오기
        Task t = KinectLoop();
    }

    private void Update()
    {
        SetColor();
    }

    // Kinect 초기화
    private void InitKinect()
    {
        // 0번째 Kinect와 연결
        kinect = Device.Open(0);
        kinect.StartCameras(new DeviceConfiguration
        {
            ColorFormat = ImageFormat.ColorBGRA32,
            ColorResolution = ColorResolution.R720p,
            DepthMode = DepthMode.NFOV_2x2Binned,
            SynchronizedImagesOnly = true,
            CameraFPS = FPS.FPS30
        });

        // 컬러 이미지의 가로 폭, 세로 폭 취득
        int width = kinect.GetCalibration().DepthCameraCalibration.ResolutionWidth;
        int height = kinect.GetCalibration().DepthCameraCalibration.ResolutionHeight;

        // 컬러 텍스쳐 생성
        kinectColorTexture = new Texture2D(width, height);

        // BodyTracking용 트래커 초기화
        tracker = Tracker.Create(kinect.GetCalibration(), TrackerConfiguration.Default);
    }

    private async Task KinectLoop()
    {
        while (true)
        {
            using (Capture capture = await Task.Run(() => this.kinect.GetCapture()).ConfigureAwait(true))
            {
                // BodyTracking 정보 취득
                tracker.EnqueueCapture(capture);
                var frame = tracker.PopResult();
                if (frame.NumberOfBodies > 0)   // 사람을 인식하면 
                {
                    this.SetMarkPos(this.leftHand, JointId.HandLeft, frame); // 왼손
                    this.SetMarkPos(this.rightHand, JointId.HandRight, frame); // 오른손
                }
            }
        }
    }

    // 컬러 텍스쳐 적용
    private void SetColor()
    {
        Capture capture = kinect.GetCapture();

        // 컬러 이미지 얻음
        Image colorImg = capture.Color;

        // 픽셀 값을 얻음
        Color32[] pixels = colorImg.GetPixels<Color32>().ToArray();

        // 픽셀값 적용
        kinectColorTexture.SetPixels32(pixels);
        kinectColorTexture.Apply();

        // 컬러 텍스쳐 적용
        rawColorImg.texture = kinectColorTexture;
    }

    // 이펙트 프리팹 지정
    private void SetMarkPos(GameObject effectPrefab, Jointld jointId, Frame frame)
    {
        // 설정한 뼈대에 이펙트 프리팹 위치 지정
        var joint = frame.GetBodySkeleton(0).GetJoint(jointId);
        var offset = 50;    // 적당한 오프셋 지정
        var pos = new Vector3(joint.Position.X / -offset, joint.Position.Y / -offset,
                                joint.Position.Z / offset);
        effectPrefab.transform.localPosition = pos;
    }
    private void OnDestroy()
    {
        // Kinect 정지
        kinect.StopCameras();
    }
}
