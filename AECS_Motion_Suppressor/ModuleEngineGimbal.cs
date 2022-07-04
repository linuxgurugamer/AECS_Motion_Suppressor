using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;

using static AECS_Motion_Suppressor.AECS_VesselModule;

namespace AECS_Motion_Suppressor
{
#if false
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class EngineGimbalController : MonoBehaviour
    {
        void Start()
        {
            GameEvents.onVesselPartCountChanged.Add(OnVesselPartCountChanged);
            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);

            StartCoroutine(SlowUpdate());
        }


        public void Destroy()
        {
            GameEvents.onVesselPartCountChanged.Remove(OnVesselPartCountChanged);
            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);
        }


        void RebuildToggle(Vessel v)
        {
            List<ModuleEngineGimbal> meg = v.FindPartModulesImplementing<ModuleEngineGimbal>().ToList();
            foreach (var m in meg)
                m.Start();
        }

        void OnVesselPartCountChanged(Vessel v)
        {
            RebuildToggle(v);
        }

        void OnVesselChange(Vessel v)
        {
            RebuildToggle(v);
        }

        void OnVesselGoOffRails(Vessel v)
        {
            RebuildToggle(v);
        }

        IEnumerator SlowUpdate()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.25f);
                //Debug.Log("EngineGimbalController.SlowUpdate, globalStatus: " + ModuleEngineGimbal.globalStatus);
                if (ModuleEngineGimbal.globalStatus == ModuleEngineGimbal.GlobalStatus.none)
                {
#if false
                    if (FlightInputHandler.state.mainThrottle == 0)
                    {
                        //Debug.Log("mainThrottle == 0");
                        ModuleEngineGimbal.DisableGlobal(false);
                    }
                    else
#endif
                    {
                        ModuleEngineGimbal.checkAllFuelFlow();
                    }
                }
            }
        }
    }
#endif

    public class ModuleEngineGimbal : PartModule
    {
        internal enum GlobalStatus { none, disabled, enabled };

        static internal GlobalStatus globalStatus = GlobalStatus.none;

        [KSPAction("Reset All")]
        public void toggleControlSurfaces(KSPActionParam param)
        {
            globalStatus = GlobalStatus.none;
        }

        [KSPAction("Disable All")]
        public void disableControlSurfaces(KSPActionParam param)
        {
            DisableGlobal(false);
            globalStatus = GlobalStatus.disabled;
        }

        [KSPAction("Enable All")]
        public void enableControlSurfaces(KSPActionParam param)
        {
            if (vesselModule.toggles == null)
            {
                Log.Info("ModuleEngineGimbal.enableControlSurfaces, vesselModule.toggles is null");
            }
            //Start();

            EnableGlobal();
            globalStatus = GlobalStatus.enabled;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Reset All")]
        public void EventResetAll()
        {
            globalStatus = GlobalStatus.none;
        }
        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Activate All")]
        public void EventEnableAll()
        {
            if (vesselModule.toggles == null)
            {
                Log.Info("ERROR: ModuleEngineGimbal.EventEnableAll, tvesselModule.togglesoggles is null");
                Start();
            }
            EnableGlobal();
            globalStatus = GlobalStatus.enabled;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Deactivate All")]
        public void EventDisableAll()
        {
            if (vesselModule.toggles == null)
            {
                Log.Info("ERROR: ModuleEngineGimbal.EventDisableAll, vesselModule.toggles is null");
                Start();
            }

            DisableGlobal(false);
            globalStatus = GlobalStatus.disabled;
        }

        internal bool activeFlag;
        internal bool setupCalled = false;

        //internal static List<ModuleEngineGimbal> toggles;

        [KSPField(isPersistant = true, guiName = "Engine Thrust", guiActive = true, guiActiveEditor = true)]
        internal float engineFlow;

        internal List<ModuleEngines> engineModuleList;
        internal List<ModuleEnginesFX> engineFxModuleList;
        ModuleGimbal gimbalModule;


        AECS_VesselModule vesselModule;

        internal void Start()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            gimbalModule = part.FindModuleImplementing<ModuleGimbal>();

            engineModuleList = part.FindModulesImplementing<ModuleEngines>().ToList();

            engineFxModuleList = part.FindModulesImplementing<ModuleEnginesFX>().ToList();

            vesselModule = vessel.FindVesselModuleImplementing<AECS_VesselModule>();

#if false
            if (toggles == null)
            { //if necessary, initialize static list
                toggles = new List<ModuleEngineGimbal>();
            }

            toggles.Add(this);
            removeNullToggles(); // Clean up elements from previous iterations of the list
#endif

            setupCalled = true;
            activeFlag = !gimbalModule.gimbalLock;

#if false
           GameEvents.onEngineThrustPercentageChanged.Add(onEngineThrustPercentageChanged);
            GameEvents.onEngineActiveChange.Add(onEngineActiveChange);
            GameEvents.onMultiModeEngineSwitchActive.Add(onMultiModeEngineSwitchActive);
#endif
        }

        public void Destroy()
        {
#if false
            GameEvents.onEngineThrustPercentageChanged.Remove(onEngineThrustPercentageChanged);
            GameEvents.onEngineActiveChange.Remove(onEngineActiveChange);
            GameEvents.onMultiModeEngineSwitchActive.Remove(onMultiModeEngineSwitchActive);
#endif
        }

#if false
        void onEngineThrustPercentageChanged(ModuleEngines me)
        {
            checkAllFuelFlow();
        }

        void onEngineActiveChange(ModuleEngines me)
        {
            checkAllFuelFlow();
        }

        void onMultiModeEngineSwitchActive(MultiModeEngine mme)
        {
            checkAllFuelFlow();
        }
#endif
        public void checkAllFuelFlow()
        {
            if (vesselModule.toggles == null)
                return;

            for (int i = vesselModule.toggles.Count - 1; i >= 0; i--)
            {
                ModuleEngineGimbal meg = vesselModule.toggles[i];

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

        public void enableGlobal(bool printMessage)
        {
            if (vesselModule.toggles == null) //List not initialized because we don't have any engines, and haven't launched a vessel with engines
                return;

            if (printMessage)
            {
                ScreenMessages.PostScreenMessage("Enabled all Engine Gimbaling");
                Log.Info("enableGlobal, Enabled all Engine Gimbaling");
            }

            for (int i = vesselModule.toggles.Count - 1; i >= 0; i--)
            {
                ModuleEngineGimbal meg = vesselModule.toggles[i];
                if (meg != null)
                {
                    meg.setGimbal(true);
                }
            }
        }

        public void DisableGlobal(bool printMessage)
        {
            if (vesselModule.toggles == null)
            {
                Log.Info("ERROR: ModuleEngineGimbal.disableGlobal, vesselModule.toggles is null");
                return;
            }

            if (printMessage)
            {
                ScreenMessages.PostScreenMessage("Disabled all Engine Gimbaling");
                Log.Info("DisableGlobal, Disabled all Engine Gimbaling");
            }
            for (int i = vesselModule.toggles.Count - 1; i >= 0; i--)
            {
                ModuleEngineGimbal meg = vesselModule.toggles[i];

                if (meg != null && meg.activeFlag)
                {
                    meg.setGimbal(false);
                }
            }
        }

        public void EnableGlobal()
        {
            Log.Info("Enableglobal");
            if (vesselModule.toggles == null)
            {
                Log.Info("ERROR: ModuleEngineGimbal.EnableGlobal vesselModule.toggles is null");
                return;
            };
            for (int i = vesselModule.toggles.Count - 1; i >= 0; i--)
            {
                ModuleEngineGimbal meg = vesselModule.toggles[i];

                if (meg != null && !meg.activeFlag)
                {
                    meg.setGimbal(true);
                }
            }
        }

        internal void setGimbal(bool active)
        {
            if (!active)
            {
                if (!delayedGimbalLockActive && !gimbalModule.gimbalLock)
                {
                    Log.Info("setGimbal: false, part: " + this.part.partInfo.title);
                    StartCoroutine(DoDelayedGimbalLock());
                }
            }
            else
            {
                if (gimbalModule.gimbalLock)
                {
                    Log.Info("setGimbal: true, part: " + this.part.partInfo.title);
                    if (delayedGimbalLockActive)
                    {
                        Log.Info("setGimbal: Stopping delayedGimbalLock, part: " + this.part.partInfo.title);
                        StopCoroutine(DoDelayedGimbalLock());
                    }
                    gimbalModule.gimbalLock = !active;
                    activeFlag = !gimbalModule.gimbalLock;
                    delayedGimbalLockActive = false;
                }
            }
        }

        bool delayedGimbalLockActive = false;
        IEnumerator DoDelayedGimbalLock()
        {
            delayedGimbalLockActive = true;

            yield return new WaitForSeconds(5f);
            Log.Info("DoDelayedGimbalLock, setting gimbalLock true for: " + part.partInfo.title);
            gimbalModule.gimbalLock = true;
            activeFlag = !gimbalModule.gimbalLock;

            delayedGimbalLockActive = false;
        }

#if false
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
#endif

    }
}
