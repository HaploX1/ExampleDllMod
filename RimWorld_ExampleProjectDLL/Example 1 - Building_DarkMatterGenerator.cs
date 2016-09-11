// Note:
// The first steps are with this tutorial already done:
// - The target .Net version is already switched to 3.5
// - The references are already set to UnityEngine and Assembly-CSharp
//   (Can all be done in the Projectsolution-Explorer)
//     => right-click on RimWorld_ExampleProject => Settings => ...
// So on we go...

// ----------------------------------------------------------------------
// These are basic usings. Always let them be here.
// ----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// ----------------------------------------------------------------------
// These are RimWorld-specific usings. Activate/Deactivate what you need:
// ----------------------------------------------------------------------
using UnityEngine;         // Always needed
//using VerseBase;         // Material/Graphics handling functions are found here
using Verse;               // RimWorld universal objects are here (like 'Building')
//using Verse.AI;          // Needed when you do something with the AI
//using Verse.Sound;       // Needed when you do something with Sound
//using Verse.Noise;       // Needed when you do something with Noises
using RimWorld;            // RimWorld specific functions are found here (like 'Building_Battery')
//using RimWorld.Planet;   // RimWorld specific functions for world creation
//using RimWorld.SquadAI;  // RimWorld specific functions for squad brains 

// Note: If the usings are not found, (red line under it,) look into the folder '/Source-DLLs' and follow the instructions in the text files


// Now the program starts:
// Here I've provided the source code of the Dark Matter Generator. A small power generating structure based of the Plasma Generator from mrofa


// This is the namespace of your mod. Change it to your liking, but don't forget to change it in your XML too.
namespace DarkMatterGenerator
{
    // Now follows the class with the actual programm of the building. 

    // But first a few descriptions:

    // /// <summary>
    // /// This is an XML-Tag. It is good practice to make a short description of the function in these.
    // /// If you call the function from somewhere else, you'll get this summary text as a quick tooltip info.
    // /// Try the ToolTipSample in the class...
    // /// </summary>


    // class (This is a class)
    // Building_DarkMatterGenerator (This is the name of your new class) 
    // : Building (This is the base class 'Building'. Your class takes all the functions from the base class, but can override some of them to make it work how you like it to work.)
    //             This is, so that you don't need to write every function needed again and again. You write it once and take this as a base, so that the other classes have it already.
    //             The same goes for the error correction. If you find an error, you only have to correct it once and all your classes, that use this as a base, have the correction.
    

    // Note: The following code will be partially created, if you write /// in the line before a class, function, ...
    /// <summary>
    /// This is the main class for the Dark Matter Generator.
    /// </summary>
    /// <author>mrofa, Haplo</author>
    /// <permission>Usage of this code is .....</permission>
    public class Building_DarkMatterGenerator : Building
    {
        
        // No real function, just test of tooltip
        private void Tooltip_Test()
        {
            // Hover over this function and look at the tooltip..
            ToolTipSample.Sample();
            // Compare the tooltip with what is written in ToolTipSample..
        }


        // It is easier to understand your code, when you write your private variables at the beginning of the class
        // private variables => variables that need to hold their values between ticks or longer
        // Another reason to place them here and later on only use the variables instead of the values: 
        //   If you want to change the values, you have them directly at the beginning and not somewhere deep in your code..
        // ------------------------------------------------------------------------------------------------------


        // ===================== Variables =====================

        // Work variable
        private int counter = 0;                  // 60Ticks = 1s // 20000Ticks = 1 Day
        private Phase phase = Phase.offline;      // Actual phase
        private Phase phaseOld = Phase.active;    // Save-variable

        // Variables to set a specific value
        private const int counterPhaseChargingMax = 2500; // Recharge-Time
        private const int counterPhaseActiveMax = 20000;  // Active-Time

        private float powerOutputCharging = -250;         // Power needed at Recharge
        private float powerOutputActive = 1000;           // Power produced at Active


        // Work enumeration - To make the reading of the active phase easier
        private enum Phase
        {
            offline = 0,
            recharging,
            active
        }

        // Component references (will be set in 'SpawnSetup()')
        // CompGlower       - This makes it possible for your building to glow. You can start and stop the glowing.
        // CompPowerTrader  - Checks, if power is available; gives power to the powernet;...
        private CompGlower glowerComp;
        private CompPowerTrader powerComp;

        // Sound refferences (Not used here..)
        //private static readonly SoundDef SoundHiss = SoundDef.Named("PowerOn");

        // Text-variables: with .Translate() they are updated from the language file in the folder 'Languages\en_US\Keyed\...' with the active language.
        // Look into the function 'GetInspectString()' on how it will be translated
        private string txtStatus = "DarkMatterGenerator_Status"; 
        private string txtOffline = "DarkMatterGenerator_Offline";
        private string txtRecharging = "DarkMatterGenerator_Recharging";
        private string txtActive = "DarkMatterGenerator_Active";


        // Destroyed flag. Most of the time not really needed, but sometimes...
        private bool destroyedFlag = false;



        // ===================== Setup Work =====================

        // --- Not really needed here ---
        ///// <summary>
        ///// Do something after the object is initialized, but before it is spawned
        ///// </summary>
        //public override void PostMake()
        //{
        //    // Do the work of the base class (Building)
        //    base.PostMake();
        //}


        /// <summary>
        /// Do something after the object is spawned
        /// </summary>
        public override void SpawnSetup()
        {
            // Do the work of the base class (Building)
            base.SpawnSetup();

            // Get refferences to the components CompPowerTrader and CompGlower
            SetPowerGlower();
        }


        /// <summary>
        /// To save and load actual values (savegame-data)
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            // Save and load the work variables, so they don't default after loading
            Scribe_Values.LookValue<Phase>(ref phase, "phase", Phase.offline);
            Scribe_Values.LookValue<int>(ref counter, "counter", 0);

            // Set the old value to the phase value
            phaseOld = phase;

            // Get refferences to the components CompPowerTrader and CompGlower
            SetPowerGlower();
        }


        /// <summary>
        /// Find the PowerComp and GlowerComp
        /// </summary>
        private void SetPowerGlower()
        {

            // Get refferences to the components CompPowerTrader and CompGlower
            powerComp = base.GetComp<CompPowerTrader>();
            glowerComp = base.GetComp<CompGlower>();

            // Preset the PowerOutput to 0 (negative values will draw power from the powernet)
            powerComp.PowerOutput = 0;

        }



        // ===================== Destroy =====================

        /// <summary>
        /// Clean up when this is destroyed
        /// </summary>
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            // block further ticker work
            destroyedFlag = true;

            base.Destroy(mode);
        }



        // ===================== Ticker =====================

        /// <summary>
        /// This is used, when the Ticker in the XML is set to 'Rare'
        /// This is a tick thats done once every 250 normal Ticks
        /// </summary>
        public override void TickRare()
        {
            if (destroyedFlag) // Do nothing further, when destroyed (just a safety)
                return;

            // Don't forget the base work
            base.TickRare();

            // Call work function
            DoTickerWork(250);
        }


        /// <summary>
        /// This is used, when the Ticker in the XML is set to 'Normal'
        /// This Tick is done often (60 times per second)
        /// </summary>
        public override void Tick()
        {
            if (destroyedFlag) // Do nothing further, when destroyed (just a safety)
                return;

            base.Tick();

            // Call work function
            DoTickerWork(1);
        }



        // ===================== Main Work Function =====================

        /// <summary>
        /// This will be called from one of the Ticker-Functions.
        /// </summary>
        /// <param name="tickerAmount"></param>
        private void DoTickerWork(int tickerAmount)
        {
            // The following, if activated, creates an entry to the output_log.txt file, so that you can debug something
            //Log.Error("This description will be shown, if active, in the console and always in the output_log.txt");

            if (powerComp.PowerOn)
            {
                // Power is on -> do work
                // ----------------------

                // We have 3 Phases: Offline, Recharging, Active
                // Offline: When the power was cut, it is offline (counter will be reset)
                // Recharge: For 1/8 of a day it needs 'Recharge', then it switches to 'Active'
                // Active: It produces power for 1 day, before it needs to 'Recharge'


                // phase == offline (status after power switch off)
                if (phase == Phase.offline)
                {
                    // Savety to prevent a loop if old == offline
                    if (phaseOld == Phase.offline)
                        phaseOld = Phase.active;

                    // set to the old phase
                    phase = phaseOld;
                    return;
                }

                // set the old variable
                phaseOld = phase;

                // increase the counter by the ticker amount
                counter += tickerAmount; // +1 with normal ticker, +250 with rare ticker

                // phase == charging
                if (phase == Phase.recharging)
                {
                    if (counter >= counterPhaseChargingMax) // counter >= 2500 ?
                    {
                        // Switch to active, counter 0
                        phase = Phase.active;
                        counter = 0;
                        return;
                    }

                    powerComp.PowerOutput = powerOutputCharging; // value: -250
                }

                // phase == active
                if (phase == Phase.active)
                {
                    if (counter >= counterPhaseActiveMax) // counter >= 20000 ?
                    {
                        // Switch to recharge, glower off, counter 0
                        phase = Phase.recharging;
                        counter = 0;
                        return;
                    }

                    powerComp.PowerOutput = powerOutputActive; // value: 1000
                }

            }
            else
            {
                // Power off

                // save old phase
                if (phase != Phase.offline)
                    phaseOld = phase;

                // set phase to offline
                phase = Phase.offline;

                powerComp.PowerOutput = 0;
            }
        }


        // ===================== Inspections =====================

        /// <summary>
        /// This string will be shown when the object is selected (focus)
        /// </summary>
        /// <returns></returns>
        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            // Add the inspections string from the base
            stringBuilder.Append(base.GetInspectString());

            // Add your own strings (caution: string shouldn't be more than 5 lines (including base)!)
            //stringBuilder.Append("Power output: " + powerComp.powerOutput + " W");
            //stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.Append(txtStatus.Translate() + " ");  // <= TRANSLATION

            // Phase -> Offline: Add text 'Offline' (Translation from active language file)
            if (phase == Phase.offline)
                stringBuilder.Append(txtOffline.Translate());   // <= TRANSLATION

            // Phase -> Recharging: Add text 'Recharge' (Translation from active language file)
            if (phase == Phase.recharging)
                stringBuilder.Append(txtRecharging.Translate());// <= TRANSLATION

            // Phase -> Active: Add text 'Active' (Translation from active language file)
            if (phase == Phase.active)
                stringBuilder.Append(txtActive.Translate());    // <= TRANSLATION

            // return the complete string
            return stringBuilder.ToString();
        }

        ///// <summary>
        ///// This creates selection buttons
        ///// </summary>
        ///// <returns></returns>
        //public override IEnumerable<Command> GetGizmos()
        //{
        //    IList<Command> list = new List<Command>();

        //    // Key-Binding F - 
        //    Command_Action optF;
        //    optF = new Command_Action();
        //    optF.icon = UI_DoorLocked;
        //    optF.defaultDesc = txtLocksUnlocksDoor.Translate();
        //    optF.hotKey = KeyCode.F;
        //    optF.activateSound = SoundDef.Named("Click");
        //    optF.action = DoWorkFunction;
        //    optF.groupKey = 1234567; // unique number, for grouping in game
        //    // yield return optF;
        //    list.Add(optF);

        //    // Adding the base.GetCommands() when not empty
        //    IEnumerable<Command> baseList = base.GetGizmos();
        //    if (baseList != null)
        //        return list.AsEnumerable<Command>().Concat(baseList);
        //    else
        //        return list.AsEnumerable<Command>();
        //}



    }
}
