using System.Collections.Generic;
using UnityEngine;

// Метка на модели чужого игрока: по ней удары и брошенные предметы понимают, в кого попали
public class RemoteAvatar : MonoBehaviour
{
    private static readonly Dictionary<Collider, RemoteAvatar> ByCollider = new();

    public string Id;

    private Collider[] _colliders;

    public static bool TryFind(Collider collider, out RemoteAvatar avatar) => ByCollider.TryGetValue(collider, out avatar);

    // Капсула (рост ~1.8 м) и кинематический Rigidbody, чтобы в игрока можно было попасть и предметы от него отскакивали
    public static RemoteAvatar Attach(GameObject go, string id)
    {
        var avatar = go.AddComponent<RemoteAvatar>();
        avatar.Id = id;

        float scale = Mathf.Max(0.01f, go.transform.lossyScale.y);
        var capsule = go.AddComponent<CapsuleCollider>();
        capsule.height = 1.8f / scale;
        capsule.radius = 0.35f / scale;
        capsule.center = new Vector3(0, 0.85f / scale, 0);

        var rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;

        avatar.RegisterColliders();
        return avatar;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState() => ByCollider.Clear();

    private void OnDestroy()
    {
        if (_colliders == null) return;
        foreach (var own in _colliders) ByCollider.Remove(own);
    }

    private void RegisterColliders()
    {
        _colliders = GetComponentsInChildren<Collider>(true);
        foreach (var own in _colliders) ByCollider[own] = this;
    }
}
