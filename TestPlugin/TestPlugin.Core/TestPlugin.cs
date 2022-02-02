using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
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

        public static ConfigEntry<KeyboardShortcut> TriggerTextureCompute { get; private set; }  

        internal void Start()
        {
            Logger = base.Logger;    
            CharacterApi.RegisterExtraBehaviour<TestPluginCharaController>(GUID);      

            var hi = new Harmony(GUID);
            Hooks.InitHooks(hi);

            //Keybind for triggering texture compute
            TriggerTextureCompute  = Config.Bind<KeyboardShortcut>("Keybinds", "Trigger Texture Compute", new KeyboardShortcut(),
                new ConfigDescription("Rquires that the correct shader be loaded to the character's body"));
        }    
    }

}
