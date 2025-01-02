using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace KeyzLocalModAlert.HarmonyPatches;

[HarmonyPatch(typeof(UIRoot_Entry))]
public static class UIRoot_Entry_Patch
{
    public static List<ModDetails> ActiveMods;
    public static Vector2 scrollPosition = Vector2.zero;

    public class ModDetails
    {
        public ModMetaData Mod;
        public string VersionDetails;
        public string ErrorDetails;
        public string CurrentBranch;
        public bool ValidGit = false;
        public bool OutOfSync = false;
        public bool ProcessingDone = false;

        public ModDetails(ModMetaData mod)
        {
            Mod = mod;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                ValidGit = GitOps.CheckGit(mod.RootDir, out ErrorDetails, out VersionDetails,  out CurrentBranch, out OutOfSync);
                ProcessingDone = true;
            });
        }
    }

    [HarmonyPatch(nameof(UIRoot_Entry.Init))]
    [HarmonyPostfix]
    public static void InitPatch(UIRoot_Entry __instance)
    {
        LongEventHandler.QueueLongEvent(delegate
        {
            ActiveMods = ModLister.AllInstalledMods.Where(mod=>mod.Active && !mod.Official && !mod.OnSteamWorkshop).Select(mod=>new ModDetails(mod)).ToList();
        }, "KeyzLocalModAlert_Task", true, null);

    }

    [HarmonyPatch("DoMainMenu")]
    [HarmonyPostfix]
    public static void DoMainMenuPatch(UIRoot_Entry __instance)
    {
        Rect panelRect = new Rect(10,100, 300f, 450f);

        GUI.color = new Color(1f, 1f, 1f, 1f);
        Widgets.DrawShadowAround(panelRect);
        GUI.color = Color.white;

        Rect labelRect = new Rect(panelRect.x + 10, panelRect.y + 10, panelRect.width - 10, 30f);
        Widgets.Label(labelRect, "Local mod git status");

        float rowSize = 60f;

        float scrollingViewHeight = ActiveMods.Count * rowSize;

        Rect scrollingView = new Rect(0f,0f,panelRect.width - 10f - 30f, scrollingViewHeight);
        Rect scrollContainer = panelRect.ContractedBy(10f);
        scrollContainer.yMin += 40f;

        if (!GitOps.IsGitAvailable(out string error))
        {
            Widgets.Label(scrollContainer, $"<color=yellow>Git Not Found</color>\n{error}");
            return;
        }

        Widgets.BeginScrollView(scrollContainer, ref scrollPosition, scrollingView);

        try
        {
            float x = scrollingView.x;
            float y = scrollingView.y;
            float labelHeight = (rowSize - 15f) / 2f;

            foreach (ModDetails mod in ActiveMods.OrderByDescending(m=>m.OutOfSync))
            {
                try
                {
                    Widgets.Label(new Rect(5f, y + 5, 250f, labelHeight), mod.Mod.Name);
                    if (!mod.ProcessingDone)
                    {
                        Color col =GUI.color;
                        GUI.color = Color.green;
                        Widgets.Label(new Rect(5f, y + 10 + labelHeight, 250f, labelHeight), "Processing...");
                        GUI.color = col;
                    }
                    else if(mod.ValidGit){
                        Widgets.Label(new Rect(5f, y + 10 + labelHeight, 150f, labelHeight), mod.VersionDetails);
                        Widgets.Label(new Rect(160f, y + 10 + labelHeight, 80f, labelHeight), mod.CurrentBranch);
                    }
                    else
                    {
                        Color col =GUI.color;
                        GUI.color = Color.red;
                        Widgets.Label(new Rect(5f, y + 10 + labelHeight, 250f, labelHeight), mod.ErrorDetails);
                        GUI.color = col;
                    }
                }finally{
                    y += rowSize;

                    Widgets.DrawLine(new Vector2(x+10f, y), new Vector2(x+240f, y), Color.gray, 1f);
                }
            }
        }
        finally
        {
            Widgets.EndScrollView();
        }
    }
}
