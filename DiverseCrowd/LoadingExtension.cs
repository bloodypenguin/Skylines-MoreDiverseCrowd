using DiverseCrowd.Detours;
using ICities;
using PrefabHook;
using UnityEngine;

namespace DiverseCrowd
{
    public class LoadingExtension : LoadingExtensionBase
    {

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
            if (!IsHooked())
            {
                return;
            }
            CitizenInfoHook.OnPreInitialization += OnPrePropInit;
            CitizenInfoHook.Deploy();
        }

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

        public void OnPrePropInit(CitizenInfo prop)
        {
            if (prop == null)
            {
                return;
            }
            if (prop.name != "Educated Male")
            {
                return;
            }
            LoadingManager.instance.QueueLoadingAction(Util.ActionWrapper(() =>
            {
                //CitizenInstanceDetour.customSkins.Add(prop.name, Clone(prop, "rainbow"));
                CitizenInstanceDetour.customSkins.Add(prop.name, Clone(prop, "stripes"));
            }));
        }

        private static CitizenInfo Clone(CitizenInfo prop, string modificationId)
        {
            var texture = Util.LoadTextureFromAssembly($"DiverseCrowd.skin.{modificationId}.png", false);
            var gameObject = GameObject.Find("MoreDiverseCrowd") ?? new GameObject("MoreDiverseCrowd");
            var clone = Util.ClonePrefab(prop, $"{prop.name}_{modificationId}", gameObject.transform);
            clone.m_lodObject = Object.Instantiate(prop.m_lodObject);
            clone.m_lodObject.GetComponent<Renderer>().sharedMaterial.mainTexture = texture;
            PrefabCollection<CitizenInfo>.InitializePrefabs("MoreDiverseCrowd", new[] { clone }, null);
            clone.m_lodMaterial = Object.Instantiate(prop.m_lodMaterial);
            clone.m_lodMaterial.name = prop.m_lodMaterial.name + $"_{modificationId}";
            clone.m_lodMaterial.mainTexture = texture;
            clone.m_lodMaterialCombined = clone.m_lodMaterial;
            clone.m_subCulture = Citizen.SubCulture.Gangsta; //to make sure it won't be spawned in a natural way
            clone.gameObject.SetActive(true);
            return clone;
        }

        private static bool IsHooked()
        {
            return Util.IsModActive("Prefab Hook");
        }
    }
}