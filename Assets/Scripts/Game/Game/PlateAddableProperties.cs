using UnityEngine;
using static Plate.PlateAddable;

public class PlateAddableProperties : MonoBehaviour {
    public Vector3 scalePosition_min = Vector3.up / 2;
    public Vector3 scalePosition_max = Vector3.up / 2;
    public Vector3 offsetPosition_min = Vector3.zero;
    public Vector3 offsetPosition_max = Vector3.zero;
    public PlateAddableType type;

    private Vector3 _scalePosition;
    private bool _scalePositionCalculated = false;
    public Vector3 scalePosition {
        get {
            if (!_scalePositionCalculated) {
                _scalePosition = new Vector3(
                    Mathf.Lerp(scalePosition_min.x, scalePosition_max.x, Random.value),
                    Mathf.Lerp(scalePosition_min.y, scalePosition_max.y, Random.value),
                    Mathf.Lerp(scalePosition_min.z, scalePosition_max.z, Random.value)
                );
                _scalePositionCalculated = true;
            }
            return _scalePosition;
        }
        private set => _scalePosition = value;
    }

    private Vector3 _offsetPosition;
    private bool _offsetPositionCalculated = false;
    public Vector3 offsetPosition {
        get {
            if (!_offsetPositionCalculated) {
                _offsetPosition = new Vector3(
                    Mathf.Lerp(offsetPosition_min.x, offsetPosition_max.x, Random.value),
                    Mathf.Lerp(offsetPosition_min.y, offsetPosition_max.y, Random.value),
                    Mathf.Lerp(offsetPosition_min.z, offsetPosition_max.z, Random.value)
                );
                _offsetPositionCalculated = true;
            }
            return _offsetPosition;
        }
        private set => _offsetPosition = value;
    }
}
