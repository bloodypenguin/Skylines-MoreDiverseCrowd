using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace DiverseCrowd.Patches.CitizenInstancePatches
{
    public static class RenderInstancePatch
    {
        private static bool _deployed;

        public static void Apply()
        {
            if (_deployed)
            {
                return;
            }

            PatchUtil.Patch(
                new PatchUtil.MethodDefinition(typeof(CitizenInstance),
                    nameof(CitizenInstance.RenderInstance), BindingFlags.Default, new[]{typeof(RenderManager.CameraInfo), typeof(ushort)}),
                transpiler:new PatchUtil.MethodDefinition(typeof(RenderInstancePatch), nameof(Transpile)));

            _deployed = true;
        }

        public static void Undo()
        {
            if (!_deployed)
            {
                return;
            }

            PatchUtil.Unpatch(
                new PatchUtil.MethodDefinition(typeof(CitizenInstance),
                    nameof(CitizenInstance.RenderInstance), BindingFlags.Default,
                    new[] { typeof(RenderManager.CameraInfo), typeof(ushort) }));

            _deployed = false;
        }

        private static IEnumerable<CodeInstruction> Transpile(MethodBase original,
            IEnumerable<CodeInstruction> instructions)
        {
            Debug.Log("MoreDiverseCrowd: RenderInstancePatch - Transpiling method: " + original.DeclaringType + "." +
                      original);
            var replaced = false;
            foreach (var codeInstruction in instructions)
            {
                if (replaced || SkipInstruction(codeInstruction))
                {
                    yield return codeInstruction;
                    continue;
                }

                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(RenderInstancePatch), nameof(GetUpdatedInfo)))
                {
                    labels = codeInstruction.labels
                };
                Debug.Log(
                    $"MoreDiverseCrowd: RenderInstancePatch - Replaced getting citizen info");
                replaced = true;
            }
        }

        private static bool SkipInstruction(CodeInstruction codeInstruction)
        {
            return codeInstruction.opcode != OpCodes.Call || codeInstruction.operand  == null || !codeInstruction.operand.ToString().Contains("get_Info");
        }

        public static CitizenInfo GetUpdatedInfo(ref CitizenInstance citizenInstance)
        {
            return CitizenInstanceHelper.GetUpdatedInfo(citizenInstance);
        }
    }
}