using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// AzureKinect SDK
using Microsoft.Azure.Kinect.Sensor;

public class KinectCtrl : MonoBehaviour
{
    // Kinect 변수
    Device kinect;

    // 컬러 텍스쳐
    Texture2D kinectColorTexture;

    [SerializeField]
    UnityEngine.UI.RawImage rawColorImg;

    private void Start()
    {
        InitKinect();
    }

    // Kinect 초기화
     private void InitKinect()
    {
        // 0번째 Kinect와 연결
        kinect = Device.Open(0);

        // Kinect 모드 설정
        kinect.StartCameras(new DeviceConfiguration
        {
            ColorFormat = ImageFormat.ColorBGRA32,
            ColorResolution = ColorResolution.R720p,
            DepthMode = DepthMode.NFOV_2x2Binned,
            SynchronizedImagesOnly = true,
            CameraFPS = FPS.FPS30
        });

        // 컬러 이미지의 가로 폭, 세로 폭
        int width = kinect.GetCalibration().ColorCameraCalibration.ResolutionWidth;
        int height = kinect.GetCalibration().ColorCameraCalibration.ResolutionHeight;

        // 컬러 텍스쳐 생성
        kinectColorTexture = new Texture2D(width, height);
    }

    void Update()
    {
        Capture capture = kinect.GetCapture();

        // 컬러 이미지를 얻음
        Image colorImg = capture.Color;

        // 픽셀 값을 얻음
        Color32[] pixels = colorImg.GetPixels<Color32>().ToArray();
        for (int i = 0; i < pixels.Length; i++)
        {
            var d = pixels[i].b;
            var k = pixels[i].r;
            pixels[i].r = d;
            pixels[i].b = k;
        }

        // 픽셀 값 적용
        kinectColorTexture.SetPixels32(pixels);
        kinectColorTexture.Apply();

        // 컬러 텍스처 적용
        rawColorImg.texture = kinectColorTexture;
    }

    private void OnDestroy()
    {
        // Kinect 정지
        kinect.StopCameras();
    }
}
