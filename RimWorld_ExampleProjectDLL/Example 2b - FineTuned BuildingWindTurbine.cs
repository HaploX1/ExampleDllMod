//This is the fine tuned source code of the wind turbine
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;


namespace WindTurbine
{
    /// <summary>
    /// This is a wind tubine building
    /// </summary>
    class Building_PowerPlantWindTurbine : Building_PowerPlant
    {

        #region Variables
        // ==================================


        public static Graphic[] graphic = null;

        private const int arraySize = 12; // Turn animation off => set to 1
        private string graphicPathAdditionWoNumber = "_frame"; // everything before this will be used for the other file names

        private int activeGraphicFrame = 0;
        private int activeGraphicFrameOld = -1;
        private int ticksSinceUpdateGraphic;
        private const int updateAnimationEveryXTicks = 10;


        public int updateWeatherEveryXTicks = 250;
        private int ticksSinceWeatherUpdate;

        private List<IntVec3> windPathCells;
        private bool windPathBlocked;
        private List<Thing> windPathBlockedByThings;
        private List<IntVec3> windPathBlockedCells;
        private const float powerReductionPercentPerObstacle = 0.2f; 

        private float maxWindIntensity = 0f;

        private const string translateWindPathIsBlocked = "WindTurbineExample_WindPathIsBlocked";
        private const string translateWindPathIsBlockedBy = "WindTurbineExample_WindPathIsBlockedBy";
        private const string translateWindPathIsBlockedByRoof = "WindTurbineExample_WindPathIsBlockedByRoof";

        private static Vector2 BarSize;
        private readonly static Material BarFilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.5f, 0.475f, 0.1f));
        private readonly static Material BarUnfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.15f, 0.15f, 0.15f));

        #endregion


        #region Setup Work
        // ==================================

        /// <summary>
        /// Do something after the object is spawned into the world
        /// </summary>
        public override void SpawnSetup()
        {
            base.SpawnSetup();

            BarSize = new Vector2(def.size.z - 0.95f, 0.14f);

            maxWindIntensity = 0f;
            foreach (WeatherDef wDef in DefDatabase<WeatherDef>.AllDefs)
            {
                if (wDef.windSpeedFactor > maxWindIntensity)
                    maxWindIntensity = wDef.windSpeedFactor;
            }

            CheckWindPathBlocked(ref windPathCells, out windPathBlockedCells, out windPathBlockedByThings);
        }


        /// <summary>
        /// To save and load actual values (savegame-data)
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<int>(ref ticksSinceWeatherUpdate, "updateCounter");
        }


        /// <summary>
        /// Import the graphics
        /// </summary>
        private void UpdateGraphics()
        {
            // resize the graphic array
            graphic = new Graphic_Single[arraySize];

            // Get the base path (without _frameXX)
            int indexOf_frame = def.graphicPath.ToLower().LastIndexOf(graphicPathAdditionWoNumber);
            string graphicRealPathBase = def.graphicPath.Remove(indexOf_frame);

            // fill the graphic array
            for (int i = 0; i < arraySize; i++)
            {
                string graphicRealPath = graphicRealPathBase + graphicPathAdditionWoNumber + (i + 1).ToString();

                // Set the graphic
                graphic[i] = GraphicDatabase.Get<Graphic_Single>(graphicRealPath, def.shader, def.DrawSize, def.defaultColor, def.defaultColorTwo);
            }
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
            base.Tick();


            // Power off
            if ( powerComp == null || !powerComp.PowerOn )
            {
                activeGraphicFrame = 0;
                powerComp.powerOutput = -0.0f;
                return;
            }


            if (powerComp.powerOutput != 0)
            {
                ticksSinceUpdateGraphic++;
                if (ticksSinceUpdateGraphic >= updateAnimationEveryXTicks)
                {
                    ticksSinceUpdateGraphic = 0;
                    activeGraphicFrame++;
                    if (activeGraphicFrame >= arraySize)
                        activeGraphicFrame = 0;

                    UpdateGraphicForAnimation();
                }
            }



            // Power update based on weather
            // This is used instead of TickRare to prevent the possibility, that all turbines check the wind path at the same time after loading
            ticksSinceWeatherUpdate++;
            if (ticksSinceWeatherUpdate >= updateWeatherEveryXTicks)
            {
                ticksSinceWeatherUpdate = 0;
                WeatherDef weather = Find.WeatherManager.curWeather;
                powerComp.powerOutput = -( powerComp.props.basePowerConsumption * weather.windSpeedFactor );

                // Just for a little bit wind randomness..
                powerComp.powerOutput += (float)Rand.RangeInclusive(-20, 20);

                // If obstacled, reduce production
                windPathBlocked = CheckWindPathBlocked(ref windPathCells, out windPathBlockedCells, out windPathBlockedByThings);

                if (windPathBlocked)
                {
                    float reduction = 0;
                    for (int i = 0; i < windPathBlockedCells.Count; i++)
                        reduction += powerComp.powerOutput * powerReductionPercentPerObstacle;

                    if (reduction < powerComp.powerOutput)
                        powerComp.powerOutput -= reduction;
                    else
                    {
                        powerComp.powerOutput = -0.0f;
                        activeGraphicFrame = 0;
                        UpdateGraphicForAnimation();
                    }
                }
            }
        }



        #endregion


        #region Graphics / Inspections
        // ==================================

        /// <summary>
        /// This returns the graphic of the object.
        /// The renderer will draw the needed object graphic from here.
        /// </summary>
        public override Graphic Graphic
        {
            get
            {

                if (graphic == null || graphic[0] == null)
                {
                    UpdateGraphics();
                    // Graphic couldn't be loaded? (Happends after load for a while)
                    if (graphic == null || graphic[0] == null)
                        return base.Graphic;
                }

                if (graphic[activeGraphicFrame] != null)
                    return graphic[activeGraphicFrame];

                return base.Graphic;
            }
        }



        /// <summary>
        /// Draw power display
        /// </summary>
        public override void Draw()
        {
            base.Draw();
            GenDraw.FillableBarRequest fillableBarRequest = new GenDraw.FillableBarRequest()
            {
                center = DrawPos + (Vector3.up * 0.1f),
                size = BarSize,
                fillPercent = powerComp.powerOutput / (-powerComp.props.basePowerConsumption * maxWindIntensity),
                filledMat = BarFilledMat,
                unfilledMat = BarUnfilledMat,
                margin = 0.15f
            };

            IntRot rotation = base.Rotation;
            rotation.Rotate(RotationDirection.Clockwise);
            fillableBarRequest.rotation = rotation;
            GenDraw.DrawFillableBar(fillableBarRequest);
        }


        /// <summary>
        /// This string will be shown when the object is selected (focus)
        /// </summary>
        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(base.GetInspectString());

            stringBuilder.AppendLine();

            if (windPathBlocked)
            {
                stringBuilder.Append(translateWindPathIsBlocked.Translate());
                stringBuilder.AppendLine();

                Thing blocker = null;
                if (windPathBlockedByThings != null)
                    blocker = windPathBlockedByThings[0];

                if (blocker != null)
                    stringBuilder.Append(translateWindPathIsBlockedBy.Translate() + " " + blocker.Label);
                else
                    stringBuilder.Append(translateWindPathIsBlockedByRoof.Translate());

            }

            // return the complete string
            return stringBuilder.ToString();
        }


        /// <summary>
        /// Draw the wind path
        /// </summary>
        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();

            if (windPathCells != null)
                GenDraw.DrawFieldEdges(windPathCells);
        }

        #endregion


        #region Functions

        private void UpdateGraphicForAnimation()
        {
            if (activeGraphicFrame == activeGraphicFrameOld)
                return;

            activeGraphicFrameOld = activeGraphicFrame;

            // Tell the MapDrawer that here is something thats changed
            Find.MapDrawer.MapChanged(Position, MapChangeType.Things, true, false);
        }


        // Check if an object is blocking the wind flow
        // Check for Thing.def.altitudeLayer == BuildingTall
        // blockedThings are correlating to blockedCells, but can have null fields. This means that corresponding cell is roofed.
        private bool CheckWindPathBlocked(ref List<IntVec3> checkCells, out List<IntVec3> blockedCells, out List<Thing> blockedByThings)
        {
            if (checkCells == null)
                checkCells = new List<IntVec3>();

            if (checkCells.Count() == 0)
            {
                IEnumerable<IntVec3> tmpCells = GetCellsOfConeBeforeAndBehindObject(Position, Rotation, def.size);
                checkCells.AddRange(tmpCells);
            }

            blockedCells = new List<IntVec3>();
            blockedByThings = new List<Thing>();

            for (int i = 0; i < checkCells.Count; i++)
            {
                IntVec3 cell = checkCells[i];

                // roofed?
                if (Find.RoofGrid.Roofed(cell))
                {
                    blockedByThings.Add(null);
                    blockedCells.Add(cell);
                    continue;
                }

                // blocked?
                List<Thing> workThings = Find.ThingGrid.ThingsListAt(cell);
                for (int j = 0; j < workThings.Count; j++ )
                {
                    Thing workThing = workThings[j];
                    if (workThing.def.altitudeLayer == AltitudeLayer.BuildingTall)
                    {
                        blockedByThings.Add(workThing);
                        blockedCells.Add(cell);
                        break;
                    }
                }
            }

            return blockedCells.Count > 0;
        }


        // Find the cells of the wind path before and behind the wind turbine
        private IEnumerable<IntVec3> GetCellsOfConeBeforeAndBehindObject(IntVec3 thingCenter, IntRot thingRot, IntVec2 thingSize)
        {
            // Base cone:
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

            // change to resize wind path
            int extensionLengthFront = 5;
            int extensionLengthBack = 2;

            // Find base points to work with
            // X1 and Z1 are the lower values
            if (thingRot == IntRot.north)
            {
                numBaseX1 = thingCenter.x - (thingSize.x + 1) / 2 + 1;
                numBaseX2 = numBaseX1 + thingSize.x - 1;
                numBaseZ1 = thingCenter.z - (thingSize.z + 1) / 2 + 1;
                numBaseZ2 = numBaseZ1 + thingSize.z - 1;
            }
            else if (thingRot == IntRot.east)
            {
                numBaseX1 = thingCenter.x - (thingSize.z + 1) / 2 + 1;
                numBaseX2 = numBaseX1 + thingSize.z - 1;
                numBaseZ1 = thingCenter.z - thingSize.x / 2;
                numBaseZ2 = numBaseZ1 + thingSize.x - 1;
            }
            else if (thingRot == IntRot.south)
            {
                numBaseX1 = thingCenter.x - thingSize.x / 2;
                numBaseX2 = numBaseX1 + thingSize.x - 1;
                numBaseZ1 = thingCenter.z - thingSize.z / 2;
                numBaseZ2 = numBaseZ1 + thingSize.z - 1;
            }
            else //if ( thingRot == IntRot.west )
            {
                numBaseX1 = thingCenter.x - thingSize.z / 2;
                numBaseX2 = numBaseX1 + thingSize.z - 1;
                numBaseZ1 = thingCenter.z - (thingSize.x + 1) / 2 + 1;
                numBaseZ2 = numBaseZ1 + thingSize.x - 1;
            }

            IntVec3 intVec3;

            // Get the cells from here if the rotation is north or south
            if (Rotation == IntRot.north || Rotation == IntRot.south)
            {
                // Base cone, inner part
                num1X1 = numBaseX1 + 0;
                num1X2 = numBaseX2 + 0;
                num1Z1 = numBaseZ1 - 1;
                num1Z2 = numBaseZ2 + 1;
                // Base cone, outer part
                num2X1 = numBaseX1 + 0; // original: +1
                num2X2 = numBaseX2 + 0; // original: -1
                num2Z1 = numBaseZ1 - 2;
                num2Z2 = numBaseZ2 + 2;

                if (Rotation == IntRot.north)
                {
                    // Extended dome
                    num3X1 = numBaseX1 + 1;
                    num3X2 = numBaseX2 - 1;
                    num3Z1 = numBaseZ1 - 2 - extensionLengthBack;
                    num3Z2 = numBaseZ1 - 2;

                    num4X1 = numBaseX1 + 1;
                    num4X2 = numBaseX2 - 1;
                    num4Z1 = numBaseZ2 + (extensionLengthFront - 2);
                    num4Z2 = numBaseZ2 + (extensionLengthFront + 2);
                }
                else //if (Rotation == IntRot.south)
                {
                    // Extended dome
                    num3X1 = numBaseX1 + 1;
                    num3X2 = numBaseX2 - 1;
                    num3Z1 = numBaseZ1 - (extensionLengthFront + 2);
                    num3Z2 = numBaseZ1 - (extensionLengthFront - 2);

                    num4X1 = numBaseX1 + 1;
                    num4X2 = numBaseX2 - 1;
                    num4Z1 = numBaseZ2 + 2;
                    num4Z2 = numBaseZ2 + 2 + extensionLengthBack;
                }


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
            if (Rotation == IntRot.east || Rotation == IntRot.west)
            {
                // Base cone, inner part
                num1X1 = numBaseX1 - 1;
                num1X2 = numBaseX2 + 1;
                num1Z1 = numBaseZ1 + 0;
                num1Z2 = numBaseZ2 + 0;
                // Base cone, outer part
                num2X1 = numBaseX1 - 2;
                num2X2 = numBaseX2 + 2;
                num2Z1 = numBaseZ1 + 0; // original: +1
                num2Z2 = numBaseZ2 + 0; // original: -1

                if (Rotation == IntRot.east)
                {
                    // Extended dome
                    num3X1 = numBaseX1 - 2 - extensionLengthBack;
                    num3X2 = numBaseX1 - 2;
                    num3Z1 = numBaseZ1 + 1;
                    num3Z2 = numBaseZ2 - 1;

                    num4X1 = numBaseX2 + (extensionLengthFront - 2);
                    num4X2 = numBaseX2 + (extensionLengthFront + 2);
                    num4Z1 = numBaseZ1 + 1;
                    num4Z2 = numBaseZ2 - 1;
                }
                else //if (Rotation == IntRot.west)
                {
                    // Extended dome
                    num3X1 = numBaseX1 - (extensionLengthFront + 2);
                    num3X2 = numBaseX1 - (extensionLengthFront - 2);
                    num3Z1 = numBaseZ1 + 1;
                    num3Z2 = numBaseZ2 - 1;

                    num4X1 = numBaseX2 + 2;
                    num4X2 = numBaseX2 + 2 + extensionLengthBack;
                    num4Z1 = numBaseZ1 + 1;
                    num4Z2 = numBaseZ2 - 1;
                }


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
