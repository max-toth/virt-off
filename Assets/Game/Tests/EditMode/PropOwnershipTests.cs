using Game.Scripts.Combat.Props;
using Game.Scripts.Combat.Props.Models;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class PropOwnershipTests
    {
        private PropOwnership _ownership;

        [SetUp]
        public void SetUp() => _ownership = new PropOwnership();

        [Test]
        public void StartsFree()
        {
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Free));
            Assert.That(_ownership.IsHeld, Is.False);
            Assert.That(_ownership.HasAuthority, Is.False);
        }

        [Test]
        public void Pickup_ThenGrant_IsHeldWithAuthority()
        {
            _ownership.RequestPickup(0f);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.PickupPending));
            Assert.That(_ownership.IsHeldByUs, Is.True);
            Assert.That(_ownership.HasAuthority, Is.False);

            _ownership.ApplyGrant();

            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Held));
            Assert.That(_ownership.HasAuthority, Is.True);
        }

        [Test]
        public void GrantWithoutPickup_StartsSimulating()
        {
            _ownership.ApplyGrant();

            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Simulating));
            Assert.That(_ownership.IsHeld, Is.False);
            Assert.That(_ownership.HasAuthority, Is.True);
        }

        [Test]
        public void GrantToOther_WhilePending_RejectsPickup()
        {
            _ownership.RequestPickup(0f);

            Assert.That(_ownership.ApplyGrantToOther(), Is.True);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.HeldByOther));
            Assert.That(_ownership.IsHeld, Is.True);
            Assert.That(_ownership.IsHeldByUs, Is.False);
        }

        [Test]
        public void GrantToOther_WhileHeld_TakesPropAway()
        {
            _ownership.RequestPickup(0f);
            _ownership.ApplyGrant();

            Assert.That(_ownership.ApplyGrantToOther(), Is.False);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.HeldByOther));
            Assert.That(_ownership.HasAuthority, Is.False);
        }

        [Test]
        public void GrantTimeout_CancelsPickup()
        {
            _ownership.RequestPickup(1f);

            Assert.That(_ownership.CheckGrantTimeout(1f + PropOwnership.GrantTimeout), Is.False);
            Assert.That(_ownership.CheckGrantTimeout(1f + PropOwnership.GrantTimeout + 0.01f), Is.True);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Free));
        }

        [Test]
        public void GrantTimeout_AfterGrant_DoesNothing()
        {
            _ownership.RequestPickup(0f);
            _ownership.ApplyGrant();

            Assert.That(_ownership.CheckGrantTimeout(10f), Is.False);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Held));
        }

        [Test]
        public void Release_ReturnsToFree()
        {
            _ownership.RequestPickup(0f);
            _ownership.ApplyGrant();

            _ownership.Release();

            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Free));
        }

        [Test]
        public void ReleaseEvent_OfPreviousOwner_IsIgnoredWhileWeHold()
        {
            _ownership.RequestPickup(0f);

            Assert.That(_ownership.ApplyRelease(releasedByUs: false), Is.False);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.PickupPending));
        }

        [Test]
        public void ReleaseEvent_OfOther_FreesProp()
        {
            _ownership.ApplySnapshot(held: true);

            Assert.That(_ownership.ApplyRelease(releasedByUs: false), Is.True);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Free));
        }

        [Test]
        public void ReleaseEvent_Ours_FreesProp()
        {
            _ownership.ApplyGrant();

            Assert.That(_ownership.ApplyRelease(releasedByUs: true), Is.True);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Free));
        }

        [TestCase(true, PropOwnershipState.HeldByOther)]
        [TestCase(false, PropOwnershipState.Free)]
        public void Snapshot_SetsState(bool held, PropOwnershipState expected)
        {
            _ownership.RequestPickup(0f);

            _ownership.ApplySnapshot(held);

            Assert.That(_ownership.State, Is.EqualTo(expected));
        }

        [TestCase(true, PropOwnershipState.HeldByOther)]
        [TestCase(false, PropOwnershipState.Free)]
        public void RemoteState_SetsState(bool held, PropOwnershipState expected)
        {
            _ownership.ApplyGrant();

            Assert.That(_ownership.ApplyRemoteState(held), Is.True);
            Assert.That(_ownership.State, Is.EqualTo(expected));
        }

        [Test]
        public void RemoteState_IsRejectedWhileWeHold()
        {
            _ownership.RequestPickup(0f);
            _ownership.ApplyGrant();

            Assert.That(_ownership.ApplyRemoteState(true), Is.False);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Held));
        }
    }
}
