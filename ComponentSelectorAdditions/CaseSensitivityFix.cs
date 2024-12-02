using FrooxEngine;
using HarmonyLib;
using MonkeyLoader.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ComponentSelectorAdditions
{
    [HarmonyPatchCategory(nameof(CaseSensitivityFix))]
    internal sealed class CaseSensitivityFix : Monkey<CaseSensitivityFix>
    {
        public override bool CanBeDisabled => true;

        protected override bool OnLoaded()
        {
            if (!Enabled)
                return true;

            GlobalTypeRegistry._nameToSystemType = new(GlobalTypeRegistry._nameToSystemType, StringComparer.OrdinalIgnoreCase);
            GlobalTypeRegistry._byName = new(GlobalTypeRegistry._byName, StringComparer.OrdinalIgnoreCase);

            return base.OnLoaded();
        }

        private static void Postfix(AssemblyTypeRegistry __instance)
        {
            __instance._typesByFullName = new(__instance._typesByFullName, StringComparer.OrdinalIgnoreCase);
            __instance._typesByName.dictionary = new(__instance._typesByName.dictionary, StringComparer.OrdinalIgnoreCase);
            __instance._movedTypes = new(__instance._movedTypes, StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<MethodBase> TargetMethods()
            => AccessTools.GetDeclaredConstructors(typeof(AssemblyTypeRegistry), false);
    }
}