#nullable enable
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Shared.GameObjects.Components.Storage
{
    /// <summary>
    ///    Players can pick up, drop, and put items in bags, and they can be seen in player's hands.
    /// </summary>
    public abstract class SharedItemComponent : Component, IEquipped, IUnequipped, IExAct, IInteractHand
    {
        public override string Name => "Item";

        public override uint? NetID => ContentNetIDs.ITEM;

        /// <summary>
        ///     How much big this item is.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int Size
        {
            get => _size;
            set
            {
                _size = value;
                Dirty();
            }
        }
        private int _size;

        /// <summary>
        ///     Part of the state of the sprite shown on the player when this item is in their hands.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public string? EquippedPrefix
        {
            get => _equippedPrefix;
            set
            {
                _equippedPrefix = value;
                OnEquippedPrefixChange();
                Dirty();
            }
        }
        private string? _equippedPrefix;

        /// <summary>
        ///     Color of the sprite shown on the player when this item is in their hands.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public Color Color
        {
            get => _color;
            protected set
            {
                _color = value;
                Dirty();
            }
        }
        private Color _color;

        /// <summary>
        ///     Rsi of the sprite shown on the player when this item is in their hands.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public string? RsiPath
        {
            get => _rsiPath;
            set
            {
                _rsiPath = value;
                Dirty();
            }
        }
        private string? _rsiPath;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Size, "size", 1);
            serializer.DataField(this, x => x.EquippedPrefix, "HeldPrefix", null);
            serializer.DataField(this, x => x.Color, "color", Color.White);
            serializer.DataField(this, x => x.RsiPath, "sprite", RsiPath);
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new ItemComponentState(Size, EquippedPrefix, Color, RsiPath);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState == null)
                return;

            if (curState is not ItemComponentState state)
                return;

            Size = state.Size;
            EquippedPrefix = state.EquippedPrefix;
            Color = state.Color;
            RsiPath = state.RsiPath;
        }

        /// <summary>
        ///     If a player can pick up this item.
        /// </summary>
        public bool CanPickup(IEntity user)
        {
            if (!ActionBlockerSystem.CanPickup(user))
                return false;

            if (user.Transform.MapID != Owner.Transform.MapID)
                return false;

            if (Owner.TryGetComponent(out IPhysicsComponent? physics) && physics.Anchored)
                return false;

            return user.InRangeUnobstructed(Owner, ignoreInsideBlocker: true, popup: true);
        }

        void IEquipped.Equipped(EquippedEventArgs eventArgs)
        {
            EquippedToSlot();
        }

        void IUnequipped.Unequipped(UnequippedEventArgs eventArgs)
        {
            RemovedFromSlot();
        }

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            var source = eventArgs.Source;
            var target = eventArgs.Target.Transform.Coordinates;

            var throwForce = eventArgs.Severity switch
            {
                ExplosionSeverity.Destruction => 3.0f,
                ExplosionSeverity.Heavy => 2.0f,
                _ => 1.0f,
            };
            ThrowItem(source, target, throwForce);
        }

        bool IInteractHand.InteractHand(InteractHandEventArgs eventArgs)
        {
            return TryPutInHand(eventArgs.User);
        }

        /// <summary>
        ///     Tries to put this item in a player's hands.
        ///     TODO: Move server implementation here once hands are in shared.
        /// </summary>
        public abstract bool TryPutInHand(IEntity user);

        protected virtual void OnEquippedPrefixChange() { }

        public virtual void RemovedFromSlot() { }

        public virtual void EquippedToSlot() { }

        //TODO: Move server implementation here once throwing is in shared
        protected virtual void ThrowItem(EntityCoordinates sourceLocation, EntityCoordinates targetLocation, float throwForce) { }
    }

    [Serializable, NetSerializable]
    public class ItemComponentState : ComponentState
    {
        public int Size { get; }
        public string? EquippedPrefix { get; }
        public Color Color { get; }
        public string? RsiPath { get; }

        public ItemComponentState(int size, string? equippedPrefix, Color color, string? rsiPath) : base(ContentNetIDs.ITEM)
        {
            Size = size;
            EquippedPrefix = equippedPrefix;
            Color = color;
            RsiPath = rsiPath;
        }
    }

    /// <summary>
    ///     Reference sizes for common containers and items.
    /// </summary>
    public enum ReferenceSizes
    {
        Wallet = 4,
        Pocket = 12,
        Box = 24,
        Belt = 30,
        Toolbox = 60,
        Backpack = 100,
        NoStoring = 9999
    }
}
