using UnityEngine;

namespace Game.Scripts.Combat.Props
{
    public static class NetPropFactory
    {
        private const float MinColliderSize = 0.02f;
        private const float Density = 25f;
        private const float MinMass = 0.3f;
        private const float MaxMass = 10f;
        private const float LinearDamping = 0.1f;
        private const float AngularDamping = 0.3f;

        public static NetProp GetOrCreate(GameObject target)
        {
            if (!target.TryGetComponent(out NetProp prop)) prop = Create(target);
            if (!target.TryGetComponent(out NetPropImpact _)) target.AddComponent<NetPropImpact>();
            return prop;
        }

        private static NetProp Create(GameObject target)
        {
            foreach (var collider in target.GetComponentsInChildren<Collider>())
                collider.enabled = false;

            var bounds = CalculateLocalBounds(target.transform);
            var box = target.AddComponent<BoxCollider>();
            box.center = bounds.center;
            box.size = Vector3.Max(bounds.size, Vector3.one * MinColliderSize);

            var worldSize = Vector3.Scale(box.size, target.transform.lossyScale);
            if (!target.TryGetComponent(out Rigidbody body)) body = target.AddComponent<Rigidbody>();
            body.mass = Mathf.Clamp(Mathf.Abs(worldSize.x * worldSize.y * worldSize.z) * Density, MinMass, MaxMass);
            body.linearDamping = LinearDamping;
            body.angularDamping = AngularDamping;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.isKinematic = true;

            return target.AddComponent<NetProp>();
        }

        private static Bounds CalculateLocalBounds(Transform root)
        {
            var bounds = new Bounds();
            var initialized = false;

            foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = meshFilter.sharedMesh;
                if (mesh == null) continue;

                var meshBounds = mesh.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var sign = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
                    var corner = meshBounds.center + Vector3.Scale(meshBounds.extents, sign);
                    var local = root.InverseTransformPoint(meshFilter.transform.TransformPoint(corner));

                    if (initialized)
                    {
                        bounds.Encapsulate(local);
                    }
                    else
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        initialized = true;
                    }
                }
            }

            return bounds;
        }
    }
}
