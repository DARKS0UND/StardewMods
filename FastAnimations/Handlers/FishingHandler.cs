using Pathoschild.Stardew.FastAnimations.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace Pathoschild.Stardew.FastAnimations.Handlers
{
    /// <summary>Handles the fishing animation.</summary>
    /// <remarks>See game logic in <see cref="StardewValley.Tools.FishingRod.beginUsing"/>.</remarks>
    internal class FishingHandler : BaseAnimationHandler
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public FishingHandler(float multiplier)
            : base(multiplier) { }

        /// <inheritdoc />
        public override bool IsEnabled(int playerAnimationID)
        {
            return
                Context.IsWorldReady
                && Game1.player.UsingTool
                && Game1.player.CurrentTool is FishingRod { isTimingCast: false, isFishing: false };
        }

        /// <inheritdoc />
        public override void Update(int playerAnimationID)
        {
            this.SpeedUpPlayer();
        }
    }
}
