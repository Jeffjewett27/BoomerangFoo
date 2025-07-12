using HarmonyLib;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace BoomerangFoo.Patches
{
    [HarmonyPatch(typeof(Level), "ShuffleSpawnPoints")]
    class LevelShuffleSpawnPointsPatch
    {
        static void Prefix(Level __instance, List<Transform> spawnPoints)
        {
            if (spawnPoints.Count < Singleton<GameManager>.Instance.players.Count)
            {
                // As a failsafe, duplicate spawnpoints
                var players = Singleton<GameManager>.Instance.players;
                int needed = players.Count;

                var pool = new List<Transform>(spawnPoints.Count);

                // As long as we’re under the desired count, keep pulling from a shuffled pool
                while (spawnPoints.Count < needed)
                {
                    // If pool is exhausted, refill & reshuffle
                    if (pool.Count == 0)
                    {
                        pool = new List<Transform>(spawnPoints);
                        pool.Shuffle();
                    }

                    // Take one from the front of our pool
                    spawnPoints.Add(pool[0]);
                    pool.RemoveAt(0);
                }
            }
        }
    }
    
}
