using UnityEngine;
using UnityEngine.Assertions;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init() {
        Instance = null;
    }
    public Camera mainCamera { get; private set; }
    public Camera getCamera(string cameraName) {
        cameraName += "Camera";
        var newCamTransform = transform.Find(cameraName);
        Assert.IsTrue(newCamTransform != null, "Camera transform not found");
        var camera = newCamTransform.GetComponent<Camera>();
        Assert.IsTrue(camera, "Could not find camera: " + cameraName);
        return camera;
    }
    public void SetActiveCamera(Camera newCamera) {
        Assert.IsNotNull(newCamera, "Active Camera cannot be null");
        Assert.IsTrue(newCamera.transform.parent == transform, "Cameras must be parented to GameObject.Cameras");
        Assert.IsTrue(!mainCamera || newCamera.tag == mainCamera.tag, "Tags between cameras must be the same");
        foreach (var cam in GetComponentsInChildren<Camera>(true)) {
            cam.gameObject.SetActive(cam == newCamera);
            if (mainCamera)
                cam.transform.position = mainCamera.transform.position;
        }
        mainCamera = newCamera;
    }
    public void SetActiveCamera(string newCameraName) {
        SetActiveCamera(getCamera(newCameraName));
    }
    public void SetCameraTarget(Transform target) {
        foreach (var cinemachine in GetComponentsInChildren<CinemachineCamera>(true)) {
            cinemachine.LookAt = cinemachine.Follow = target;
        }
    }
    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            Destroy(this);
            return;
        }
        Assert.IsTrue(Instance == null, "CameraController already initalized!");
        Instance = this;

    }

    private void Start() {
        DontDestroyOnLoad(this);   
        SetActiveCamera("Orbit");
    }
}
