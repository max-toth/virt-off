using UnityEngine;

// Метка на модели чужого игрока: по ней удары и брошенные предметы понимают, в кого попали
public class RemoteAvatar : MonoBehaviour
{
    public string Id;

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
        return avatar;
    }
}
