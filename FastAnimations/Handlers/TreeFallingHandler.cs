using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Common;
using Pathoschild.Stardew.FastAnimations.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Pathoschild.Stardew.FastAnimations.Handlers
{
    /// <summary>Handles the falling-tree animation.</summary>
    /// <remarks>See game logic in <see cref="Tree.tickUpdate"/>.</remarks>
    internal class TreeFallingHandler : BaseAnimationHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The trees in the current location.</summary>
        private Dictionary<Vector2, TerrainFeature> Trees = new();


        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
        public TreeFallingHandler(float multiplier)
            : base(multiplier) { }

        /// <inheritdoc />
        public override void OnNewLocation(GameLocation location)
        {
            this.Trees =
                (
                    from pair in location.terrainFeatures.FieldDict
                    let tree = pair.Value.Value as Tree
                    let fruitTree = pair.Value.Value as FruitTree
                    where
                        (
                            tree != null
                            && !tree.stump.Value
                            && tree.growthStage.Value > Tree.bushStage
                        )
                        || (
                            fruitTree != null
                            && !fruitTree.stump.Value
                            && fruitTree.growthStage.Value > FruitTree.bushStage
                        )
                    select pair
                )
                .ToDictionary(p => p.Key, p => p.Value.Value);
        }

        /// <inheritdoc />
        public override bool IsEnabled(int playerAnimationID)
        {
            return
                Context.IsWorldReady
                && this.GetFallingTrees().Any();
        }

        /// <inheritdoc />
        public override void Update(int playerAnimationID)
        {
            GameTime gameTime = Game1.currentGameTime;

            int skips = this.GetSkipsThisTick();
            foreach (TerrainFeature tree in this.GetFallingTrees())
                this.ApplySkips(skips, () => tree.tickUpdate(gameTime), until: () => tree.Location is null);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get all trees in the current location which are currently falling.</summary>
        private IEnumerable<TerrainFeature> GetFallingTrees()
        {
            Rectangle visibleTiles = TileHelper.GetVisibleArea();
            foreach (KeyValuePair<Vector2, TerrainFeature> pair in this.Trees)
            {
                if (visibleTiles.Contains((int)pair.Key.X, (int)pair.Key.Y))
                {
                    bool isFalling = pair.Value switch
                    {
                        Tree tree => tree.falling.Value,
                        FruitTree tree => tree.falling.Value,
                        _ => false
                    };

                    if (isFalling && pair.Value.Location != null)
                        yield return pair.Value;
                }
            }
        }
    }
}
