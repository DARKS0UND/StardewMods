using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Common;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using Pathoschild.Stardew.LookupAnything.Framework.Lookups;
using Pathoschild.Stardew.LookupAnything.Framework.Lookups.Buildings;
using Pathoschild.Stardew.LookupAnything.Framework.Lookups.Characters;
using Pathoschild.Stardew.LookupAnything.Framework.Lookups.Items;
using Pathoschild.Stardew.LookupAnything.Framework.Lookups.TerrainFeatures;
using Pathoschild.Stardew.LookupAnything.Framework.Lookups.Tiles;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Pathoschild.Stardew.LookupAnything.Framework
{
    /// <summary>Finds and analyzes lookup targets in the world.</summary>
    internal class TargetFactory : ISubjectRegistry
    {
        /*********
        ** Fields
        *********/
        /// <summary>Provides utility methods for interacting with the game code.</summary>
        private readonly GameHelper GameHelper;

        /// <summary>The instances which provides lookup data for in-game entities.</summary>
        private readonly ILookupProvider[] LookupProviders;


        /*********
        ** Public methods
        *********/
        /****
        ** Constructors
        ****/
        /// <summary>Construct an instance.</summary>
        /// <param name="reflection">Simplifies access to private game code.</param>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="config">The mod configuration.</param>
        /// <param name="showRawTileInfo">Whether to show raw tile info like tilesheets and tile indexes.</param>
        public TargetFactory(IReflectionHelper reflection, GameHelper gameHelper, Func<ModConfig> config, Func<bool> showRawTileInfo)
        {
            this.GameHelper = gameHelper;

            ISubjectRegistry codex = this;
            this.LookupProviders = new ILookupProvider[]
            {
                new BuildingLookupProvider(reflection, gameHelper, config, codex),
                new CharacterLookupProvider(reflection, gameHelper, config, codex),
                new ItemLookupProvider(reflection, gameHelper, config, codex),
                new TerrainFeatureLookupProvider(reflection, gameHelper, codex),
                new TileLookupProvider(reflection, gameHelper, config, showRawTileInfo)
            };
        }

        /****
        ** Targets
        ****/
        /// <summary>Get all potential lookup targets in the current location.</summary>
        /// <param name="location">The current location.</param>
        /// <param name="originTile">The tile from which to search for targets.</param>
        public IEnumerable<ITarget> GetNearbyTargets(GameLocation location, Vector2 originTile)
        {
            var targets = this.LookupProviders
                .SelectMany(p => p.GetTargets(location, originTile))
                .WhereNotNull();

            foreach (ITarget target in targets)
                yield return target;
        }

        /// <summary>Get the target on the specified tile.</summary>
        /// <param name="location">The current location.</param>
        /// <param name="tile">The tile to search.</param>
        public ITarget? GetTargetFromTile(GameLocation location, Vector2 tile)
        {
            return (
                from target in this.GetNearbyTargets(location, tile)
                where target.Tile == tile
                select target
            ).FirstOrDefault();
        }

        /// <summary>Get the target at the specified coordinate.</summary>
        /// <param name="location">The current location.</param>
        /// <param name="tile">The tile to search.</param>
        /// <param name="position">The viewport-relative pixel coordinate to search.</param>
        public ITarget? GetTargetFromScreenCoordinate(GameLocation location, Vector2 tile, Vector2 position)
        {
            // get target sprites which might overlap cursor position (first approximation)
            Rectangle tileArea = this.GameHelper.GetScreenCoordinatesFromTile(tile);
            var candidates = (
                from target in this.GetNearbyTargets(location, tile)
                let spriteArea = target.GetWorldArea()
                let isAtTile = target.Tile == tile
                where (isAtTile || spriteArea.Intersects(tileArea))
                orderby
                    target.Type != SubjectType.Tile ? 0 : 1, // Tiles are always under anything else.
                    spriteArea.Y descending,                 // A higher Y value is closer to the foreground, and will occlude any sprites behind it.
                    spriteArea.X ascending                   // If two sprites at the same Y coordinate overlap, assume the left sprite occludes the right.

                select new { target, spriteArea, isAtTile }
            ).ToArray();

            // choose best match
            {
                ITarget? fallback = null;

                // sprite pixel under cursor
                foreach (var candidate in candidates)
                {
                    try
                    {
                        if (candidate.target.SpriteIntersectsPixel(tile, position, candidate.spriteArea))
                            return candidate.target;
                    }
                    catch
                    {
                        // if the sprite check fails (e.g. due to an invalid texture), select this target if we don't
                        // find a more specific one (since it did pass the world area check above)
                        fallback ??= candidate.target;
                    }
                }

                // tile under cursor
                foreach (var candidate in candidates)
                {
                    if (candidate.isAtTile)
                        return candidate.target;
                }

                // fallback
                return fallback;
            }
        }

        /****
        ** Subjects
        ****/
        /// <summary>Get metadata for a Stardew object at the specified position.</summary>
        /// <param name="player">The player performing the lookup.</param>
        /// <param name="location">The current location.</param>
        /// <param name="hasCursor">Whether the player has a visible cursor.</param>
        public ISubject? GetSubjectFrom(Farmer player, GameLocation location, bool hasCursor)
        {
            ITarget? target = hasCursor
                ? this.GetTargetFromScreenCoordinate(location, Game1.currentCursorTile, this.GameHelper.GetScreenCoordinatesFromCursor())
                : this.GetTargetFromTile(location, this.GetFacingTile(player));

            return target?.GetSubject();
        }

        /// <summary>Get metadata for a menu element at the specified position.</summary>
        /// <param name="menu">The active menu.</param>
        /// <param name="cursorPos">The cursor's viewport-relative coordinates.</param>
        public ISubject? GetSubjectFrom(IClickableMenu menu, Vector2 cursorPos)
        {
            int cursorX = (int)cursorPos.X;
            int cursorY = (int)cursorPos.Y;

            return this.LookupProviders
                .Select(p => p.GetSubject(menu, cursorX, cursorY))
                .FirstOrDefault(p => p != null);
        }

        /// <summary>Get the subject for an in-game entity.</summary>
        /// <param name="entity">The entity instance.</param>
        /// <param name="location">The location containing the entity, if applicable.</param>
        public ISubject? GetByEntity(object entity, GameLocation? location)
        {
            return this.LookupProviders
                .Select(p => p.GetSubjectFor(entity, location))
                .FirstOrDefault(p => p != null);
        }

        /// <summary>Get all known subjects for the search UI.</summary>
        public IEnumerable<ISubject> GetSearchSubjects()
        {
            return this.LookupProviders
                .SelectMany(p => p.GetSearchSubjects());
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the tile the player is facing.</summary>
        /// <param name="player">The player to check.</param>
        private Vector2 GetFacingTile(Farmer player)
        {
            Vector2 tile = player.Tile;
            FacingDirection direction = (FacingDirection)player.FacingDirection;
            return direction switch
            {
                FacingDirection.Up => tile + new Vector2(0, -1),
                FacingDirection.Right => tile + new Vector2(1, 0),
                FacingDirection.Down => tile + new Vector2(0, 1),
                FacingDirection.Left => tile + new Vector2(-1, 0),
                _ => throw new NotSupportedException($"Unknown facing direction {direction}")
            };
        }
    }
}
