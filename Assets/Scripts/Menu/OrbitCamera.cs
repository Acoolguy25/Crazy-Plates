using UnityEngine;

public class OrbitCamera : MonoBehaviour {
    [Header("Orbit Settings")]
    public Transform center;                  // Orbit around this (if null, uses centerPosition)
    public Vector3 centerPosition;
    public float radius = 2f;
    public float degreesPerSecond = 60f;

    [Header("Tilt")]
    public Vector3 planeNormal = new Vector3(0.3f, 1f, 0f); // The tilt of the orbit plane
    public Vector3 initialDirection = Vector3.right;        // Starting direction in the plane

    [Header("Look")]
    public bool lookAtCenter = true;
    public Transform lookAtTarget; // Optional override target

    private float angleDeg;
    private Vector3 u, v, n;

    void Start() {
        //if (center == null) centerPosition = transform.position;

        // Normalize plane normal
        n = planeNormal.normalized;

        // Build orthonormal basis (u,v) in the plane
        u = Vector3.ProjectOnPlane(initialDirection, n);
        if (u.sqrMagnitude < 1e-6f) {
            // fallback if initialDirection is parallel to normal
            u = Vector3.Cross(n, Vector3.forward);
            if (u.sqrMagnitude < 1e-6f) u = Vector3.Cross(n, Vector3.right);
        }
        u.Normalize();
        v = Vector3.Cross(n, u); // also normalized
    }

    void Update() {
        Vector3 c = center ? center.position : centerPosition;

        // Advance angle
        angleDeg += degreesPerSecond * Time.deltaTime;
        float rad = angleDeg * Mathf.Deg2Rad;

        // Compute orbit offset in tilted plane
        Vector3 offset = (u * Mathf.Cos(rad) + v * Mathf.Sin(rad)) * radius;
        transform.position = c + offset;

        // Look at center or target
        Vector3 target = lookAtTarget ? lookAtTarget.position : (lookAtCenter ? c : transform.position + transform.forward);
        if (lookAtCenter || lookAtTarget)
            transform.rotation = Quaternion.LookRotation((target - transform.position).normalized, n);
    }
}
