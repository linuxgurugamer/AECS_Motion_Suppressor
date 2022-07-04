﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;

namespace AECS_Motion_Suppressor {

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class CSToggleController : MonoBehaviour{

        bool wasInAtmo = true;

		void Start() {
			if (FlightGlobals.ActiveVessel == null) {
				Debug.LogError("ToggleController Start method; ActiveVessel still Null");
			}
			wasInAtmo = FlightGlobals.ActiveVessel.atmDensity != 0;
			GameEvents.onVesselSituationChange.Add(UpdateCurrentAtmosphereState);

		}

		void OnDestroy() {
			GameEvents.onVesselSituationChange.Remove(UpdateCurrentAtmosphereState);

		}

		public void UpdateCurrentAtmosphereState(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> data) {

			if (FlightGlobals.ActiveVessel == null) 
                return;

            bool inAtmo = FlightGlobals.ActiveVessel.atmDensity != 0;

            if (wasInAtmo && !inAtmo) {
                ModuleCSToggle.disableGlobal(true);
            }
            if (!wasInAtmo && inAtmo) {
                ModuleCSToggle.enableGlobal(true);
            }
            wasInAtmo = inAtmo;
        }

    }
}
