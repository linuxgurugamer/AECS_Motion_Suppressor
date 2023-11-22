using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using UnityEngine;
using static AECS_Motion_Suppressor.AECS_VesselModule;

namespace AECS_Motion_Suppressor
{
    public class ModuleCSToggle : PartModule
    {
        [KSPAction("Toggle All")]
        public void toggleControlSurfaces(KSPActionParam param)
        {
            if (controlActive)
                disableGlobal(true);
            else
                enableGlobal(true);
        }

        [KSPAction("Disable All")]
        public void disableControlSurfaces(KSPActionParam param)
        {
            disableGlobal(true);
        }

        [KSPAction("Enable All")]
        public void enableControlSurfaces(KSPActionParam param)
        {
            enableGlobal(true);
        }

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        bool controlActive = true;

        //[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        //float authority; // value of the authority limiter if control surface is enabled

        float GetAuthorityList(int x)
        {
            float rc = 0;
            if (x == 0) rc = authority1;
            if (x == 1) rc = authority2;
            if (x == 2) rc = authority3;
            return rc;
        }

        void SetAuthorityList(int x, float value)
        {
            if (x == 0)  authority1 = value;
            if (x == 1)  authority2 = value;
            if (x == 2)  authority3 = value;
        }

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        float authority1; // value of the authority limiter if control surface is enabled

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        float authority2; // value of the authority limiter if control surface is enabled

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        float authority3; // value of the authority limiter if control surface is enabled


        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        bool wasInAtmo; // Used for checking whether  an atmosphere change happened when vessel unloaded, only used in start


        public ModuleControlSurface cs;
        public List<ModuleControlSurface> csList;
        public bool isModuleAeroSurface = false;

        //[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        //float ctrlSurfaceRange;


        float GetCtrlSurfaceRangeList(int x)
        {
            float rc = 0;
            if (x == 1) rc = ctrlSurfaceRange1;
            if (x == 2) rc = ctrlSurfaceRange2;
            if (x == 3) rc = ctrlSurfaceRange3;
            return rc;
        }

        void SetCtrlSurfaceRangeList(int x, float value)
        {
            if (x == 1) ctrlSurfaceRange1 = value;
            if (x == 2) ctrlSurfaceRange2 = value;
            if (x == 3) ctrlSurfaceRange3 = value;
        }

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        float ctrlSurfaceRange1;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        float ctrlSurfaceRange2;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)]
        float ctrlSurfaceRange3;


        bool setupCalled = false;
        static List<ModuleCSToggle> toggles;

        List<BaseField> csFields;


        public void setup()
        {

            //Set up atmoshpere change listener

            //Debug.Log("setup called");
            cs = GetComponent<ModuleAeroSurface>() as ModuleControlSurface;
                csList = new List<ModuleControlSurface> ();
            if (cs != null)
            {
                isModuleAeroSurface = true;
                csList.Add(cs);
            }
            else
            {
                cs = GetComponent<ModuleControlSurface>(); //get the parts control surface module
                csList = GetComponents<ModuleControlSurface>().ToList();
            }
            if (cs == null)
                return;
                
            //if (cs.ctrlSurfaceRange != 0)
            //    ctrlSurfaceRange = cs.ctrlSurfaceRange;

            if (toggles == null)
            { //if necessary, initialize static list
                toggles = new List<ModuleCSToggle>();
            }

            toggles.Add(this);

            removeNullToggles(); // Clean up elements from previous iterations of the list

            csFields = new List<BaseField>();

            //if (cs.authorityLimiter != 0)
            //{ //We don't want to override authority if the surface is disabled
            //    authority = cs.authorityLimiter;
            //}

            for (var i = 0; i < csList.Count; i++)
            {
                if (csList[i].ctrlSurfaceRange != 0)
                    SetCtrlSurfaceRangeList(i, csList[i].ctrlSurfaceRange);
                    
                if (csList[i].authorityLimiter != 0)
                { //We don't want to override authority if the surface is disabled
                    SetAuthorityList(i, csList[i].authorityLimiter);
                }
            }

            if (!controlActive)
            { //Needed to disable the appropriate gui event buttons
                disableCS();
            }
            else
            {
                enableCS();
            }

            //bool inAtmo = FlightGlobals.ActiveVessel.atmDensity != 0;
            //if (inAtmo && !wasInAtmo) { //If we changed atmosphere state while vessel was not loaded, enable / disable control surface
            //	enableCS();
            //}
            //if (!inAtmo && wasInAtmo) {
            //	disableCS();
            //}


            GameEvents.onVesselSituationChange.Add(UpdateCurrentAtmosphereState);


            setupCalled = true; //Setup done
        }

        public override void OnStart﻿(StartState state)
        {

            if (!setupCalled)
            {
                setup();
            }
        }


        void OnDestroy()
        {
            GameEvents.onVesselSituationChange.Remove(UpdateCurrentAtmosphereState);
        }


        public void UpdateCurrentAtmosphereState(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> data)
        {
            wasInAtmo = FlightGlobals.ActiveVessel.atmDensity != 0; //updating wasInAtmo so it gets saved correctly
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Activate Control Surface")]
        public void EventEnable()
        {
            foreach (Part p in part.symmetryCounterparts)
            {
                if (p.GetComponent<ModuleCSToggle>() != null)
                {
                    p.GetComponent<ModuleCSToggle>().enableCS();
                }
            }
            enableCS();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Deactivate Control Surface")]
        public void EventDisable()
        {
            foreach (Part p in part.symmetryCounterparts)
            {
                if (p.GetComponent<ModuleCSToggle>() != null)
                {
                    p.GetComponent<ModuleCSToggle>().disableCS();
                }
            }
            disableCS();
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Activate All")]
        public void EventEnableAll()
        {
            enableGlobal(true);
        }

        [KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "Deactivate All")]
        public void EventDisableAll()
        {
            disableGlobal(true);
        }

        public static void enableGlobal(bool printMessage)
        {
            if (toggles == null) //List not initialized because we don't have any control surfaces, and haven't launched a vessel with control surfaces
                return;
            if (!vesselHasControlSurface())
                return;

            if (printMessage)
            {
                ScreenMessages.PostScreenMessage("Enabled all Control Surfaces");
            }
            foreach (ModuleCSToggle ct in toggles)
            {
                if (ct != null)
                {
                    ct.enableCS();
                }
            }
        }

        public static void disableGlobal(bool printMessage)
        {
            if (toggles == null)
                return;
            if (!vesselHasControlSurface())
                return;

            if (printMessage)
            {
                ScreenMessages.PostScreenMessage("Disabled all Control Surfaces");
            }
            foreach (ModuleCSToggle ct in toggles)
            {
                if (ct != null)
                {
                    ct.disableCS();
                }
            }
        }

        public static bool vesselHasControlSurface()
        {

            foreach (Part p in FlightGlobals.ActiveVessel.parts)
            { // check if any part of the active vessel is a control surface
                if (p.GetComponent<ModuleCSToggle>() != null || p.GetComponentInChildren<ModuleCSToggle>() != null)
                {
                    return true;
                }
            }
            return false;
        }

        public void toggleCS()
        {
            if (controlActive)
            {
                disableCS();
            }
            else
            {
                enableCS();
            }
        }

        public void setEventVisibility()
        {

            Events["EventEnable"].active = !controlActive;
            Events["EventEnableAll"].active = !controlActive;
            Events["EventDisable"].active = controlActive;
            Events["EventDisableAll"].active = controlActive;
        }


        public void enableCS()
        {
            //Debug.Log("enabled " + part.name);
            controlActive = true;
            setEventVisibility();

            for (var i = 0; i < csList.Count; i++)
            {
                csList[i].authorityLimiter = GetAuthorityList(i);
                if (isModuleAeroSurface)
                    csList[i].ctrlSurfaceRange = GetCtrlSurfaceRangeList(i);
            }

            //cs.authorityLimiter = authority;
            //if (isModuleAeroSurface)
            //    cs.ctrlSurfaceRange = ctrlSurfaceRange;


            for (int i = 0; i < csFields.Count; i++)
            { //Reactivate disabled fields
                csFields[i].guiActive = true;
            }
        }

        public void disableCS()
        {
            //Debug.Log("disabled "+part.name);

            controlActive = false;
            setEventVisibility();

            //if (cs.authorityLimiter != 0)
            //{ //avoid overwriting value if already disabled
            //    authority = cs.authorityLimiter;
            //}
            cs.authorityLimiter = 0;
            //if (isModuleAeroSurface)
            //{
            //    if (cs.ctrlSurfaceRange != 0)
            //        ctrlSurfaceRange = cs.ctrlSurfaceRange;
            //
            //    cs.ctrlSurfaceRange = 0;
            //}

            for (int i = 0; i < csList.Count; i++)
            {
                if (csList[i].authorityLimiter != 0)
                { //avoid overwriting value if already disabled
                    SetAuthorityList(i, csList[i].authorityLimiter);                    
                }
                csList[i].authorityLimiter = 0;
                if (isModuleAeroSurface)
                {
                    if (csList[i].ctrlSurfaceRange != 0)
                        SetCtrlSurfaceRangeList(i, csList[i].ctrlSurfaceRange);
                    csList[i].ctrlSurfaceRange = 0;
                }
            }
            for (int i = 0; i < cs.Fields.Count; i++)
            { // Save which fields have been disabled, so the correct one's will be enabled
                if (cs.Fields[i].guiActive)
                {
                    cs.Fields[i].guiActive = false;
                    if (!csFields.Contains(cs.Fields[i]))
                    { //Avoid duplicates
                        csFields.Add(cs.Fields[i]);
                    }
                }
            }
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
