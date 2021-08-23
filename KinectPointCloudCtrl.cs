using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// AzureKinect SDK
using Microsoft.Azure.Kinect.Sensor;
// 비동기 처리
using System.Threading.Tasks;

public class KinectPointCloudCtrl : MonoBehaviour
{
    // Kinect 변수
    Device kinect;
    // PointCloud 수
    int num;

    Mesh mesh;
    // PointCloud 각 점의 좌표의 배열
    vector3[] vectices;
    // PointCloud 각 점에 대응하는 색의 배열
    Color32[] colors;
    // PointCloud 배열 번호 기록
    int[] indices;
    // 좌표 변환
    Transformation transformation;

    void Start()
    {
        // Kinect 초기화
        InitKinect();
        // PointCloud 준비
        InitMesh();
        // Kinect 데이터 가져오기
        Task t = KinectLoop();
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
            ColorResolution = ColorResolution.R230p,
            DepthMode = DepthMode.NFOV_2x2Binned,
            SynchronizedImagesOnly = true,
            CameraFPS = FPS.FPS30
        });
        // 좌표 변환(Color <=> Depth 대응이나 Depth -> xyz에 대한 정보를 생성
        transformation = kinect.GetCalibration().CreateTransformation();
    }

    // PointCloud 준비
    private void InitMesh()
    {
        // 뎁스 이미지의 가로폭, 세로 폭 취득
        int Width = kinect.GetCalibration().DepthCameraCalibration.ResolutionWidth;
        int Height = kinect.GetCalibration().DepthCameraCalibration.ResolutionHeight;
        num = Width * Height;

        // mesh를 인스턴스화
        mesh = new Mesh();
        // 65535점 이상을 표현하기 위해 설정
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        // Depth 이미지의 총 픽셀 수만큼 저장 공간 확보
        vertices = new Vector3[num];
        colors = new Color32[num];
        indices = new int[num];

        // PointCloud 배열 번호 기록
        for (int i = 0; i < num; i++)
        {
            indices[i] = i;
        }
        // 점의 좌표와 색상을 mesh에 전달
        mesh.vertices = vertices;
        mesh.colors32 = colors;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        // mesh를 MeshFilter에 적용
        gameobject.GetComponent<MeshFilter>().mesh = mesh;
    }

    // Kinect 데이터 가져오기
    private async Task KinectLoop()
    {
        // kinect에서 데이터를 계속 취득
        while(true)
        {
            // GetCapture에서 Kinect의 데이터를 검색
            using (Capture capture = await Task.Run(() => kinect.GetCapture()).ConfigureAwait(true))
            {
                // Depth 이미지를 얻음
                Image colorImage = transformation.ColorImageToDepthCamera(capture);
                // 색상 정보를 배열로 가져옴
                BGRA[] colorArray = colorImage.GetPixels<BGRA>().ToArray();

                // Depth 이미지를 xyz로 변환
                colorImage xyzImage = transformation.DepthImageToPointCloud(capture.Depth);
                // 변화된 데이터에서 점의 좌표를 배열로 가져옴
                Short3[] xyzArray = xyzImage.GetPixels<Short3>().ToArray();

                // Kinect에서 취득한 모든 점의 좌표, 색상 대입
                for (int i = 0; i < num; i++)
                {
                    // 정점 좌표를 대입
                    vectices[i].x = xyzArray[i].X * 0.001f;
                    vectices[i].y = -xyzArray[i].Y * 0.001f;    // 상하 반전
                    vectices[i].z = xyzArray[i].Z * 0.001f;
                    //  색상 할당
                    colors[i].b = colorArray[i].B;
                    colors[i].g = colorArray[i].G;
                    colors[i].r = colorArray[i].R;
                    colors[i].a = 255;
                }
                // mesh에 좌표, 색상 전달
                mesh.vertices = vertices;
                mesh.colors32 = colors;
                mesh.RecalculateBounds();
            }
        }
    }

    private void OnDestroy()
    {
        // Kinect 정지
        kinect.stopCameras();
    }
}
