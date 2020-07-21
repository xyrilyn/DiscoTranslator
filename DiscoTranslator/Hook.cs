using DiscoTranslator.Translation;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DiscoTranslator
{
    public static class Hook
    {
        public static bool EnableImageHook = true;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(I2.Loc.LocalizationManager), nameof(I2.Loc.LocalizationManager.GetTranslation))]
        static bool GetTermTranslationPrefix(string Term, string overrideLanguage, ref string __result)
        {
            if (overrideLanguage != null && overrideLanguage == "English")
                return true;
            if (Term == null)
                return true;
            return !TranslationManager.TryGetTranslation(Term, out __result);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocalizationCustomSystem.LocalizationManager), nameof(LocalizationCustomSystem.LocalizationManager.GetLocalizedSprite))]
        static bool GetlocalizedSpritePrefix(string term, ref UnityEngine.Sprite __result)
        {
            if (!EnableImageHook) return true;
            return !ImageManager.TryGetImage(term, out __result);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LocalizedLayerElement), nameof(LocalizedLayerElement.SetLocalizedParameters))]
        static bool SetLocalizedParametersPrefix(ref UnityEngine.Sprite fallbackSprite)
        {
            string outputPath = Path.Combine(DiscoTranslatorPlugin.GetPluginDir(), "OriginalImages", "DialogueImages", fallbackSprite.name + ".png");
            if (!File.Exists(outputPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                ImageManager.SaveTexture2D(fallbackSprite.texture, outputPath);
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(I2.Loc.LocalizeTarget_UnityStandard_SpriteRenderer), "DoLocalize")]
        static bool DoLocalizePrefix(ref I2.Loc.Localize cmp, I2.Loc.LocalizeTarget_UnityStandard_SpriteRenderer __instance)
        {
            if (cmp.Term == "FURIES_QUOTE_IMG" && ImageManager.Furies != null)
            {
                __instance.mTarget.sprite = ImageManager.Furies;
                return false;
            }
            return true;
        }
    }
}
