using Verse;
using UnityEngine;
using HarmonyLib;

namespace KeyzLocalModAlert;

public class KeyzLocalModAlertMod : Mod
{

    public KeyzLocalModAlertMod(ModContentPack content) : base(content)
    {
        Log.Message("Hello world from KeyzLocalModAlert");
#if DEBUG
        Harmony.DEBUG = true;
#endif
        Harmony harmony = new Harmony("keyz182.rimworld.KeyzLocalModAlert.main");	
        harmony.PatchAll();
    }
}
