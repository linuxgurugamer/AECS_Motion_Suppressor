using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;

using KSP_Log;

namespace AECS_Motion_Suppressor
{
    internal class AECS_VesselModule : VesselModule
    {
        internal List<ModuleEngineGimbal> toggles;

        internal static Log Log;

        new void Start()
        {
#if DEBUG
            if (Log == null)
                Log = new Log("AECS_Motion_Suppressor", Log.LEVEL.INFO);
#else
            if (Log == null)
                Log = new Log("AECS_Motion_Suppressor", Log.LEVEL.ERROR);
#endif
            Log.Info("AECS_VesselModule.Start");

            base.Start();
            GameEvents.onVesselPartCountChanged.Add(OnVesselPartCountChanged);
            GameEvents.onVesselChange.Add(OnVesselChange);
            GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);

            GameEvents.onEngineThrustPercentageChanged.Add(onEngineThrustPercentageChanged);
            GameEvents.onChangeEngineDVIncludeState.Add(onChangeEngineDVIncludeState);
            GameEvents.onEngineActiveChange.Add(onEngineActiveChange);
            GameEvents.onMultiModeEngineSwitchActive.Add(onMultiModeEngineSwitchActive);

            StartCoroutine(SlowUpdate());
        }
        IEnumerator SlowUpdate()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);
                if (this.vessel == FlightGlobals.ActiveVessel)
                    checkAllFuelFlow();

            }
        }
        public void OnDestroy()
        {
            GameEvents.onVesselPartCountChanged.Remove(OnVesselPartCountChanged);
            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);

            GameEvents.onEngineThrustPercentageChanged.Remove(onEngineThrustPercentageChanged);
            GameEvents.onChangeEngineDVIncludeState.Remove(onChangeEngineDVIncludeState);
            GameEvents.onEngineActiveChange.Remove(onEngineActiveChange);
            GameEvents.onMultiModeEngineSwitchActive.Remove(onMultiModeEngineSwitchActive);
        }

        void onEngineThrustPercentageChanged(ModuleEngines me)
        {
            Log.Info("onEngineThrustPercentageChanged");
            checkAllFuelFlow();
        }

        void onChangeEngineDVIncludeState(ModuleEngines me)
        {
            Log.Info("onChangeEngineDVIncludeState");
            checkAllFuelFlow();
        }
        void onEngineActiveChange(ModuleEngines me)
        {
            Log.Info("onEngineActiveChange");
            checkAllFuelFlow();
        }

        void onMultiModeEngineSwitchActive(MultiModeEngine mme)
        {
            Log.Info("onMultiModeEngineSwitchActive");
            checkAllFuelFlow();
        }

        public void checkAllFuelFlow()
        {
            if (toggles == null)
                return;
            Log.Info("checkAllFuelFlow, toggles.Count:" + toggles.Count);

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


        void OnVesselPartCountChanged(Vessel v)
        {
            Log.Info("OnVesselPartCountChanged, vessel: " + v.vesselName + ", partCount: " + v.Parts.Count);
            RebuildToggle(v);
        }

        void OnVesselChange(Vessel v)
        {
            Log.Info("OnVesselChange");
            RebuildToggle(v);
        }

        void OnVesselGoOffRails(Vessel v)
        {
            Log.Info("OnVesselGoOffRails");
            RebuildToggle(v);
        }


        void RebuildToggle(Vessel v)
        {
            if (v != FlightGlobals.ActiveVessel)
            {
                Log.Info("RebuildToggle, v.Name: " + v.vesselName + ", ActiveVessel.name: " + FlightGlobals.ActiveVessel.vesselName);
                return;
            }
            Log.Info("RebuildToggle, vessel: " + v.name);
            List<ModuleEngineGimbal> meg = v.FindPartModulesImplementing<ModuleEngineGimbal>().ToList();
            if (toggles == null)
            { 
                toggles = new List<ModuleEngineGimbal>();
            }
            else
                toggles.Clear();
            foreach (var m in meg)
            {
                toggles.Add(m);
            }
        }
    }
}
