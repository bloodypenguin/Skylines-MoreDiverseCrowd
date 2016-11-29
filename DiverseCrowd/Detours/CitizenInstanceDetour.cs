using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.PlatformServices;
using DiverseCrowd.Redirection;
using UnityEngine;

namespace DiverseCrowd.Detours
{
    [TargetType(typeof(CitizenInstance))]
    public struct CitizenInstanceDetour
    {
        private static CitizenInfo _policeOfficer;
        private static CitizenInfo _fireman;
        private static CitizenInfo _paramedic;
        private static CitizenInfo _hearseDriver;
        private static bool _isDlcEnabled;
        private static Dictionary<MethodInfo, RedirectCallsState> _redirects;

        public static void Deploy()
        {
            if (_redirects != null)
            {
                return;
            }
            _redirects = RedirectionUtil.RedirectType(typeof(CitizenInstanceDetour));
            _policeOfficer = Resources.FindObjectsOfTypeAll<CitizenInfo>().First(c => c.name == "Police Officer");
            _fireman = Resources.FindObjectsOfTypeAll<CitizenInfo>().First(c => c.name == "Fireman Default");
            _paramedic = Resources.FindObjectsOfTypeAll<CitizenInfo>().First(c => c.name == "Paramedic Default");
            _hearseDriver = Resources.FindObjectsOfTypeAll<CitizenInfo>().First(c => c.name == "Hearse Driver Default");
            _isDlcEnabled = PlatformService.IsDlcInstalled(369150U);
        }

        public static void Revert()
        {
            if (_redirects == null)
            {
                return;
            }
            foreach (var redirect in _redirects)
            {
                RedirectionHelper.RevertRedirect(redirect.Key, redirect.Value);
            }
            _redirects = null;
        }

        [RedirectMethod]
        public static bool RenderInstance(ref CitizenInstance instance, RenderManager.CameraInfo cameraInfo, ushort instanceID)
        {
            if ((instance.m_flags & CitizenInstance.Flags.Character) == CitizenInstance.Flags.None)
                return false;
            CitizenInfo info = instance.Info;
            if ((UnityEngine.Object)info == (UnityEngine.Object)null)
                return false;
            uint num = Singleton<SimulationManager>.instance.m_referenceFrameIndex - ((uint)instanceID << 4) / 65536U;
            CitizenInstance.Frame frameData1 = instance.GetFrameData(num - 32U);
            float maxDistance = Mathf.Min(RenderManager.LevelOfDetailFactor * 800f, info.m_maxRenderDistance + cameraInfo.m_height * 0.5f);
            if (!cameraInfo.CheckRenderDistance(frameData1.m_position, maxDistance) || !cameraInfo.Intersect(frameData1.m_position, 10f))
                return false;

            CitizenInstance.Frame frameData2 = instance.GetFrameData(num - 16U);
            float t = (float)(((double)(num & 15U) + (double)Singleton<SimulationManager>.instance.m_referenceTimer) * (1.0 / 16.0));
            bool flag1 = frameData2.m_underground && frameData1.m_underground;
            bool flag2 = frameData2.m_insideBuilding && frameData1.m_insideBuilding;
            bool flag3 = frameData2.m_transition || frameData1.m_transition;
            if (flag2 && !flag3 || flag1 && !flag3 && (cameraInfo.m_layerMask & 1 << Singleton<CitizenManager>.instance.m_undergroundLayer) == 0)
                return false;
            //begin mod
            info = GetUpdatedInfo(instance, instanceID);
            //end mod
            Vector3 vector3 = new Bezier3()
            {
                a = frameData1.m_position,
                b = (frameData1.m_position + frameData1.m_velocity * 0.333f),
                c = (frameData2.m_position - frameData2.m_velocity * 0.333f),
                d = frameData2.m_position
            }.Position(t);
            Quaternion quaternion = Quaternion.Lerp(frameData1.m_rotation, frameData2.m_rotation, t);
            Color color = info.m_citizenAI.GetColor(instanceID, ref instance, Singleton<InfoManager>.instance.CurrentMode);
            if (cameraInfo.CheckRenderDistance(vector3, info.m_lodRenderDistance))
            {
                InstanceID id = InstanceID.Empty;
                id.CitizenInstance = instanceID;
                CitizenInfo citizenInfo = info.ObtainPrefabInstance<CitizenInfo>(id, (int)byte.MaxValue);
                if ((UnityEngine.Object)citizenInfo != (UnityEngine.Object)null)
                {
                    Vector3 velocity = Vector3.Lerp(frameData1.m_velocity, frameData2.m_velocity, t);
                    //begin mod
                    if (info.m_subCulture == Citizen.SubCulture.Generic)
                    {
                        citizenInfo.m_citizenAI.SetRenderParameters(cameraInfo, instanceID, ref instance, vector3,
                            quaternion, velocity, color,
                            (flag1 || flag3) &&
                            (cameraInfo.m_layerMask & 1 << Singleton<CitizenManager>.instance.m_undergroundLayer) != 0);
                    }
                    else
                    {
                        citizenInfo.SetRenderParameters(vector3, quaternion, velocity, color, 5,
                            (flag1 || flag3) &&
                            (cameraInfo.m_layerMask & 1 << Singleton<CitizenManager>.instance.m_undergroundLayer) != 0);
                    }
                    //end mod
                    return true;
                }
            }
            if (flag1 || flag3)
            {
                info.m_undergroundLodLocations[info.m_undergroundLodCount].SetTRS(vector3, quaternion, Vector3.one);
                info.m_undergroundLodColors[info.m_undergroundLodCount] = color;
                info.m_undergroundLodMin = Vector3.Min(info.m_undergroundLodMin, vector3);
                info.m_undergroundLodMax = Vector3.Max(info.m_undergroundLodMax, vector3);
                if (++info.m_undergroundLodCount == info.m_undergroundLodLocations.Length)
                    CitizenInstance.RenderUndergroundLod(cameraInfo, info);
            }
            if (!flag1 || flag3)
            {
                info.m_lodLocations[info.m_lodCount].SetTRS(vector3, quaternion, Vector3.one);
                info.m_lodColors[info.m_lodCount] = color;
                info.m_lodMin = Vector3.Min(info.m_lodMin, vector3);
                info.m_lodMax = Vector3.Max(info.m_lodMax, vector3);
                if (++info.m_lodCount == info.m_lodLocations.Length)
                    CitizenInstance.RenderLod(cameraInfo, info);
            }
            return true;
        }

        private static CitizenInfo GetUpdatedInfo(CitizenInstance instance, ushort instanceID)
        {
            CitizenInfo originalInfo = instance.Info;
            if (originalInfo == null || originalInfo.GetService() != ItemClass.Service.Residential)
            {
                return originalInfo;
            }
            var originalAI = originalInfo.m_citizenAI;
            if (originalAI is ServicePersonAI || originalAI is AnimalAI)
            {
                return originalInfo;
            }
            if (originalInfo.m_agePhase == Citizen.AgePhase.Teen0 || originalInfo.m_agePhase == Citizen.AgePhase.Teen1 || originalInfo.m_agePhase > Citizen.AgePhase.Adult3)
            {
                return originalInfo;
            }
            var citizenID = instance.m_citizen;
            var citizen = CitizenManager.instance.m_citizens.m_buffer[citizenID];
            if (citizen.m_instance != instanceID || instance.m_flags.IsFlagSet(CitizenInstance.Flags.RidingBicycle) ||
                instance.m_flags.IsFlagSet(CitizenInstance.Flags.OnBikeLane))
            {
                return originalInfo;
            }
            var workBuilding = BuildingManager.instance.m_buildings.m_buffer[citizen.m_workBuilding];
            if (workBuilding.m_flags != Building.Flags.None && originalInfo.m_agePhase != Citizen.AgePhase.Child)
            {
                if (workBuilding.Info.m_buildingAI is FireStationAI)
                {
                    return originalInfo.m_gender == Citizen.Gender.Male ? _fireman : _paramedic;
                }
                if (workBuilding.Info.m_buildingAI is HospitalAI || workBuilding.Info.m_buildingAI is PoliceStationAI)
                {
                    return originalInfo.m_gender == Citizen.Gender.Female ? _paramedic : _policeOfficer;
                }
                if (workBuilding.Info.m_buildingAI is CemeteryAI && originalInfo.m_gender == Citizen.Gender.Male)
                {
                    return _hearseDriver;
                }
                if (workBuilding.Info.m_buildingAI is PlayerBuildingAI && originalInfo.m_gender == Citizen.Gender.Male)
                {
                    return _policeOfficer;
                }
            }
            if (!_isDlcEnabled)
            {
                return originalInfo;
            }
            var r1 = new Randomizer(citizenID);
            var value = r1.Int32(15);
            Citizen.SubCulture subCulture;
            switch (value)
            {
                case 0:
                    subCulture = Citizen.SubCulture.Hippie;
                    break;
                case 1:
                    subCulture = Citizen.SubCulture.Hipster;
                    break;
                case 2:
                    subCulture = Citizen.SubCulture.Redneck;
                    break;
                default:
                    return originalInfo;
            }
            var r2 = new Randomizer(citizenID);
            var info = Singleton<CitizenManager>.instance.GetGroupCitizenInfo(ref r2,
                originalInfo.GetService(),
                originalInfo.m_gender, subCulture, originalInfo.m_agePhase);
            if (info == null || info.m_citizenAI.GetType() != originalAI.GetType())
            {
                info = originalInfo;
            }
            return info;
        }
    }
}