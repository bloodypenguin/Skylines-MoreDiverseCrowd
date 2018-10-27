using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColossalFramework;
using ColossalFramework.Math;
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
        private static CitizenInfo _rescueWorker;
        private static CitizenInfo _bluecollarFemale;
        private static CitizenInfo _parkStaff;
        
        private static CitizenInfo _farmingWorker;
        private static CitizenInfo _oilWorker;
        private static CitizenInfo _oreWorker;
        private static CitizenInfo _forestryWorker;


        private static bool _isNdDlcEnabled;
        private static bool _isIndustriesDlcEnabled;
        private static bool _isParklifeDlcEnabled;
        
        private static Dictionary<MethodInfo, RedirectCallsState> _redirects;

        public static void Deploy()
        {
            if (_redirects != null)
            {
                return;
            }

            _redirects = RedirectionUtil.RedirectType(typeof(CitizenInstanceDetour));
            var infos = Resources.FindObjectsOfTypeAll<CitizenInfo>();
            _policeOfficer = infos.First(c => c.name == "Police Officer");
            _fireman = infos.First(c => c.name == "Fireman Default");
            _paramedic = infos.First(c => c.name == "Paramedic Default");
            _hearseDriver = infos.First(c => c.name == "Hearse Driver Default");
            try
            {
                _bluecollarFemale = infos.First(c => c.name == "Bluecollar Female");
            }
            catch
            {
                _bluecollarFemale = null;
            }

            _isNdDlcEnabled = SteamHelper.IsDLCOwned(SteamHelper.DLC.NaturalDisastersDLC);
            _isIndustriesDlcEnabled = SteamHelper.IsDLCOwned(SteamHelper.DLC.IndustryDLC);
            _isParklifeDlcEnabled = SteamHelper.IsDLCOwned(SteamHelper.DLC.ParksDLC);
            if (_isNdDlcEnabled)
            {
                _rescueWorker = infos.First(c => c.name == "Rescue Worker");
            }

            if (_isIndustriesDlcEnabled)
            {
                _farmingWorker = infos.First(c => c.name == "Farmer 01");
                _forestryWorker = infos.First(c => c.name == "Forestry Worker 01");
                _oilWorker = infos.First(c => c.name == "Oil Industry Worker 01");
                _oreWorker = infos.First(c => c.name == "Ore Industry Worker 01");
            }

            if (_isParklifeDlcEnabled)
            {
                _parkStaff = infos.First(c => c.name == "Park Staff");    
            }
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
        public static bool RenderInstance(ref CitizenInstance instance, RenderManager.CameraInfo cameraInfo,
            ushort instanceID)
        {
            if ((instance.m_flags & CitizenInstance.Flags.Character) == CitizenInstance.Flags.None)
                return false;
            CitizenInfo info = instance.Info;
            if ((Object) info == (Object) null)
                return false;
            uint num = Singleton<SimulationManager>.instance.m_referenceFrameIndex - ((uint) instanceID << 4) / 65536U;
            CitizenInstance.Frame frameData1 = instance.GetFrameData(num - 32U);
            float maxDistance = Mathf.Min(RenderManager.LevelOfDetailFactor * 800f,
                info.m_maxRenderDistance + cameraInfo.m_height * 0.5f);
            if (!cameraInfo.CheckRenderDistance(frameData1.m_position, maxDistance) ||
                !cameraInfo.Intersect(frameData1.m_position, 10f))
                return false;

            CitizenInstance.Frame frameData2 = instance.GetFrameData(num - 16U);
            float t =
                (float) (((double) (num & 15U) + (double) Singleton<SimulationManager>.instance.m_referenceTimer) *
                         (1.0 / 16.0));
            bool flag1 = frameData2.m_underground && frameData1.m_underground;
            bool flag2 = frameData2.m_insideBuilding && frameData1.m_insideBuilding;
            bool flag3 = frameData2.m_transition || frameData1.m_transition;
            if (flag2 && !flag3 || flag1 && !flag3 &&
                (cameraInfo.m_layerMask & 1 << Singleton<CitizenManager>.instance.m_undergroundLayer) == 0)
                return false;
            //begin mod
            info = GetUpdatedInfo(instance, instanceID);
            //end mod
            Vector3 vector3 = new Bezier3()
            {
                a = frameData1.m_position, b = (frameData1.m_position + frameData1.m_velocity * 0.333f),
                c = (frameData2.m_position - frameData2.m_velocity * 0.333f), d = frameData2.m_position
            }.Position(t);
            Quaternion quaternion = Quaternion.Lerp(frameData1.m_rotation, frameData2.m_rotation, t);
            Color color =
                info.m_citizenAI.GetColor(instanceID, ref instance, Singleton<InfoManager>.instance.CurrentMode);
            if (cameraInfo.CheckRenderDistance(vector3, info.m_lodRenderDistance))
            {
                InstanceID id = InstanceID.Empty;
                id.CitizenInstance = instanceID;
                CitizenInfo prefabInstance = info.ObtainPrefabInstance<CitizenInfo>(id, (int) byte.MaxValue);
                if ((Object) prefabInstance != (Object) null)
                {
                    Vector3 velocity = Vector3.Lerp(frameData1.m_velocity, frameData2.m_velocity, t);
                    prefabInstance.m_citizenAI.SetRenderParameters(cameraInfo, instanceID, ref instance, vector3,
                        quaternion, velocity, color,
                        (flag1 || flag3) && (cameraInfo.m_layerMask &
                                             1 << Singleton<CitizenManager>.instance.m_undergroundLayer) != 0);
                    return true;
                }
            }

            if (flag1 || flag3)
            {
                info.m_undergroundLodLocations[info.m_undergroundLodCount].SetTRS(vector3, quaternion, Vector3.one);
                info.m_undergroundLodColors[info.m_undergroundLodCount] = (Vector4) color.linear;
                info.m_undergroundLodMin = Vector3.Min(info.m_undergroundLodMin, vector3);
                info.m_undergroundLodMax = Vector3.Max(info.m_undergroundLodMax, vector3);
                if (++info.m_undergroundLodCount == info.m_undergroundLodLocations.Length)
                    CitizenInstance.RenderUndergroundLod(cameraInfo, info);
            }

            if (!flag1 || flag3)
            {
                info.m_lodLocations[info.m_lodCount].SetTRS(vector3, quaternion, Vector3.one);
                info.m_lodColors[info.m_lodCount] = (Vector4) color.linear;
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
            if (originalAI is ServicePersonAI || originalAI is AnimalAI || originalAI is PrisonerAI)
            {
                return originalInfo;
            }

            if (originalInfo.m_agePhase < Citizen.AgePhase.Adult0 || originalInfo.m_agePhase > Citizen.AgePhase.Adult3)
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
            if (workBuilding.Info == null || workBuilding.m_flags == Building.Flags.None)
            {
                return originalInfo;
            }

            CitizenInfo replacementInfo = null;
            GetReplacement(workBuilding, originalInfo, ref replacementInfo);
            return replacementInfo == null ? originalInfo : replacementInfo;
        }

        private static void GetReplacement(Building workBuilding, CitizenInfo originalInfo, ref CitizenInfo replacementInfo)
        {
            if (workBuilding.Info.m_buildingAI is FireStationAI)
            {
                    replacementInfo = originalInfo.m_gender == Citizen.Gender.Male ? _fireman : _paramedic;
            }

            if (workBuilding.Info.m_buildingAI is HospitalAI && originalInfo.m_gender == Citizen.Gender.Female)
            {
                    replacementInfo = _paramedic;
            }

            if (originalInfo.m_gender == Citizen.Gender.Male)
            {
                if (workBuilding.Info.m_buildingAI is PoliceStationAI || workBuilding.Info.m_buildingAI is TollBoothAI)
                {
                    replacementInfo = _policeOfficer;
                }

                if (workBuilding.Info.m_buildingAI is CemeteryAI)
                {
                    replacementInfo = _hearseDriver;
                }
            }
            
            if (_isParklifeDlcEnabled)
            {
                if (originalInfo.m_gender == Citizen.Gender.Male)
                {
                    if (workBuilding.Info.m_buildingAI is ParkAI || (workBuilding.Info.m_buildingAI is ParkBuildingAI ai && ai.m_parkType != DistrictPark.ParkType.AmusementPark) ||
                        workBuilding.Info.m_buildingAI is ParkGateAI || (workBuilding.Info.m_buildingAI is MaintenanceDepotAI && workBuilding.Info.GetService() == ItemClass.Service.Beautification))
                    {
                        replacementInfo = _parkStaff;
                    }
                }
            }
            
            if (_isIndustriesDlcEnabled)
            {
                if (workBuilding.Info.m_buildingAI is IndustrialBuildingAI ||
                    workBuilding.Info.m_buildingAI is IndustryBuildingAI ||
                    workBuilding.Info.m_buildingAI is ExtractingDummyAI ||
                    workBuilding.Info.m_buildingAI is MainIndustryBuildingAI ||
                    workBuilding.Info.m_buildingAI is WarehouseAI)
                {
                    switch (workBuilding.Info.GetSubService())
                    {
                        case ItemClass.SubService.PlayerIndustryForestry:
                        case ItemClass.SubService.IndustrialForestry:
                        {
                            replacementInfo = originalInfo.m_gender == Citizen.Gender.Male ? _forestryWorker : _bluecollarFemale;
                            break;
                        }
                        case ItemClass.SubService.PlayerIndustryFarming:
                        case ItemClass.SubService.IndustrialFarming:
                        {
                            replacementInfo = originalInfo.m_gender == Citizen.Gender.Male ? _farmingWorker : _bluecollarFemale;
                            break;
                        }
                        case ItemClass.SubService.PlayerIndustryOil:
                        case ItemClass.SubService.IndustrialGeneric:
                        case ItemClass.SubService.IndustrialOil:
                        {
                            replacementInfo = originalInfo.m_gender == Citizen.Gender.Male ? _oilWorker : _bluecollarFemale;
                            break;;
                        }
                        case ItemClass.SubService.PlayerIndustryOre:
                        case ItemClass.SubService.IndustrialOre:
                        {
                            replacementInfo = originalInfo.m_gender == Citizen.Gender.Male ? _oreWorker : _bluecollarFemale;
                            break;
                        }
                    }
                }
                else if (workBuilding.Info.m_buildingAI is LandfillSiteAI || workBuilding.Info.m_buildingAI is PowerPlantAI ||
                         workBuilding.Info.m_buildingAI is WaterFacilityAI || workBuilding.Info.m_buildingAI is CargoStationAI || workBuilding.Info.m_buildingAI is SnowDumpAI ||
                         (workBuilding.Info.m_buildingAI is MaintenanceDepotAI && workBuilding.Info.GetService() == ItemClass.Service.Road))
                {
                    replacementInfo = originalInfo.m_gender == Citizen.Gender.Male ? _oilWorker : _bluecollarFemale;  
                }
            }

            if (_isNdDlcEnabled)
            {
                if (workBuilding.Info.m_buildingAI is DisasterResponseBuildingAI)
                {
                        replacementInfo = originalInfo.m_gender == Citizen.Gender.Male ? _rescueWorker : _paramedic;
                }

                if (workBuilding.Info.m_buildingAI is HelicopterDepotAI &&
                    workBuilding.Info.m_class.m_service == ItemClass.Service.FireDepartment)
                {
                        replacementInfo = originalInfo.m_gender == Citizen.Gender.Male ? _fireman : _paramedic;
                }

                if (originalInfo.m_gender == Citizen.Gender.Male)
                {
                    if (workBuilding.Info.m_buildingAI is HelicopterDepotAI &&
                        workBuilding.Info.m_class.m_service == ItemClass.Service.PoliceDepartment)
                    {
                            replacementInfo = _policeOfficer;
                    }
                }
                else if (originalInfo.m_gender == Citizen.Gender.Female)
                {
                    if (workBuilding.Info.m_buildingAI is HelicopterDepotAI &&
                        workBuilding.Info.m_class.m_service == ItemClass.Service.HealthCare)
                    {
                            replacementInfo = _paramedic;
                    }
                }
            }
        }
    }
}