using BoomerangFoo.Settings;
using HarmonyLib;
using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.UI;

namespace BoomerangFoo.Patches
{
    class PatchUIMenuRoundEnd
    {
        public static void EnsureMaxPlayers(UIMenuRoundEnd __instance)
        {
            int maxPlayers = ModSettings.Instance.MaxPlayers;
            if (__instance.uiScoreRows.Length < maxPlayers)
            {
                GameObject scoreRowTemplate = __instance.uiScoreRows[1].transform.gameObject;
                int origLen = __instance.uiScoreRows.Length;
                Array.Resize(ref __instance.uiScoreRows, maxPlayers);
                for (int i = origLen; i < maxPlayers; i++)
                {
                    GameObject newScoreRowObject = UnityEngine.Object.Instantiate(scoreRowTemplate);
                    newScoreRowObject.transform.SetParent(scoreRowTemplate.transform.parent, false);
                    UIScoreRow newScoreRow = newScoreRowObject.GetComponent<UIScoreRow>();
                    newScoreRow.playerIcon = newScoreRowObject.GetComponentInChildren<UIPlayerIcon>();
                    GameObject outline = newScoreRowObject.transform.GetChild(1).gameObject;
                    GameObject backdrop = outline.transform.GetChild(0).gameObject;
                    GameObject sprite = backdrop.transform.GetChild(0).gameObject;
                    GameObject crown = backdrop.transform.GetChild(1).gameObject;
                    newScoreRow.outline = outline.GetComponent<UnityEngine.UI.Image>();
                    newScoreRow.characterIconBackground = backdrop.GetComponent<UnityEngine.UI.Image>();
                    newScoreRow.characterIconSprite = sprite.GetComponent<UnityEngine.UI.Image>();
                    newScoreRow.characterIconCrown = crown.GetComponent<RectTransform>();
                    newScoreRow.playerIcon.SetPlayer(i, PlayerType.Bot);
                    __instance.uiScoreRows[i] = newScoreRow;
                    __instance.uiScoreRows[i].gameObject.SetActive(value: false);
                }
                //if (__instance.uiScoreRows[0].playerIcon.playerID != 0)
                //{
                //    BoomerangFoo.Logger.LogWarning($"uiScoreRows[0] was mutated to {__instance.uiScoreRows[0].playerIcon.playerID}");
                //    __instance.uiScoreRows[0].playerIcon.SetPlayer(0, PlayerType.Human);
                //}
                //__instance.uiScoreRows[8].playerIcon.SetPlayer(4, PlayerType.Human);
            }
        }
    }

    [HarmonyPatch(typeof(UIMenuRoundEnd), nameof(UIMenuRoundEnd.Init))]
    class UIMenuRoundEndInitPatch
    {
        static void Prefix(UIMenuRoundEnd __instance)
        {
            BoomerangFoo.Logger.LogInfo("Round End Info");
            PatchUIMenuRoundEnd.EnsureMaxPlayers(__instance);
        }

        static void Postfix(UIMenuRoundEnd __instance)
        {
            // Determine how many rows to show
            int numRows = GameManager.Instance.players.Count;
            if (Singleton<SettingsManager>.Instance.teamMatch)
            {
                numRows = 2;
            }

            // Show or hide them
            for (int i = 0; i < __instance.uiScoreRows.Length; i++)
            {
               __instance.uiScoreRows[i].gameObject.SetActive(i < numRows);
            }

            // Adjust the spacing between rows and position of rows to accomodate more rows.
            var container = __instance.uiScoreRows[0].transform.parent;
            var rectTransform = container.GetComponent<RectTransform>();
            if (numRows > 6)
            {
                rectTransform.anchoredPosition = new Vector2(0, -20f);
            } else
            {
                rectTransform.anchoredPosition = new Vector2(0, 20f);
            }
            var vertLayout = container.GetComponent<VerticalLayoutGroup>();
            if (numRows <= 8)
            {
                vertLayout.spacing = 20f;
            } else if (numRows <= 10)
            {
                vertLayout.spacing = 10f;
            } else
            {
                vertLayout.spacing = 0f;
            }
        }
    }

    [HarmonyPatch(typeof(UIMenuRoundEnd), nameof(UIMenuRoundEnd.ResetScores))]
    class UIMenuRoundEndResetScoresPatch
    {
        static void Prefix(UIMenuRoundEnd __instance)
        {
            PatchUIMenuRoundEnd.EnsureMaxPlayers(__instance);
        }
    }

}
