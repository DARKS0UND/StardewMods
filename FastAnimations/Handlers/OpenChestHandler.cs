using System.Collections.Generic;
using System.Linq;
using Pathoschild.Stardew.FastAnimations.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace Pathoschild.Stardew.FastAnimations.Handlers
{
    /// <summary>Handles the chest-open animation.</summary>
    /// <remarks>See game logic in <see cref="Chest.checkForAction"/>.</remarks>
    internal class OpenChestHandler : BaseAnimationHandler, IAnimationHandlerWithObjectList
    {
        /*********
        ** Fields
        *********/
        /// <summary>The chests in the current location.</summary>
        private readonly List<Chest> Chests = new();


        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public OpenChestHandler(float multiplier)
            : base(multiplier) { }

        /// <inheritdoc />
        public override bool IsEnabled(int playerAnimationID)
        {
            return this.GetOpeningChest() != null;
        }

        /// <inheritdoc />
        public override void OnNewLocation(GameLocation location)
        {
            this.UpdateChestCache(location);
        }

        /// <inheritdoc />
        public void OnObjectListChanged(ObjectListChangedEventArgs e)
        {
            this.UpdateChestCache(e.Location);
        }

        /// <inheritdoc />
        public override void Update(int playerAnimationID)
        {
            Chest? chest = this.GetOpeningChest();

            this.ApplySkips(
                run: () => chest?.updateWhenCurrentLocation(Game1.currentGameTime),
                until: () => !this.IsOpening(chest)
            );
        }


        /*********
         ** Private methods
         *********/
        /// <summary>Get the chest in the current location which is currently opening.</summary>
        private Chest? GetOpeningChest()
        {
            foreach (Chest chest in this.Chests)
            {
                if (this.IsOpening(chest))
                    return chest;
            }

            return null;
        }

        /// <summary>Get whether a chest is opening.</summary>
        /// <param name="chest">The chest to check.</param>
        private bool IsOpening(Chest? chest)
        {
            return chest?.frameCounter.Value > -1;
        }

        /// <summary>Update the cached list of chests in the current location.</summary>
        /// <param name="location">The location to check.</param>
        private void UpdateChestCache(GameLocation location)
        {
            this.Chests.Clear();
            this.Chests.AddRange(
                location.objects.Values.OfType<Chest>()
            );
        }
    }
}
