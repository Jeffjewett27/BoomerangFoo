using I2.Loc;
using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using System.Text.RegularExpressions;

namespace BoomerangFoo.Patches.I2Patches
{
    [HarmonyPatch(typeof(LocalizedString), nameof(LocalizedString.ToString))]
    class LocalizedStringToStringPatch
    {
        static bool Prefix(LocalizedString __instance, ref string __result)
        {
            string[] pieces = __instance.mTerm.Split(new string[] { "__" }, StringSplitOptions.None);
            string translation = LocalizationManager.GetTranslation(pieces[0], !__instance.mRTL_IgnoreArabicFix, __instance.mRTL_MaxLineLength, !__instance.mRTL_ConvertNumbers, applyParameters: true);
            LocalizationManager.ApplyLocalizationParams(ref translation, !__instance.m_DontLocalizeParameters);

            if (__instance.mTerm != null && __instance.mTerm.Length > 0 && (translation == null || translation.Length == 0))
            {
                // if an mTerm was present, but it failed to produce a localized string, then just return that mTerm
                translation = __instance.mTerm;
            }

            // Replace %s% placeholders sequentially
            for (int i = 1; i < pieces.Length; i++)
            {
                string param = pieces[i];
                if (translation.Contains("%s%"))
                {
                    int templateIndex = translation.IndexOf("%s%");
                    translation = translation.Remove(templateIndex) + param + translation.Substring(templateIndex + 3);
                }
            }

            __result = translation;
            return false;
        }
    }
}
