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

            Assert.That(_ownership.ApplyRelease(releasedByUs: false, isFree: false), Is.False);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.PickupPending));
        }

        [Test]
        public void ReleaseEvent_OfOther_FreesProp()
        {
            _ownership.ApplySnapshot(held: true);

            Assert.That(_ownership.ApplyRelease(releasedByUs: false, isFree: false), Is.True);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Free));
        }

        [Test]
        public void ReleaseEvent_EchoOfOurThrow_KeepsSimulating()
        {
            _ownership.RequestPickup(0f);
            _ownership.ApplyGrant();
            _ownership.Throw();

            Assert.That(_ownership.ApplyRelease(releasedByUs: true, isFree: false), Is.False);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Simulating));
            Assert.That(_ownership.HasAuthority, Is.True);
        }

        [Test]
        public void ReleaseEvent_FreedByServer_FreesOurProp()
        {
            _ownership.ApplyGrant();

            Assert.That(_ownership.ApplyRelease(releasedByUs: true, isFree: true), Is.True);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Free));
        }

        [Test]
        public void Throw_AfterGrant_KeepsAuthorityUntilSettled()
        {
            _ownership.RequestPickup(0f);
            _ownership.ApplyGrant();

            _ownership.Throw();

            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Simulating));
            Assert.That(_ownership.IsHeld, Is.False);
            Assert.That(_ownership.HasAuthority, Is.True);
        }

        [Test]
        public void Throw_BeforeGrant_LateGrantStartsSimulating()
        {
            _ownership.RequestPickup(0f);
            _ownership.Throw();
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Free));

            _ownership.ApplyGrant();

            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Simulating));
        }

        [Test]
        public void Push_RequestsSimulationOnce()
        {
            Assert.That(_ownership.RequestSimulation(0f), Is.True);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.SimulationPending));
            Assert.That(_ownership.RequestSimulation(0.1f), Is.False);

            _ownership.ApplyGrant();

            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Simulating));
            Assert.That(_ownership.IsHeld, Is.False);
        }

        [Test]
        public void Push_OfHeldProp_IsNotRequested()
        {
            _ownership.ApplySnapshot(held: true);

            Assert.That(_ownership.RequestSimulation(0f), Is.False);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.HeldByOther));
        }

        [Test]
        public void PushTimeout_FreesWithoutRollback()
        {
            _ownership.RequestSimulation(1f);

            Assert.That(_ownership.CheckGrantTimeout(1f + PropOwnership.GrantTimeout + 0.01f), Is.False);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Free));
            Assert.That(_ownership.RequestSimulation(2f), Is.True);
        }

        [Test]
        public void Pickup_OfOwnThrownProp_IsAllowed()
        {
            _ownership.RequestPickup(0f);
            _ownership.ApplyGrant();
            _ownership.Throw();

            _ownership.RequestPickup(1f);
            _ownership.ApplyGrant();

            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Held));
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
            _ownership.ApplySnapshot(!held);

            Assert.That(_ownership.ApplyRemoteState(held), Is.True);
            Assert.That(_ownership.State, Is.EqualTo(expected));
        }

        [Test]
        public void RemoteState_IsRejectedWhileWeSimulate()
        {
            _ownership.RequestSimulation(0f);
            _ownership.ApplyGrant();

            Assert.That(_ownership.ApplyRemoteState(false), Is.False);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Simulating));
        }

        [Test]
        public void RemoteState_WhilePushPending_GivesPropToOwner()
        {
            _ownership.RequestSimulation(0f);

            Assert.That(_ownership.ApplyRemoteState(false), Is.True);
            Assert.That(_ownership.State, Is.EqualTo(PropOwnershipState.Free));
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
