using Pathoschild.Stardew.FastAnimations.Framework;
using StardewValley;
using StardewValley.Minigames;

namespace Pathoschild.Stardew.FastAnimations.Handlers
{
    /// <summary>Handles the casino slots minigame spin animation.</summary>
    /// <remarks>See game logic in <see cref="Slots"/>.</remarks>
    internal class CasinoSlotsHandler : BaseAnimationHandler
    {
        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public CasinoSlotsHandler(float multiplier)
            : base(multiplier) { }

        /// <inheritdoc />
        public override bool IsEnabled(int playerAnimationID)
        {
            return Game1.currentMinigame is Slots { spinning: true };
        }

        /// <inheritdoc />
        public override void Update(int playerAnimationID)
        {
            Slots minigame = (Slots)Game1.currentMinigame;

            this.ApplySkips(
                run: () => minigame.tick(Game1.currentGameTime),
                until: () => !minigame.spinning
            );
        }
    }
}
