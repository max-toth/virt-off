using UnityEngine;

namespace Game.Scripts.Combat.Props
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerPropPusher : MonoBehaviour
    {
        private const float MinPushDirectionY = -0.3f;

        [Tooltip("Во сколько раз быстрее своей скорости игрок толкает предметы, в которые упирается")]
        [SerializeField] private float pushPower = 1.2f;

        private CharacterController _controller;

        private void Awake() => _controller = GetComponent<CharacterController>();

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            var body = hit.rigidbody;
            if (body == null || hit.moveDirection.y < MinPushDirectionY) return;

            if (NetProp.TryFind(hit.collider, out var prop))
            {
                if (prop.IsHeld) return;
                prop.TakeOwnership(NetPropSender.LocalId);
            }

            if (body.isKinematic) return;

            var direction = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z).normalized;
            var ownVelocity = _controller.velocity;
            var speed = new Vector2(ownVelocity.x, ownVelocity.z).magnitude * pushPower;
            var along = Vector3.Dot(body.linearVelocity, direction);
            if (along < speed) body.AddForceAtPosition(direction * (speed - along), hit.point, ForceMode.VelocityChange);
        }
    }
}
