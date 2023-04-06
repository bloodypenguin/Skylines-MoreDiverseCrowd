using CitiesHarmony.API;
using ICities;

namespace DiverseCrowd
{
    public class Mod : IUserMod
    {
        public string Name => "More Diverse Crowd";
        public string Description => "Adds service people to the streets";
        
        public void OnEnabled()
        {
            HarmonyHelper.EnsureHarmonyInstalled();
        }
    }
}
