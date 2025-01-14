﻿/*
    Copyright 2011 MCForge
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;
using MCGalaxy.Blocks.Physics;
using MCGalaxy.Games;
using BlockID = System.UInt16;

namespace MCGalaxy.Modules.Games.LS 
{
    public sealed partial class LSGame : RoundsGame 
    {
        void UpdateBlockHandlers() {
            Map.UpdateBlockHandlers(Block.Water);
            Map.UpdateBlockHandlers(Block.Deadly_ActiveWater);
            Map.UpdateBlockHandlers(Block.Lava);
            Map.UpdateBlockHandlers(Block.Deadly_ActiveLava);
        }

        void HandleBlockHandlersUpdated(Level lvl, BlockID block) {
            if (!Running || lvl != Map) return;

            switch (block)
            {
                case Block.Water:
                case Block.Deadly_ActiveWater:
                    lvl.PhysicsHandlers[block] = DoWater; break;
                case Block.Lava:
                case Block.Deadly_ActiveLava:
                    lvl.PhysicsHandlers[block] = DoLava; break;
            }
        }

        void DoWater(Level lvl, ref PhysInfo C) {
            ushort x = C.X, y = C.Y, z = C.Z;
            
            if (!lvl.CheckSpongeWater(x, y, z)) {
                BlockID block = C.Block;
                
                SpreadWater(lvl, (ushort)(x + 1), y, z, block);
                SpreadWater(lvl, (ushort)(x - 1), y, z, block);
                SpreadWater(lvl, x, y, (ushort)(z + 1), block);
                SpreadWater(lvl, x, y, (ushort)(z - 1), block);
                SpreadWater(lvl, x, (ushort)(y - 1), z, block);

                if (floodUp) SpreadWater(lvl, x, (ushort)(y + 1), z, block);
            } else { //was placed near sponge
                lvl.AddUpdate(C.Index, Block.Air, default(PhysicsArgs));
            }
            C.Data.Data = PhysicsArgs.RemoveFromChecks;
        }
        
        void DoLava(Level lvl, ref PhysInfo C) {
            ushort x = C.X, y = C.Y, z = C.Z;

            if (C.Data.Data < spreadDelay) {
                C.Data.Data++; return;
            }
            
            if (!lvl.CheckSpongeLava(x, y, z)) {
                BlockID block = C.Block;
                SpreadLava(lvl, (ushort)(x + 1), y, z, block);
                SpreadLava(lvl, (ushort)(x - 1), y, z, block);
                SpreadLava(lvl, x, y, (ushort)(z + 1), block);
                SpreadLava(lvl, x, y, (ushort)(z - 1), block);
                SpreadLava(lvl, x, (ushort)(y - 1), z, block);

                if (floodUp) SpreadLava(lvl, x, (ushort)(y + 1), z, block);
            } else { //was placed near sponge
                lvl.AddUpdate(C.Index, Block.Air, default(PhysicsArgs));
            }
            C.Data.Data = PhysicsArgs.RemoveFromChecks;
        }

                
        void SpreadWater(Level lvl, ushort x, ushort y, ushort z, BlockID type) {
            int index;
            BlockID block = lvl.GetBlock(x, y, z, out index);
            if (InSafeZone(x, y, z)) return;

            switch (block) {
                case Block.Air:
                    if (!lvl.CheckSpongeWater(x, y, z)) {
                        lvl.AddUpdate(index, type);
                    }
                    break;

                case Block.Lava:
                case Block.FastLava:
                case Block.Deadly_ActiveLava:
                    if (!lvl.CheckSpongeWater(x, y, z)) {
                        lvl.AddUpdate(index, Block.Stone, default(PhysicsArgs));
                    }
                    break;

                case Block.Sand:
                case Block.Gravel:
                case Block.FloatWood:
                    lvl.AddCheck(index); break;
                    
                default:
                    // Adv physics kills flowers and mushrooms in water
                    if (!lvl.Props[block].WaterKills) break;
                    
                    if (lvl.physics > 1 && !lvl.CheckSpongeWater(x, y, z)) {
                        lvl.AddUpdate(index, Block.Air, default(PhysicsArgs));
                    }
                    break;
            }
        }
        
        void SpreadLava(Level lvl, ushort x, ushort y, ushort z, BlockID type) {
            int index;
            BlockID block = lvl.GetBlock(x, y, z, out index);
            if (InSafeZone(x, y, z)) return;

            // in LS, sponge should stop lava too
            switch (block) {
                case Block.Air:
                    if (!lvl.CheckSpongeWater(x, y, z)) {
                        lvl.AddUpdate(index, type);
                    }
                    break;
                    
                case Block.Water:
                case Block.Deadly_ActiveWater:
                    if (!lvl.CheckSpongeWater(x, y, z)) {
                        lvl.AddUpdate(index, Block.Stone, default(PhysicsArgs));
                    }
                    break;
                    
                case Block.Sand:
                    if (lvl.physics > 1) { //Adv physics changes sand to glass next to lava
                        lvl.AddUpdate(index, Block.Glass, default(PhysicsArgs));
                    } else {
                        lvl.AddCheck(index);
                    } break;
                    
                case Block.Gravel:
                    lvl.AddCheck(index); break;

                default:
                    //Adv physics kills flowers, wool, mushrooms, and wood type blocks in lava
                    if (!lvl.Props[block].LavaKills) break;
                    
                    if (lvl.physics > 1 && !lvl.CheckSpongeWater(x, y, z)) {
                        lvl.AddUpdate(index, Block.Air, default(PhysicsArgs));
                    }
                    break;
            }
        }
    }
}
