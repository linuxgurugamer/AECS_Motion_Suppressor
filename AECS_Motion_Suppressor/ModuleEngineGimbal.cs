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
            //Debug.Log("EngineGimbalController.Start");
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
            ModuleEngineGimbal.DisableGlobal(false);
            globalStatus = GlobalStatus.disabled;
        }

        [KSPAction("Enable All")]
        public void enableControlSurfaces(KSPActionParam param)
        {
            if (toggles == null)
                Start();

            ModuleEngineGimbal.EnableGlobal();
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
            if (toggles == null)
            {
                Debug.Log("ERROR: ModuleEngineGimbal.EventEnableAll, toggles is null");
                Start();
            }
            ModuleEngineGimbal.EnableGlobal();
            globalStatus = GlobalStatus.enabled;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Deactivate All")]
        public void EventDisableAll()
        {
            if (toggles == null)
            {
                Debug.Log("ERROR: ModuleEngineGimbal.EventDisableAll, toggles is null");
                Start();
            }

            ModuleEngineGimbal.DisableGlobal(false);
            globalStatus = GlobalStatus.disabled;
        }

        internal bool activeFlag;
        internal bool setupCalled = false;

        internal static List<ModuleEngineGimbal> toggles;

        [KSPField(isPersistant = true, guiName = "Engine Thrust", guiActive = true, guiActiveEditor = true)]
        float engineFlow;

        List<ModuleEngines> engineModuleList;
        List<ModuleEnginesFX>  engineFxModuleList;
        ModuleGimbal gimbalModule;



        internal void Start()
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
            activeFlag = !gimbalModule.gimbalLock;
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
            {
                Debug.Log("ERROR: ModuleEngineGimbal.disableGlobal, toggles is null");
                return;
            }

            if (printMessage)
                ScreenMessages.PostScreenMessage("Disabled all Engine Gimbaling");

            for (int i = toggles.Count - 1; i >= 0; i--)
            {
                ModuleEngineGimbal meg = toggles[i];
            
                if (meg != null && meg.activeFlag)
                {
                    meg.setGimbal(false);
                }
            }
        }

        public static void EnableGlobal()
        {
            //Debug.Log("Enableglobal");
            if (toggles == null)
            {
                Debug.Log("ERROR: ModuleEngineGimbal.EnableGlobal toggles is null");
                return;
            };
            for (int i = toggles.Count - 1; i >= 0; i--)
            {
                ModuleEngineGimbal meg = toggles[i];

                if (meg != null && !meg.activeFlag)
                {
                    meg.setGimbal(true);
                }
            }
        }

        void setGimbal(bool b)
        {
            gimbalModule.gimbalLock = !b;
            activeFlag = !gimbalModule.gimbalLock;
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
