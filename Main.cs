using BepInEx.Logging;
using HarmonyLib;
using Il2CppSystem.Runtime.InteropServices;
using Polytopia.Data;
using PolytopiaBackendBase.Game;
using UnityEngine;
using static SkinVisualsRenderer;

namespace MidjiwanTribeMod;

public static class Main
{
    public static ManualLogSource? modLogger;
    private static Dictionary<string, int> nameToHexColor = new()
    {
        {"midjiate", 3710081},
        {"midjifawr", 6735259},
        {"midjify", 10834247}, //1907997
        {"midjimod", 13682529},
        {"midjitone", 15827146}, 
        {"midjitoo", 6050382}, //15198183
        {"midjiwan", 6147244}, //5354648
        {"midjix", 7412856}, 
        {"zoythrus", 8742184}, // 16514891
        {"boat", 197379}, // 4576502
        {"ship", 197379},

		{"zoythrus_aimo", 3596970},
		{"zoythrus_aquarion", 15958913},
		{"zoythrus_bardur", 3482900},
		{"zoythrus_elyrion", 6711833},
		{"zoythrus_hoodrick", 10053120},
		{"zoythrus_imperius", 255},
		{"zoythrus_kickoo", 65280},
		{"zoythrus_luxidoor", 11221974},
		{"zoythrus_oumaji", 16776960},
		{"zoythrus_quetzali", 2579530},
		{"zoythrus_vengir", 16777215},
		{"zoythrus_xinxi", 13369344},
		{"zoythrus_yadakk", 8200988},
		{"zoythrus_zebasi", 16750848},
		{"zoythrus_polaris", 11968901},
		{"zoythrus_cymanti", 12778752},
    };

    public static void Load(ManualLogSource logger)
    {
        Harmony.CreateAndPatchAll(typeof(Main));
        modLogger = logger;
    }

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Unit), nameof(Unit.UpdateObject), new Type[] { })]
    [HarmonyPatch(typeof(Unit), nameof(Unit.UpdateObject), new Type[] { typeof(MapRenderContext), typeof(SkinVisualsTransientData) })]
	public static void UpdateObject(Unit __instance)
	{
        if (__instance == null)
        {
            return;
        }

		if (__instance.unitState == null)
        {
            return;
        }
        string unitName = EnumCache<UnitData.Type>.GetName(__instance.unitState.type);
        if(!nameToHexColor.ContainsKey(unitName))
            return;

        Color color = ColorUtil.ColorFromInt(nameToHexColor[unitName]);

        for (int i = 0; i < __instance.skinVisuals.visualParts.Length; i++)
        {
            SetTint(__instance.skinVisuals, color);
        }
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(UIUnitRenderer), nameof(UIUnitRenderer.CreateUnit))]
	public static bool CreateUnit(UIUnitRenderer  __instance)
	{
        string unitName = EnumCache<UnitData.Type>.GetName(__instance.unitType);
        if(nameToHexColor.ContainsKey(unitName))
        {
            __instance.tintColor = nameToHexColor[unitName];
        }
        return true;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(ActionUtils), nameof(ActionUtils.TrainUnit))]
	public static bool TrainUnit(ref UnitState __result, GameState gameState, PlayerState playerState, TileData tile, ref UnitData unitData)
    {
        if(GameManager.LocalPlayer == null)
            return true;
        if(GameManager.LocalPlayer.tribe != EnumCache<PolytopiaBackendBase.Common.TribeType>.GetType("midjiwan"))
            return true;
        if(unitData.type == UnitData.Type.Bunny && gameState.GameLogicData.TryGetData(EnumCache<UnitData.Type>.GetType("midjix"), out UnitData midjixData))
        {
            unitData = midjixData;
        }
        else if(unitData.type == UnitData.Type.Transportship)
        {
            if(gameState.GameLogicData.TryGetData(UnitData.Type.Ship, out UnitData shipData) &&
                gameState.GameLogicData.GetUnlockedUnits(GameManager.LocalPlayer, gameState, shouldIncludeHidden: true).Contains(shipData))
            {
                unitData = shipData;
            }
            else if(gameState.GameLogicData.TryGetData(UnitData.Type.Boat, out UnitData boatData))
            {
                unitData = boatData;
            }
        }
        var zoyType = EnumCache<UnitData.Type>.GetType("zoythrus");
        if(unitData.type == zoyType && gameState.GameLogicData.TryGetData(zoyType, out var data))
        {
            PolytopiaBackendBase.Common.TribeType subTribe = RandomFromPos(tile.coordinates.x, tile.coordinates.y, gameState.CurrentTurn);
            string tribeString = EnumCache<PolytopiaBackendBase.Common.TribeType>.GetName(subTribe).ToLower();
            if(!gameState.GameLogicData.TryGetData(EnumCache<UnitData.Type>.GetType($"zoythrus_{tribeString}"), out UnitData zoythrusData))
            {
                modLogger!.LogWarning($"Failed to get Zoythrus data for {tribeString}");
                return true;
            }

            unitData = zoythrusData;
        }
        return true;
    }

    private static PolytopiaBackendBase.Common.TribeType RandomFromPos(int x, int y, uint turn)
    {
        unchecked
        {
            int seed = 17;
            seed = seed * 31 + x;
            seed = seed * 31 + y;
            seed = seed * 31 + (int)turn;
            return (PolytopiaBackendBase.Common.TribeType)(new System.Random(seed).Next(0, 16) + 2);
        }
    }

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ClientInteraction), nameof(ClientInteraction.OnRelease))]
	private static void OnRelease(ClientInteraction __instance, Tile tile, int touchIndex = 0)
    {
        if (tile == null)
		{
			return;
		}
		int frameCount = Time.frameCount;
		if (__instance.lastTileClick != null)
		{
			if (tile.Coordinates == __instance.lastTileClick.Coordinates && !tile.Data.IsWater && tile.Data.owner == GameManager.LocalPlayer.Id && tile.Data.unit == null && GameManager.IsPlayerViewing(tile.Owner.Id) && GameManager.GameState.Settings.GameType == GameType.SinglePlayer)
			{
				if (__instance.consecutiveClicks++ > 15)
				{
					if (frameCount != __instance.lastClickedFrame && GameManager.GameState.TryGetPlayer(255, out var playerState) && GameManager.GameState.GameLogicData.TryGetData(UnitData.Type.Bunny, out var data))
					{
						UnitState unitState = ActionUtils.TrainUnit(GameManager.GameState, playerState, tile.Data, data);
						AudioManager.PlaySFXAtTile(SFXTypes.Plop, tile.Coordinates);
						unitState.moved = false;
						unitState.attacked = false;
						tile.RenderUnit();
						tile.SpawnPuff();
						if (tile.Data.IsBeingCaptured(GameManager.GameState))
						{
							tile.SpawnFire();
						}
					}
					__instance.consecutiveClicks = 0;
				}
			}
			else
			{
				__instance.consecutiveClicks = 0;
			}
		}
		__instance.lastClickedFrame = frameCount;
		__instance.lastTileClick = tile;
    }

	[HarmonyPostfix]
	[HarmonyPatch(typeof(ResearchAction), nameof(ResearchAction.Execute))]
	public static void Execute(ResearchAction __instance, GameState state)
    {
        if(!state.GameLogicData.TryGetData(__instance.Type, out TechData techData))
            return;
        if(techData.unitUnlocks == null)
            return;
        if(!techData.unitUnlocks.Contains(UnitData.Type.Ship))
            return;
        if (!state.TryGetPlayer(__instance.PlayerId, out var playerState))
		{
            return;
		}

        if(!state.GameLogicData.TryGetData(UnitData.Type.Ship, out UnitData shipData))
            return;

        foreach (var tile in state.Map.Tiles)
        {
            UnitState? unit = tile.unit;
            if(unit == null)
                continue;
            if(unit.owner != __instance.PlayerId)
                continue;
            if(unit.type != UnitData.Type.Boat)
                continue;

            state.ActionStack.Add(new UpgradeAction(__instance.PlayerId, UnitData.Type.Ship, tile.coordinates, 0));
        }
    }
}
