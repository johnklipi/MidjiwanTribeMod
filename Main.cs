using BepInEx.Logging;
using HarmonyLib;
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
        {"boat", 4576502},
        {"ship", 197379},
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
        if(unitData.type == UnitData.Type.Bunny)
        {
            gameState.GameLogicData.TryGetData(EnumCache<UnitData.Type>.GetType("midjix"), out unitData);
        }
        else if(unitData.type == UnitData.Type.Transportship)
        {
            gameState.GameLogicData.TryGetData(UnitData.Type.Boat, out unitData);
        }
        return true;
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
}
