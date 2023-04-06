using CitiesHarmony.API;
using DiverseCrowd.Patches.CitizenInstancePatches;
using ICities;

namespace DiverseCrowd
{
    public class LoadingExtension : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            CitizenInstanceHelper.Initialize();
            if (!HarmonyHelper.IsHarmonyInstalled)
            {
                return;
            }
            RenderInstancePatch.Apply();
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            if (!HarmonyHelper.IsHarmonyInstalled)
            {
                return;
            }
            RenderInstancePatch.Undo();
        }
    }
}