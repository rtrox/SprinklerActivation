﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace SprinklerActivation
{
    public class SprinklerActivation : Mod
    {
        private ModConfig Config;
        private object BetterSprinklersApi, PrismaticToolsApi, SimpleSprinklerApi;
        private bool LineSprinklersIsLoaded;
        private Multiplayer mp;

        enum animSize
        {
            SMALL,
            MEDIUM,
            LARGE
        }

        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();

            Helper.Events.GameLoop.GameLaunched += this.OnGameLaunch;

            if (Config.ActivateOnAction)
            {
                Helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            }

            if (Config.ActivateOnPlacement)
            {
                Helper.Events.World.ObjectListChanged += this.OnWorld_ObjectListChanged;
            }
            mp = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();
        }
        private void OnGameLaunch(object sender, GameLaunchedEventArgs e)
        {
            if (Helper.ModRegistry.IsLoaded("Speeder.BetterSprinklers"))
            {
                BetterSprinklersApi = Helper.ModRegistry.GetApi("Speeder.BetterSprinklers");
            }

            if (Helper.ModRegistry.IsLoaded("stokastic.PrismaticTools"))
            {
                PrismaticToolsApi = Helper.ModRegistry.GetApi("stokastic.PrismaticTools");
            }

            if(Helper.ModRegistry.IsLoaded("tZed.SimpleSprinkler"))
            {
                SimpleSprinklerApi = Helper.ModRegistry.GetApi("tZed.SimpleSprinkler");
            }

            LineSprinklersIsLoaded = Helper.ModRegistry.IsLoaded("hootless.LineSprinklers");
        }

        private void OnWorld_ObjectListChanged(object sender, ObjectListChangedEventArgs e)
        {
            foreach (var pair in e.Added)
            {
                ActivateSprinkler(pair.Value);
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            // Ignore event if world is not loaded and player is not interacting with the world
            if (!Context.IsPlayerFree)
                return;



            if (e.Button.IsActionButton())
            {
                var tile = e.Cursor.GrabTile;
                if (tile == null) return;

                var obj = Game1.currentLocation.getObjectAtTile((int)tile.X, (int)tile.Y);
                if (obj == null) return;

                ActivateSprinkler(obj);
            }
        }

        private void ActivateSprinkler(StardewValley.Object sprinkler)
        {
            if (sprinkler == null) return;

            if (sprinkler.Name.Contains("Sprinkler"))
            {
                if (LineSprinklersIsLoaded && sprinkler.Name.Contains("Line"))
                {
                    ActivateLineSprinkler(sprinkler);
                }
                else if (PrismaticToolsApi != null && sprinkler.Name.Contains("Prismatic"))
                {
                    ActivatePrismaticSprinkler(sprinkler);
                }
                else if (BetterSprinklersApi != null)
                {
                    ActivateBetterSprinkler(sprinkler);
                }
                else if(SimpleSprinklerApi != null)
                {
                    ActivateSimpleSprinkler(sprinkler);
                }
                else
                {
                    ActivateVanillaSprinkler(sprinkler);
                }
            }
        }

        private void ActivateVanillaSprinkler(StardewValley.Object sprinkler)
        {
            Vector2 sprinklerTile = sprinkler.TileLocation;
            if (sprinkler.Name.Contains("Quality"))
            {
                Vector2[] coverage = Utility.getSurroundingTileLocationsArray(sprinklerTile);
                foreach (Vector2 v in coverage)
                {
                    WaterTile(v);
                }
                playAnimation(sprinklerTile, animSize.MEDIUM);
            }
            else if (sprinkler.Name.Contains("Iridium"))
            {
                for (int i = (int)sprinklerTile.X - 2; (float)i <= sprinklerTile.X + 2f; i++)
                {
                    for (int j = (int)sprinklerTile.Y - 2; (float)j <= sprinklerTile.Y + 2f; j++)
                    {
                        Vector2 v = new Vector2((float)i, (float)j);
                        WaterTile(v);
                    }
                    playAnimation(sprinklerTile, animSize.LARGE);
                }
            }
            else
            {
                foreach (Vector2 v in Utility.getAdjacentTileLocations(sprinklerTile))
                {
                    WaterTile(v);
                }
                playAnimation(sprinklerTile, animSize.SMALL);
            }
        }

        private void ActivateBetterSprinkler(StardewValley.Object sprinkler)
        {
            IDictionary<int, Vector2[]> coverageList = Helper.Reflection.GetMethod(BetterSprinklersApi, "GetSprinklerCoverage").Invoke<IDictionary<int, Vector2[]>>();
            Vector2[] coverage = coverageList[sprinkler.ParentSheetIndex];
            Vector2 sprinklerTile = sprinkler.TileLocation;

            float max = Math.Abs(coverage[0].X);

            foreach (Vector2 v in coverage)
            {
                WaterTile(sprinklerTile + v);
                max = Math.Max(Math.Abs(v.X), max);
                max = Math.Max(Math.Abs(v.Y), max);
            }
            if (max < 2)
                playAnimation(sprinklerTile, animSize.SMALL);
            else if (max < 3)
                playAnimation(sprinklerTile, animSize.MEDIUM);
            else
                playAnimation(sprinklerTile, animSize.LARGE);

        }

        private void ActivateSimpleSprinkler(StardewValley.Object sprinkler)
        {
            IDictionary<int, Vector2[]> coverageList = Helper.Reflection.GetMethod(SimpleSprinklerApi, "GetNewSprinklerCoverage").Invoke<IDictionary<int, Vector2[]>>();
            Vector2[] coverage = coverageList[sprinkler.ParentSheetIndex];
            Vector2 sprinklerTile = sprinkler.TileLocation;

            float max = Math.Abs(coverage[0].X);

            foreach (Vector2 v in coverage)
            {
                WaterTile(sprinklerTile + v);
                max = Math.Max(Math.Abs(v.X), max);
                max = Math.Max(Math.Abs(v.Y), max);
            }
            if (max < 2)
                playAnimation(sprinklerTile, animSize.SMALL);
            else if (max < 3)
                playAnimation(sprinklerTile, animSize.MEDIUM);
            else
                playAnimation(sprinklerTile, animSize.LARGE);
        }


        private void ActivateLineSprinkler(StardewValley.Object sprinkler)
        {
            Vector2 waterTile = sprinkler.TileLocation;
            int range;

            if (sprinkler.Name.Contains("Quality")) range = 8;
            else if (sprinkler.Name.Contains("Iridium")) range = 24;
            else range = 4;

            if (sprinkler.Name.Contains("(U)"))
            {
                for (int i = 0; i < range; i++)
                {
                    waterTile.Y--;
                    WaterTile(waterTile, true);
                }
            }
            else if (sprinkler.Name.Contains("(L)"))
            {
                for (int i = 0; i < range; i++)
                {
                    waterTile.X--;
                    WaterTile(waterTile, true);
                }
            }
            else if (sprinkler.Name.Contains("(R)"))
            {
                for (int i = 0; i < range; i++)
                {
                    waterTile.X++;
                    WaterTile(waterTile, true);
                }
            }
            else if (sprinkler.Name.Contains("(D)"))
            {
                for (int i = 0; i < range; i++)
                {
                    waterTile.Y++;
                    WaterTile(waterTile, true);
                }
            }
        }

        private void ActivatePrismaticSprinkler(StardewValley.Object sprinkler)
        {
            Vector2 sprinklerTile = sprinkler.TileLocation;
            IEnumerable<Vector2> coverage = Helper.Reflection.GetMethod(PrismaticToolsApi, "GetSprinklerCoverage").Invoke<IEnumerable<Vector2>>(sprinklerTile);

            float max = Math.Abs(coverage.First().X);
            foreach (Vector2 v in coverage)
            {
                WaterTile(v);
                max = Math.Max(Math.Abs(v.X), max);
                max = Math.Max(Math.Abs(v.Y), max);
            }
            if (max < 2)
                playAnimation(sprinklerTile, animSize.SMALL);
            else if (max < 3)
                playAnimation(sprinklerTile, animSize.MEDIUM);
            else
                playAnimation(sprinklerTile, animSize.LARGE);
        }

        private void WaterTile(Vector2 tile, bool useWatercanAnimation = false)
        {
            TerrainFeature terrainFeature;
            StardewValley.Object obj;
            WateringCan can = new WateringCan();
            GameLocation loc = Game1.currentLocation;

            loc.terrainFeatures.TryGetValue(tile, out terrainFeature);
            if(terrainFeature != null)
                terrainFeature.performToolAction(can, 0, tile, (GameLocation)null);

            loc.Objects.TryGetValue(tile, out obj);
            if(obj != null)
                obj.performToolAction(can, (GameLocation)null);

            //Watercan animation (only for sline sprinklers, because default animation don't make any sense here
            if (mp != null && useWatercanAnimation)
            {
                mp.broadcastSprites(loc, new TemporaryAnimatedSprite[]
                {
                    new TemporaryAnimatedSprite(13, tile * (float)Game1.tileSize, Color.White, 10, Game1.random.NextDouble() < 0.5, 70f, 0, -1, -1f, -1, 0)
                    {
                        delayBeforeAnimationStart = 150
                    }
                });
            }
        }

        private void playAnimation(Vector2 sprinklerTile, animSize size)
        {
            if (mp == null)
                return;

            int animDelay = Game1.random.Next(500);
            float animId = (float)((double)sprinklerTile.X * 4000.0 + (double)sprinklerTile.Y);
            Vector2 pos = sprinklerTile * (float)Game1.tileSize;
            int numberOfLoops = 50;

            switch (size)
            {
                case animSize.SMALL:
                    mp.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite[]
                    {
                        new TemporaryAnimatedSprite(29, pos + new Vector2(0.0f, (float)(-Game1.tileSize * 3 / 4)), Color.White * 0.5f, 4, false, 60f, numberOfLoops, -1, -1f, -1, 0)
                        {
                            delayBeforeAnimationStart = animDelay,
                            id = animId
                        },
                        new TemporaryAnimatedSprite(29, pos + new Vector2((float)(Game1.tileSize * 3 / 4), 0.0f), Color.White * 0.5f, 4, false, 60f, numberOfLoops, -1, -1f, -1, 0)
                        {
                            rotation = 1.570796f,
                            delayBeforeAnimationStart = animDelay,
                            id = animId
                        },
                        new TemporaryAnimatedSprite(29, pos + new Vector2(0.0f, (float)(Game1.tileSize * 3 / 4)), Color.White * 0.5f, 4, false, 60f, numberOfLoops, -1, -1f, -1, 0)
                        {
                            rotation = 3.141593f,
                            delayBeforeAnimationStart = animDelay,
                            id = animId
                        },
                        new TemporaryAnimatedSprite(29, pos + new Vector2((float)(-Game1.tileSize * 3 / 4), 0.0f), Color.White * 0.5f, 4, false, 60f, numberOfLoops, -1, -1f, -1, 0)
                        {
                            rotation = 4.712389f,
                            delayBeforeAnimationStart = animDelay,
                            id = animId
                        }
                    });
                    break;
                case animSize.MEDIUM:
                    pos -= new Vector2(Game1.tileSize, Game1.tileSize);
                    mp.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite[]
                    {
                        new TemporaryAnimatedSprite(Game1.animations.Name, new Rectangle(0, 1984, Game1.tileSize * 3, Game1.tileSize * 3), 60f, 3, numberOfLoops, pos, false, false)
                        {
                            color = Color.White * 0.4f,
                            delayBeforeAnimationStart = animDelay,
                            id = animId
                        }
                    });
                    break;
                case animSize.LARGE:
                    pos += new Vector2(-3 * Game1.tileSize + Game1.tileSize, -Game1.tileSize * 2);                    
                    mp.broadcastSprites(Game1.currentLocation, new TemporaryAnimatedSprite[]
                    {
                        new TemporaryAnimatedSprite(Game1.animations.Name, new Rectangle(0, 2176, Game1.tileSize * 5, Game1.tileSize * 5), 60f, 4, numberOfLoops, pos, false, false)
                        {
                            color = Color.White * 0.4f,
                            delayBeforeAnimationStart = animDelay,
                            id = animId
                        }
                    });
                    break;
            }
        }
    }
}
