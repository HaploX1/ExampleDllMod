// This is an advanced example, which shows you, how your own project can look
// I'll use comments only sparcely compared to example 1.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;               
//using Verse.AI;          
//using Verse.Sound;       
//using Verse.Noise;       
using RimWorld;            
//using RimWorld.Planet;   
//using RimWorld.SquadAI;


namespace WindTurbine
{
    /// <summary>
    /// This is a wind tubine
    /// </summary>
    /// <author>Haplo</author>
    /// <permission>Usage of this code is free for all</permission>
    [StaticConstructorOnStartup]
    public class Building_WindTurbineEx : Building
    {

        #region Variables
        // ==================================

        private bool disableAnimation = false;
        private bool disablePowerRandomness = false;

        // This is the graphics container:
        public static Graphic[] graphic = null;

        private const int arraySize = 12; // Turn animation off => set to 1
        private string graphicPathAdditionWoNumber = "_frame"; // everything before this will be used for the other file names

        private int activeGraphicFrame = 0;
        private int ticksSinceUpdateGraphic;
        private int updateAnimationEveryXTicks = 5; // => 60 ticks per second / 12 graphic frames = 5 ticks per frame

        protected CompPowerTrader powerComp;

        private int updateWeatherEveryXTicks = 250;
        private int ticksSinceUpdateWeather;
        private bool windPathBlocked = false;
        private Thing windPathBlocker;
        private List<IntVec3> windPathCells;

        private const string translateWindPathIsBlocked = "WindTurbineExample_WindPathIsBlocked";
        private const string translateWindPathIsBlockedBy = "WindTurbineExample_WindPathIsBlockedBy";


        #endregion


        #region Setup Work
        // ==================================

        /// <summary>
        /// Do something after the object is spawned into the world
        /// </summary>
        public override void SpawnSetup()
        {
            base.SpawnSetup();

            powerComp = base.GetComp<CompPowerTrader>();
            powerComp.PowerOn = true;

            CheckWindPath( ref windPathCells, out windPathBlocker );

        }


        /// <summary>
        /// To save and load actual values (savegame-data)
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<int>( ref ticksSinceUpdateWeather, "updateCounter" );
        }


        /// <summary>
        /// Import the graphics
        /// </summary>
        private void UpdateGraphics()
        {
            // Check if graphic is already filled
            if (graphic != null && graphic.Length > 0 && graphic[0] != null)
                return;

            // resize the graphic array
            graphic = new Graphic_Single[arraySize];

            // Get the base path (without _frameXX)
            int indexOf_frame = def.graphicData.texPath.ToLower().LastIndexOf(graphicPathAdditionWoNumber);
            string graphicRealPathBase = def.graphicData.texPath.Remove( indexOf_frame );

            // fill the graphic array
            for ( int i = 0; i < arraySize; i++ )
            {
                string graphicRealPath = graphicRealPathBase + graphicPathAdditionWoNumber + ( i + 1 ).ToString();

                // Set the graphic, use additional info from the xml data
                graphic[i] = GraphicDatabase.Get<Graphic_Single>(graphicRealPath, def.graphic.Shader, def.graphic.drawSize, def.graphic.Color, def.graphic.ColorTwo);
            }
        }

        #endregion


        #region Destroy
        // ==================================

        /// <summary>
        /// Clean up when this is destroyed
        /// </summary>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
        }

        #endregion


        #region Ticker
        // ==================================

        /// <summary>
        /// This is used, when the Ticker is set to Normal
        /// This Tick is done often (60 times per second)
        /// </summary>
        public override void Tick()
        {

            // Call work function
            DoTickerWork( 1 );

            base.TickRare();
        }


        // Here is the main decision work of this building done
        private void DoTickerWork( int ticks )
        {

            // Power off OR Roofed Position
            if ( powerComp == null || !powerComp.PowerOn ||
                Find.RoofGrid.Roofed( Position ) )
            {
                activeGraphicFrame = 0;
                powerComp.PowerOutput = 0;
                return;
            }


            // Graphic update
            if (!disableAnimation)
            {
                ticksSinceUpdateGraphic += ticks;
                if (ticksSinceUpdateGraphic >= updateAnimationEveryXTicks)
                {
                    ticksSinceUpdateGraphic = 0;
                    activeGraphicFrame++;
                    if (activeGraphicFrame >= arraySize)
                        activeGraphicFrame = 0;

                    // Tell the MapDrawer that here is something thats changed
                    Find.MapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, true, false);
                }
            }

            // Power update based on weather
            ticksSinceUpdateWeather += ticks;
            if ( ticksSinceUpdateWeather >= updateWeatherEveryXTicks )
            {
                ticksSinceUpdateWeather = 0;
                WeatherDef weather = Find.WeatherManager.curWeather;
                powerComp.PowerOutput = -( powerComp.Props.basePowerConsumption * weather.windSpeedFactor );

                // Just for a little bit randomness..
                if (!disablePowerRandomness)
                    powerComp.PowerOutput += Rand.RangeInclusive(-20, 20);

                // If obstacled, reduce production to 1/3
                windPathBlocked = !CheckWindPath( ref windPathCells, out windPathBlocker );
                if ( windPathBlocked )
                    powerComp.PowerOutput /= 3;

            }
        }



        #endregion


        #region Graphics
        // ==================================

        /// <summary>
        /// This returns the graphic of the object.
        /// The renderer will draw the needed object graphic from here.
        /// </summary>
        public override Graphic Graphic
        {
            get
            {
                if (disableAnimation)
                    return base.Graphic;

                if ( graphic == null || graphic[0] == null )
                {
                    UpdateGraphics();
                    // Graphic couldn't be loaded? (Happends after load for a while)
                    if ( graphic == null || graphic[0] == null )
                        return base.Graphic;
                }

                if ( graphic[activeGraphicFrame] != null )
                    return graphic[activeGraphicFrame];

                return base.Graphic;
            }
        }


        /// <summary>
        /// This string will be shown when the object is selected (focus)
        /// </summary>
        /// <returns></returns>
        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(base.GetInspectString());

            stringBuilder.AppendLine();

            if ( windPathBlocked )
            {
                stringBuilder.Append(translateWindPathIsBlocked.Translate());
                stringBuilder.AppendLine();

                if ( windPathBlocker != null ) // If null, then a wind cell is roofed
                    stringBuilder.Append(translateWindPathIsBlockedBy.Translate() + " " + windPathBlocker.Label);
            }

            // return the complete string
            return stringBuilder.ToString();
        }


        /// <summary>
        /// This will draw extended overlays, like the circle over the orbital trade beacon
        /// Here it will be used to show where the free wind path is needed.
        /// </summary>
        public override void DrawExtraSelectionOverlays()
        {
            //base.DrawExtraSelectionOverlays();

            if ( windPathCells != null )
            {
                GenDraw.DrawFieldEdges( windPathCells );
            }
        }

        #endregion


        #region Functions

        // Check if an object is blocking the wind flow
        // Check for Thing.def.altitudeLayer == BuildingTall
        private bool CheckWindPath( ref List<IntVec3> checkCells, out Thing foundBlocker )
        {
            if ( checkCells == null )
                checkCells = new List<IntVec3>();

            if ( checkCells.Count() == 0 )
            {
                IEnumerable<IntVec3> tmpCells = GetCellsOfConeBeforeAndBehindObject( Position, Rotation, def.size );
                checkCells.AddRange( tmpCells );
            }

            foundBlocker = null;
            foreach ( IntVec3 cell in checkCells )
            {
                if ( Find.RoofGrid.Roofed( cell ) )
                    return false;

                foundBlocker = Find.ThingGrid.ThingsAt(cell).FirstOrDefault<Thing>( t => t.def.blockWind );
                if ( foundBlocker != null )
                    return false;
            }
            return true;
        }


        // Find the cells in a cone before and behind the wind turbine
        // This is a bit difficult to understand. You don't need to understand how this works. 
        // Just know that this will define the cells, where to look for blocker items
        private IEnumerable<IntVec3> GetCellsOfConeBeforeAndBehindObject( IntVec3 thingCenter, Rot4 thingRot, IntVec2 thingSize )
        {

            //  +++          +xx+
            // +++++        ++xx++ 
            // XXXXX        ++xx++       
            // XXXXX        ++xx++ 
            // +++++        ++xx++ 
            //  +++          +xx+ 

            // Description:
            // X - Object
            // + - Returned cells

            int numBaseX1, numBaseX2, numBaseZ1, numBaseZ2;
            int num1X1, num1X2, num2X1, num2X2, num1Z1, num1Z2, num2Z1, num2Z2;
            int num3X1, num3X2, num3Z1, num3Z2, num4X1, num4X2, num4Z1, num4Z2;

            int extensionLength = 5;

            // Find base points to work with
            if ( thingRot == Rot4.North )
            {
                numBaseX1 = thingCenter.x - (thingSize.x + 1) / 2 + 1;
                numBaseX2 = numBaseX1 + thingSize.x - 1;
                numBaseZ1 = thingCenter.z - (thingSize.z + 1) / 2 + 1;
                numBaseZ2 = numBaseZ1 + thingSize.z - 1;
            }
            else if (thingRot == Rot4.East)
            {
                numBaseX1 = thingCenter.x - (thingSize.z + 1) / 2 + 1;
                numBaseX2 = numBaseX1 + thingSize.z - 1;
                numBaseZ1 = thingCenter.z - thingSize.x / 2;
                numBaseZ2 = numBaseZ1 + thingSize.x - 1;
            }
            else if (thingRot == Rot4.South)
            {
                numBaseX1 = thingCenter.x - thingSize.x / 2;
                numBaseX2 = numBaseX1 + thingSize.x - 1;
                numBaseZ1 = thingCenter.z - thingSize.z / 2;
                numBaseZ2 = numBaseZ1 + thingSize.z - 1;
            }
            else //if ( thingRot == Rot4.West )
            {
                numBaseX1 = thingCenter.x - thingSize.z / 2;
                numBaseX2 = numBaseX1 + thingSize.z - 1;
                numBaseZ1 = thingCenter.z - (thingSize.x + 1) / 2 + 1;
                numBaseZ2 = numBaseZ1 + thingSize.x - 1;
            }

            IntVec3 intVec3;

            // Get the cells from here if the rotation is north or south
            if (Rotation == Rot4.North || Rotation == Rot4.South)
            {
                // Base cone
                num1X1 = numBaseX1 + 0;
                num1X2 = numBaseX2 + 0;
                num1Z1 = numBaseZ1 - 1;
                num1Z2 = numBaseZ2 + 1;

                num2X1 = numBaseX1 + 1;
                num2X2 = numBaseX2 - 1;
                num2Z1 = numBaseZ1 - 2;
                num2Z2 = numBaseZ2 + 2;

                // Extended dome
                num3X1 = numBaseX1 + 1;
                num3X2 = numBaseX2 - 1;
                num3Z1 = numBaseZ1 - (extensionLength + 2);
                num3Z2 = numBaseZ1 - (extensionLength - 2);

                num4X1 = numBaseX1 + 1;
                num4X2 = numBaseX2 - 1;
                num4Z1 = numBaseZ2 + (extensionLength - 2);
                num4Z2 = numBaseZ2 + (extensionLength + 2);


                intVec3 = new IntVec3(num1X1, 0, num1Z1);
                do
                {
                    yield return intVec3;
                    intVec3.x += 1;
                } while (intVec3.x <= num1X2);

                intVec3 = new IntVec3(num2X1, 0, num2Z1);
                do
                {
                    yield return intVec3;
                    intVec3.x += 1;
                } while (intVec3.x <= num2X2);


                intVec3 = new IntVec3(num1X1, 0, num1Z2);
                do
                {
                    yield return intVec3;
                    intVec3.x += 1;
                } while (intVec3.x <= num1X2);

                intVec3 = new IntVec3(num2X1, 0, num2Z2);
                do
                {
                    yield return intVec3;
                    intVec3.x += 1;
                } while (intVec3.x <= num2X2);


                // Extended dome
                intVec3 = new IntVec3(num3X1 - 1, 0, num3Z1);
                while (intVec3.x < num3X2 || intVec3.z < num3Z2)
                {
                    if (intVec3.x < num3X2)
                    {
                        intVec3.x += 1;
                    }
                    else if (intVec3.z <= num3Z2)
                    {
                        intVec3.x = num3X1;
                        intVec3.z += 1;
                    }
                    yield return intVec3;
                } 

                intVec3 = new IntVec3(num4X1 - 1, 0, num4Z1);
                while (intVec3.x < num4X2 || intVec3.z < num4Z2)
                {
                    if (intVec3.x < num4X2)
                    {
                        intVec3.x += 1;
                    }
                    else if (intVec3.z <= num4Z2)
                    {
                        intVec3.x = num4X1;
                        intVec3.z += 1;
                    }
                    yield return intVec3;
                } 

            }

            // Get the cells from here if the rotation is east or west
            if (Rotation == Rot4.East || Rotation == Rot4.West)
            {
                // Base cone
                num1X1 = numBaseX1 - 1;
                num1X2 = numBaseX2 + 1;
                num1Z1 = numBaseZ1 + 0;
                num1Z2 = numBaseZ2 + 0;

                num2X1 = numBaseX1 - 2;
                num2X2 = numBaseX2 + 2;
                num2Z1 = numBaseZ1 + 1;
                num2Z2 = numBaseZ2 - 1;

                // Extended dome
                num3X1 = numBaseX1 - (extensionLength + 2);
                num3X2 = numBaseX1 - (extensionLength - 2);
                num3Z1 = numBaseZ1 + 1;
                num3Z2 = numBaseZ2 - 1;

                num4X1 = numBaseX2 + (extensionLength - 2);
                num4X2 = numBaseX2 + (extensionLength + 2);
                num4Z1 = numBaseZ1 + 1;
                num4Z2 = numBaseZ2 - 1;


                intVec3 = new IntVec3(num1X1, 0, num1Z1);
                do
                {
                    yield return intVec3;
                    intVec3.z += 1;
                } while (intVec3.z <= num1Z2);

                intVec3 = new IntVec3(num2X1, 0, num2Z1);
                do
                {
                    yield return intVec3;
                    intVec3.z += 1;
                } while (intVec3.z <= num2Z2);


                intVec3 = new IntVec3(num1X2, 0, num1Z1);
                do
                {
                    yield return intVec3;
                    intVec3.z += 1;
                } while (intVec3.z <= num1Z2);

                intVec3 = new IntVec3(num2X2, 0, num2Z1);
                do
                {
                    yield return intVec3;
                    intVec3.z += 1;
                } while (intVec3.z <= num2Z2);


                // Extended dome
                intVec3 = new IntVec3(num3X1 - 1, 0, num3Z1);
                while (intVec3.x < num3X2 || intVec3.z < num3Z2)
                {
                    if (intVec3.x < num3X2)
                    {
                        intVec3.x += 1;
                    }
                    else if (intVec3.z <= num3Z2)
                    {
                        intVec3.x = num3X1;
                        intVec3.z += 1;
                    }
                    yield return intVec3;
                }

                intVec3 = new IntVec3(num4X1 - 1, 0, num4Z1);
                while (intVec3.x < num4X2 || intVec3.z < num4Z2)
                {
                    if (intVec3.x < num4X2)
                    {
                        intVec3.x += 1;
                    }
                    else if (intVec3.z <= num4Z2)
                    {
                        intVec3.x = num4X1;
                        intVec3.z += 1;
                    }
                    yield return intVec3;
                } 

            }
        }


        #endregion

    }
}
