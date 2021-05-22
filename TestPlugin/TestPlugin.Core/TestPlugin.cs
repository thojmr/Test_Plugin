using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KKAPI;
using KKAPI.Chara;
using UnityEngine;
#if KK
    using KKAPI.MainGame;
#elif HS2
    using AIChara;
#elif AI
    using AIChara;
#endif

namespace KK_TestPlugin
{       
    [BepInPlugin(GUID, GUID, Version)]
    public partial class TestPlugin : BaseUnityPlugin
    {        
        public const string GUID = "Thojm_TestPlugin";
        public const string Version = "0.1";
        internal static new ManualLogSource Logger { get; private set; }        

        internal void Start()
        {
            Logger = base.Logger;    
            CharacterApi.RegisterExtraBehaviour<TestPluginCharaController>(GUID);      
        }    
    }

    public class TestPluginCharaController: CharaCustomFunctionController
    {  

        //To reproduce in HS2
        //  Open Maker and select any character
        //  See that the output is different for the method called by update vs. coroutine
        public bool triggerMeasureFromUpdate = false;

        protected override void OnReload(GameMode currentGameMode)
        {
            TestPlugin.Logger.LogWarning($" Reload() Started");
            StartCoroutine(ITakeMeasurement());
        }

        protected override void Update()
        {
            if (triggerMeasureFromUpdate)
            {
                TakeMeasurement("Update");
                triggerMeasureFromUpdate = false;
            }
        }

        public IEnumerator ITakeMeasurement() 
        {
            //Wait 2 seconds after Reload() is triggered 
            yield return new WaitForSeconds(2f);

            //Take measurement from coroutine
            TakeMeasurement("ITakeMeasurement");

            //trigger Update() to also take measurement at the same time (roughly)
            triggerMeasureFromUpdate = true;
        }

        public void TakeMeasurement(string triggeredBy) 
        {
            var thighLName = "cf_J_LegUp00_L";
            var thighRName = "cf_J_LegUp00_R";

            //Get the characters hip bones to measure distance
            var thighLBone = ChaControl.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == thighLName);
            var thighRBone = ChaControl.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name == thighRName);
            if (thighLBone == null || thighRBone == null) return;
            
            //Measures Left to right hip bone distance and logs result
            var waistWidth = Vector3.Distance(thighLBone.transform.InverseTransformPoint(thighLBone.position), thighLBone.transform.InverseTransformPoint(thighRBone.position));

            // WHY IS THE MEASUREMENT DIFFERENT FROM COROUTINE THAN FROM UPDATE!
            TestPlugin.Logger.LogWarning($" MeasureWaist result from {triggeredBy} : {waistWidth}");
        }










        //Ignore
        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {

        }

        protected override void Start() 
        { 
            base.Start();
        }
    }
}
