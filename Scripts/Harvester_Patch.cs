using System;
using System.Reflection;
using HarmonyLib;
using Assets.Scripts;
using Assets.Scripts.Util;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Chutes;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace StationeersBepInEx.LetHarvieUseFertilizer.Scripts
{
	[HarmonyPatch(typeof(Harvester))]
	public class HarvesterPatch
	{
		public static bool GetIsTray(Harvester harvester)
		{
			return (harvester.HydroponicTray != null) ? harvester.HydroponicTray.GetThing : null;
		}

		public static bool GetIsHarvesting(Harvester harvester)
		{
			bool isHarvesting = false;
			try
			{
				var harvesterType = typeof(Harvester);
				var harvesterField = harvesterType.GetField("_isHarvesting", BindingFlags.NonPublic | BindingFlags.Instance);
				isHarvesting = (bool)harvesterField.GetValue(harvester);
			}
			catch (Exception ex)
			{
                Debug.LogError("[" + PluginInfo.PLUGIN_NAME + "] Could not get _isHarvesting via Reflection of Harvester: " + ex.Message);
			}
			return isHarvesting;
		}

		public static bool GetIsPlanting(Harvester harvester)
		{
			bool isPlanting = false;
			try
			{
				var harvesterType = typeof(Harvester);
				var harvesterField = harvesterType.GetField("_isPlanting", BindingFlags.NonPublic | BindingFlags.Instance);
				isPlanting = (bool)harvesterField.GetValue(harvester);
			}
			catch (Exception ex)
			{
				Debug.LogError("[" + PluginInfo.PLUGIN_NAME + "] Could not get _isPlanting via Reflection of Harvester: " + ex.Message);
			}
			return isPlanting;
		}

		[HarmonyPatch("TryPlantSeed")]
		[HarmonyPrefix]
		public static bool TryPlantSeed_Prefix(Harvester __instance, ref bool __result)
		{
			if (!GameManager.RunSimulation)
			{
				__result = false;
				return false; // skip original method
			}
			if (!__instance.Powered || !__instance.OnOff)
			{
				__result = false;
				return false; // skip original method
			}
			if (GameManager.IsThread)
			{
                UnityMainThreadDispatcher.Instance().Enqueue(delegate
				{
					__instance.TryPlantSeed();
				});
				__result = true;
				return false; // skip original method
			}
			if ((ArmControl)__instance.Activate == ArmControl.Idle || (!GetIsHarvesting(__instance) && !GetIsPlanting(__instance)))
			{
				Plant importPlant = __instance.ImportPlant;
				if ((object)importPlant != null && importPlant.OnUseItem(1f, __instance.ImportPlant))
				{
					DynamicThing dynamicThing = null;
					dynamicThing = ((!(__instance.ImportPlant is Seed seed)) ? OnServer.CreateOld(__instance.ImportPlant.SourcePrefab as DynamicThing, __instance.ThingTransformPosition, __instance.ThingTransform.rotation, __instance.OwnerClientId) : OnServer.CreateOld(seed.PlantType, __instance.ThingTransformPosition, __instance.ThingTransform.rotation, __instance.OwnerClientId));
					OnServer.MoveToSlot(dynamicThing, __instance.GetRobotHandSlot);
				}
				Fertiliser importFertilizer = __instance.ImportingThing as Fertiliser;
				bool flag6 = importFertilizer != null && importFertilizer.OnUseItem(1f, __instance.ImportingThing as Fertiliser);
				if (flag6)
				{
					Fertiliser fert = __instance.ImportingThing as Fertiliser;
					bool flag7 = fert != null;
					DynamicThing childThing;
					if (flag7)
					{
						childThing = OnServer.CreateOld(fert, __instance.ThingTransform.position, __instance.ThingTransform.rotation, __instance.OwnerClientId, null);
					}
					else
					{
						childThing = OnServer.CreateOld(__instance.ImportingThing.SourcePrefab as Fertiliser, __instance.ThingTransform.position, __instance.ThingTransform.rotation, __instance.OwnerClientId, null);
					}
					OnServer.MoveToSlot(childThing, __instance.GetRobotHandSlot);
				}
				OnServer.Interact(__instance.InteractActivate, 1);
				__result = true;
				return false; // skip original method
			}
			__result = false;
			return false; // skip original method
		}

		[HarmonyPatch("OnArmPlant")]
		[HarmonyPrefix]
		public static bool OnArmPlant(Harvester __instance)
		{
			if (GameManager.RunSimulation && __instance.GetRobotHandSlot.Occupant is Plant plant)
			{
				plant.SetQuantity(1);
				if (!__instance.HydroponicTray?.GetThing || __instance.HydroponicTray.IsBeingDestroyed)
				{
					OnServer.MoveToWorld(plant);
				}
				else
				{
					OnServer.MoveToSlot(plant, __instance.HydroponicTray.InputSlot);
				}
			}
			if (GameManager.RunSimulation && __instance.GetRobotHandSlot.Occupant is Fertiliser fertilizer)
			{
				fertilizer.SetQuantity(1);
				if (!__instance.HydroponicTray?.GetThing || __instance.HydroponicTray.IsBeingDestroyed)
				{
					OnServer.MoveToWorld(fertilizer);
				}
				else
				{
					if (__instance.HydroponicTray is HydroponicTray tray)
                    {
						OnServer.MoveToSlot(fertilizer, tray.InputSlot1);
					}
					else if (__instance.HydroponicTray is HydroponicsTrayDevice trayDevice)
                    {
						OnServer.MoveToSlot(fertilizer, trayDevice.InputSlot1);
					}
					else
                    {
						Debug.LogError("[" + PluginInfo.PLUGIN_NAME + "] Could not get HydroponicTray slot1 of Harvester.");
					}
				}
			}
			return false; // skip original method
		}
	}
}