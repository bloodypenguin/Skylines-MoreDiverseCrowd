using DiverseCrowd.Detours;
using ICities;

namespace DiverseCrowd
{
    public class LoadingExtension : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            CitizenInstanceDetour.Deploy();
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            CitizenInstanceDetour.Revert();
        }
    }
}