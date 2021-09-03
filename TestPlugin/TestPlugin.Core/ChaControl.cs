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
    public class TestPluginCharaController: CharaCustomFunctionController
    {  

        //Ignore
        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {

        }

        protected override void Start() 
        { 
            base.Start();
        }

        protected override void OnReload(GameMode currentGameMode)
        {
            TestPlugin.Logger.LogWarning($" Reload() Started");
        }

        protected override void Update()
        {

        }


    }
}