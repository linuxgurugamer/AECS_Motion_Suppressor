using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;


namespace AECS_Motion_Suppressor
{

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class EngineGimbalController : MonoBehaviour
    {
        void Start()
        {
            // Debug.Log("EngineGimbalController.Start");
            StartCoroutine(SlowUpdate());
        }

        IEnumerator SlowUpdate()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.2f);

                if (FlightInputHandler.state.mainThrottle == 0)
                {
                    ModuleEngineGimbal.DisableGlobal(false);
                }
                else
                {
                    ModuleEngineGimbal.checkAllFuelFlow();
                }
            }
        }
    }

    public class ModuleEngineGimbal : PartModule
    {

        bool setupCalled = false;
        static List<ModuleEngineGimbal> toggles;

        [KSPField(isPersistant = true, guiName = "Engine Thrust", guiActive = true, guiActiveEditor = true)]
        float engineFlow;

        List<ModuleEngines> engineModuleList;
        List<ModuleEnginesFX>  engineFxModuleList;
        ModuleGimbal gimbalModule;

        

        public void setup()
        {
            gimbalModule = part.FindModuleImplementing<ModuleGimbal>();

            engineModuleList = part.FindModulesImplementing<ModuleEngines>().ToList() ;

            engineFxModuleList = part.FindModulesImplementing<ModuleEnginesFX>().ToList();

            if (toggles == null)
            { //if necessary, initialize static list
                toggles = new List<ModuleEngineGimbal>();
            }

            toggles.Add(this);
            removeNullToggles(); // Clean up elements from previous iterations of the list

            setupCalled = true;
        }

        public override void OnStart﻿(StartState state)
        {

            if (!setupCalled)
            {
                setup();
            }
        }

        public static void checkAllFuelFlow()
        {
            if (toggles == null)
                return;

            for (int i = toggles.Count - 1; i >= 0; i--)
            {
                ModuleEngineGimbal meg = toggles[i];

                if (meg != null)
                {
                    meg.engineFlow = 0;

                    if (meg.engineModuleList != null)
                    {
                        for (int j = meg.engineModuleList.Count - 1; j >= 0; j--)
                        {
                            meg.engineFlow = Math.Max(meg.engineFlow, meg.engineModuleList[j].fuelFlowGui);
                        }
                    }
                    if (meg.engineFxModuleList != null)
                    {
                        for (int j = meg.engineFxModuleList.Count - 1; j >= 0; j--)
                        {
                            meg.engineFlow = Math.Max(meg.engineFlow, meg.engineFxModuleList[j].fuelFlowGui);
                        }
                    }
                    meg.setGimbal(meg.engineFlow > 0.000001f);
                }
            }
        }

        public static void enableGlobal(bool printMessage)
        {
            if (toggles == null) //List not initialized because we don't have any engines, and haven't launched a vessel with engines
                return;

            if (printMessage)
                ScreenMessages.PostScreenMessage("Enabled all Engine Gimbaling");

            for (int i = toggles.Count - 1; i >= 0; i--)
            {
                ModuleEngineGimbal meg = toggles[i];
                if (meg != null)
                {
                    meg.setGimbal(true);
                }
            }
        }

        public static void DisableGlobal(bool printMessage)
        {
            if (toggles == null)
                return;

            if (printMessage)
                ScreenMessages.PostScreenMessage("Disabled all Engine Gimbaling");

            for (int i = toggles.Count - 1; i >= 0; i--)
            {
                ModuleEngineGimbal meg = toggles[i];
            
                if (meg != null)
                {
                    meg.setGimbal(false);
                }
            }
        }
        void setGimbal(bool b)
        {
            gimbalModule.gimbalActive = b;

        }

        static void removeNullToggles()
        {
            if (toggles == null) //Should technically never occur
                return;

            for (int i = toggles.Count - 1; i >= 0; i--)
            { //going from back to front removing instances that belonged to other vessels
                if (toggles[i] == null)
                {
                    toggles.RemoveAt(i);
                }
            }
        }
    }
}
