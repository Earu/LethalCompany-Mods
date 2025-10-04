using BepInEx;
using HarmonyLib;
using LethalLib.Modules;
using UnityEngine;
using Unity.Netcode;
using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LethalCompany
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "earu.lc.Test";
        private const string modName = "Test Mod";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        // Reference to our enemy prefab
        private static GameObject shadowDemonPrefab;
        private static bool hasSpawnedThisLanding = false;

        public void Awake()
        {
            // Create the enemy prefab
            shadowDemonPrefab = CreateEnemyPrefab();
            var enemyType = EnemyType.CreateInstance<EnemyType>();
            enemyType.canDie = false;
            enemyType.enemyName = "Shadow Demon";
            enemyType.enemyPrefab = shadowDemonPrefab;
            enemyType.canBeStunned = false;
            enemyType.canSeeThroughFog = true;

            // Register the enemy with LethalLib
            Enemies.RegisterEnemy(enemyType, 1, Levels.LevelTypes.All, Enemies.SpawnType.Default, null, null);

            // Register our spawn handler
            harmony.PatchAll(typeof(Plugin));
            Logger.LogInfo($"Plugin {modGUID} is loaded!");
        }

        private GameObject CreateEnemyPrefab()
        {
            // Create a new GameObject for our enemy
            GameObject prefab = new GameObject("ShadowDemon");

            // Add required components
            prefab.AddComponent<ShadowDemon>();
            // Networking (required for networked spawn)
            prefab.AddComponent<NetworkObject>();

            return prefab;
        }

        internal static void SpawnShadowDemonNearHost()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            {
                return;
            }

            try
            {
                var startOfRound = StartOfRound.Instance;
                if (startOfRound == null)
                {
                    return;
                }

                PlayerControllerB hostPlayer = null;
                if (startOfRound.allPlayerScripts != null)
                {
                    foreach (var player in startOfRound.allPlayerScripts)
                    {
                        if (player != null && player.IsOwner && player.isPlayerControlled)
                        {
                            hostPlayer = player;
                            break;
                        }
                    }
                    if (hostPlayer == null)
                    {
                        hostPlayer = startOfRound.allPlayerScripts.FirstOrDefault(p => p != null && p.isPlayerControlled);
                    }
                }

                Vector3 desiredPos = Vector3.zero;
                if (hostPlayer != null)
                {
                    desiredPos = hostPlayer.transform.position + hostPlayer.transform.forward * 6f;
                }

                var go = GameObject.Instantiate(shadowDemonPrefab, desiredPos, Quaternion.identity);
                var netObj = go.GetComponent<NetworkObject>();
                if (netObj != null && !netObj.IsSpawned)
                {
                    netObj.Spawn(true);
                }
            }
            catch (Exception ex)
            {
                // Use BepInEx logger if available
                UnityEngine.Debug.LogError($"[ShadowDemon] Failed to spawn: {ex}");
            }
        }

        [HarmonyPatch(typeof(StartOfRound))]
        private static class StartOfRound_Patch
        {
            [HarmonyPostfix]
            [HarmonyPatch("OnShipLandedOnMoon")]
            private static void OnShipLandedOnMoon_Postfix()
            {
                if (hasSpawnedThisLanding)
                {
                    return;
                }
                hasSpawnedThisLanding = true;
                SpawnShadowDemonNearHost();
            }

            [HarmonyPrefix]
            [HarmonyPatch("ShipLeaveMoonEarlyClientRpc")]
            private static void ShipLeaveMoonEarlyClientRpc_Prefix()
            {
                // Reset for next landing
                hasSpawnedThisLanding = false;
            }
        }
    }
}
