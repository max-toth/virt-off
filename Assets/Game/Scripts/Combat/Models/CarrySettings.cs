using System;
using UnityEngine;

namespace Game.Scripts.Combat.Models
{
    [Serializable]
    public sealed class CarrySettings
    {
        [SerializeField] private float pickupDistance = 2.5f;
        [SerializeField] private float holdDistance = 1.3f;
        [SerializeField] private float holdHeightOffset = 0.35f;
        [SerializeField] private float throwSpeed = 13f;
        [SerializeField] private float throwSpin = 6f;

        public float PickupDistance => pickupDistance;
        public float HoldDistance => holdDistance;
        public float HoldHeightOffset => holdHeightOffset;
        public float ThrowSpeed => throwSpeed;
        public float ThrowSpin => throwSpin;
    }
}
