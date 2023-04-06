
using System.Linq;
using ColossalFramework;
using UnityEngine;

namespace DiverseCrowd
{
    public static class CitizenInstanceHelper
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
        
        public static void Initialize()
        {
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

        public static CitizenInfo GetUpdatedInfo(CitizenInstance instance)
        {
            var instanceID = CitizenManager.instance.m_citizens.m_buffer[instance.m_citizen].m_instance;
            if (instanceID == 0)
            {
                return instance.Info;
            }
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