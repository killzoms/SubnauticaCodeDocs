using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FMOD.Studio;
using UnityEngine;
using UWE;

namespace AssemblyCSharp
{
    public class CraftData
    {
        public class TechGroupComparer : IEqualityComparer<TechGroup>
        {
            public bool Equals(TechGroup x, TechGroup y)
            {
                int num = (int)x;
                return num.Equals((int)y);
            }

            public int GetHashCode(TechGroup techGroup)
            {
                return (int)techGroup;
            }
        }

        public class TechCategoryComparer : IEqualityComparer<TechCategory>
        {
            public bool Equals(TechCategory x, TechCategory y)
            {
                int num = (int)x;
                return num.Equals((int)y);
            }

            public int GetHashCode(TechCategory techCategory)
            {
                return (int)techCategory;
            }
        }

        public enum BackgroundType : byte
        {
            Normal,
            Blueprint,
            PlantWater,
            PlantWaterSeed,
            PlantAir,
            PlantAirSeed,
            ExosuitArm
        }

        private sealed class TechData : ITechData
        {
            private static readonly IIngredient nullIngredient = new Ingredient(TechType.None, 0);

            public TechType _techType;

            public int _craftAmount = 1;

            public Ingredients _ingredients;

            public List<TechType> _linkedItems;

            public int craftAmount => _craftAmount;

            public int ingredientCount
            {
                get
                {
                    if (_ingredients == null)
                    {
                        return 0;
                    }
                    return _ingredients.Count;
                }
            }

            public int linkedItemCount
            {
                get
                {
                    if (_linkedItems == null)
                    {
                        return 0;
                    }
                    return _linkedItems.Count;
                }
            }

            public IIngredient GetIngredient(int index)
            {
                if (_ingredients == null || index >= _ingredients.Count || index < 0)
                {
                    return nullIngredient;
                }
                return _ingredients[index];
            }

            public TechType GetLinkedItem(int index)
            {
                if (_linkedItems == null || index >= _linkedItems.Count || index < 0)
                {
                    return TechType.None;
                }
                return _linkedItems[index];
            }
        }

        private sealed class Ingredient : IIngredient
        {
            private TechType _techType;

            private int _amount;

            public TechType techType => _techType;

            public int amount => _amount;

            public Ingredient(TechType techType, int amount = 1)
            {
                _techType = techType;
                _amount = amount;
            }
        }

        private sealed class Ingredients : List<Ingredient>
        {
            public void Add(TechType techType, int amount = 1)
            {
                Add(new Ingredient(techType, amount));
            }
        }

        public static TechGroupComparer sTechGroupComparer = new TechGroupComparer();

        public static TechCategoryComparer sTechCategoryComparer = new TechCategoryComparer();

        private static readonly Dictionary<TechType, float> craftingTimes = new Dictionary<TechType, float>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.Tank,
                3f
            },
            {
                TechType.DoubleTank,
                5f
            },
            {
                TechType.PlasteelTank,
                3f
            },
            {
                TechType.HighCapacityTank,
                3f
            },
            {
                TechType.AirBladder,
                3f
            },
            {
                TechType.Welder,
                3f
            },
            {
                TechType.LaserCutter,
                3f
            },
            {
                TechType.Builder,
                3f
            },
            {
                TechType.Fins,
                3f
            },
            {
                TechType.SwimChargeFins,
                3f
            },
            {
                TechType.UltraGlideFins,
                3f
            },
            {
                TechType.Scanner,
                3f
            },
            {
                TechType.LEDLight,
                3f
            },
            {
                TechType.Flashlight,
                3f
            },
            {
                TechType.DiveReel,
                3f
            },
            {
                TechType.Rebreather,
                3f
            },
            {
                TechType.PowerCell,
                3f
            },
            {
                TechType.ReactorRod,
                3f
            },
            {
                TechType.FirstAidKit,
                3f
            },
            {
                TechType.FireExtinguisher,
                3f
            },
            {
                TechType.CurrentGenerator,
                4f
            },
            {
                TechType.Gravsphere,
                4f
            },
            {
                TechType.Beacon,
                4f
            },
            {
                TechType.SmallStorage,
                4f
            },
            {
                TechType.CyclopsDecoy,
                4f
            },
            {
                TechType.TitaniumIngot,
                5f
            },
            {
                TechType.PlasteelIngot,
                5f
            },
            {
                TechType.PropulsionCannon,
                5f
            },
            {
                TechType.RepulsionCannon,
                5f
            },
            {
                TechType.StasisRifle,
                5f
            },
            {
                TechType.Transfuser,
                5f
            },
            {
                TechType.Terraformer,
                5f
            },
            {
                TechType.Seaglide,
                5f
            },
            {
                TechType.PowerGlide,
                5f
            },
            {
                TechType.Stillsuit,
                6f
            },
            {
                TechType.RadiationSuit,
                6f
            },
            {
                TechType.ReinforcedDiveSuit,
                6f
            },
            {
                TechType.Constructor,
                10f
            },
            {
                TechType.PrecursorIonPowerCell,
                4f
            },
            {
                TechType.PrecursorIonBattery,
                2f
            }
        };

        private static readonly Dictionary<TechGroup, Dictionary<TechCategory, List<TechType>>> groups = new Dictionary<TechGroup, Dictionary<TechCategory, List<TechType>>>(sTechGroupComparer)
        {
            {
                TechGroup.Resources,
                new Dictionary<TechCategory, List<TechType>>(sTechCategoryComparer)
                {
                    {
                        TechCategory.BasicMaterials,
                        new List<TechType>
                        {
                            TechType.Titanium,
                            TechType.TitaniumIngot,
                            TechType.FiberMesh,
                            TechType.Silicone,
                            TechType.Glass,
                            TechType.Bleach,
                            TechType.Lubricant,
                            TechType.EnameledGlass,
                            TechType.PlasteelIngot
                        }
                    },
                    {
                        TechCategory.AdvancedMaterials,
                        new List<TechType>
                        {
                            TechType.HydrochloricAcid,
                            TechType.Benzene,
                            TechType.AramidFibers,
                            TechType.Aerogel,
                            TechType.Polyaniline,
                            TechType.HatchingEnzymes
                        }
                    },
                    {
                        TechCategory.Electronics,
                        new List<TechType>
                        {
                            TechType.CopperWire,
                            TechType.Battery,
                            TechType.PrecursorIonBattery,
                            TechType.PowerCell,
                            TechType.PrecursorIonPowerCell,
                            TechType.ComputerChip,
                            TechType.WiringKit,
                            TechType.AdvancedWiringKit,
                            TechType.ReactorRod
                        }
                    }
                }
            },
            {
                TechGroup.Survival,
                new Dictionary<TechCategory, List<TechType>>(sTechCategoryComparer)
                {
                    {
                        TechCategory.Water,
                        new List<TechType>
                        {
                            TechType.FilteredWater,
                            TechType.DisinfectedWater
                        }
                    },
                    {
                        TechCategory.CookedFood,
                        new List<TechType>
                        {
                            TechType.CookedHoleFish,
                            TechType.CookedPeeper,
                            TechType.CookedBladderfish,
                            TechType.CookedGarryFish,
                            TechType.CookedHoverfish,
                            TechType.CookedReginald,
                            TechType.CookedSpadefish,
                            TechType.CookedBoomerang,
                            TechType.CookedLavaBoomerang,
                            TechType.CookedEyeye,
                            TechType.CookedLavaEyeye,
                            TechType.CookedOculus,
                            TechType.CookedHoopfish,
                            TechType.CookedSpinefish
                        }
                    },
                    {
                        TechCategory.CuredFood,
                        new List<TechType>
                        {
                            TechType.CuredHoleFish,
                            TechType.CuredPeeper,
                            TechType.CuredBladderfish,
                            TechType.CuredGarryFish,
                            TechType.CuredHoverfish,
                            TechType.CuredReginald,
                            TechType.CuredSpadefish,
                            TechType.CuredBoomerang,
                            TechType.CuredLavaBoomerang,
                            TechType.CuredEyeye,
                            TechType.CuredLavaEyeye,
                            TechType.CuredOculus,
                            TechType.CuredHoopfish,
                            TechType.CuredSpinefish
                        }
                    }
                }
            },
            {
                TechGroup.Personal,
                new Dictionary<TechCategory, List<TechType>>(sTechCategoryComparer)
                {
                    {
                        TechCategory.Equipment,
                        new List<TechType>
                        {
                            TechType.Tank,
                            TechType.DoubleTank,
                            TechType.Fins,
                            TechType.RadiationSuit,
                            TechType.ReinforcedDiveSuit,
                            TechType.Stillsuit,
                            TechType.FirstAidKit,
                            TechType.FireExtinguisher,
                            TechType.Rebreather,
                            TechType.Compass,
                            TechType.Thermometer,
                            TechType.Pipe,
                            TechType.PipeSurfaceFloater,
                            TechType.PrecursorKey_Purple,
                            TechType.PrecursorKey_Blue,
                            TechType.PrecursorKey_Orange
                        }
                    },
                    {
                        TechCategory.Tools,
                        new List<TechType>
                        {
                            TechType.Scanner,
                            TechType.Welder,
                            TechType.Flashlight,
                            TechType.Knife,
                            TechType.DiveReel,
                            TechType.AirBladder,
                            TechType.Flare,
                            TechType.Builder,
                            TechType.LaserCutter,
                            TechType.StasisRifle,
                            TechType.Terraformer,
                            TechType.PropulsionCannon,
                            TechType.LEDLight,
                            TechType.Transfuser
                        }
                    }
                }
            },
            {
                TechGroup.Machines,
                new Dictionary<TechCategory, List<TechType>>(sTechCategoryComparer) { 
                {
                    TechCategory.Machines,
                    new List<TechType>
                    {
                        TechType.Seaglide,
                        TechType.Constructor,
                        TechType.Beacon,
                        TechType.SmallStorage,
                        TechType.Gravsphere,
                        TechType.CyclopsDecoy
                    }
                } }
            },
            {
                TechGroup.Constructor,
                new Dictionary<TechCategory, List<TechType>>(sTechCategoryComparer) { 
                {
                    TechCategory.Constructor,
                    new List<TechType>
                    {
                        TechType.Seamoth,
                        TechType.Exosuit,
                        TechType.RocketBase,
                        TechType.RocketBaseLadder,
                        TechType.RocketStage1,
                        TechType.RocketStage2,
                        TechType.RocketStage3
                    }
                } }
            },
            {
                TechGroup.Workbench,
                new Dictionary<TechCategory, List<TechType>>(sTechCategoryComparer) { 
                {
                    TechCategory.Workbench,
                    new List<TechType>
                    {
                        TechType.LithiumIonBattery,
                        TechType.HeatBlade,
                        TechType.PlasteelTank,
                        TechType.HighCapacityTank,
                        TechType.UltraGlideFins,
                        TechType.SwimChargeFins,
                        TechType.RepulsionCannon,
                        TechType.CyclopsHullModule2,
                        TechType.CyclopsHullModule3,
                        TechType.VehicleHullModule2,
                        TechType.VehicleHullModule3,
                        TechType.ExoHullModule2,
                        TechType.PowerGlide
                    }
                } }
            },
            {
                TechGroup.VehicleUpgrades,
                new Dictionary<TechCategory, List<TechType>>(sTechCategoryComparer) { 
                {
                    TechCategory.VehicleUpgrades,
                    new List<TechType>
                    {
                        TechType.VehicleHullModule1,
                        TechType.VehicleArmorPlating,
                        TechType.VehiclePowerUpgradeModule,
                        TechType.VehicleStorageModule,
                        TechType.SeamothSolarCharge,
                        TechType.SeamothElectricalDefense,
                        TechType.SeamothTorpedoModule,
                        TechType.SeamothSonarModule,
                        TechType.ExoHullModule1,
                        TechType.ExosuitThermalReactorModule,
                        TechType.ExosuitJetUpgradeModule,
                        TechType.ExosuitPropulsionArmModule,
                        TechType.ExosuitGrapplingArmModule,
                        TechType.ExosuitDrillArmModule,
                        TechType.ExosuitTorpedoArmModule,
                        TechType.WhirlpoolTorpedo,
                        TechType.GasTorpedo
                    }
                } }
            },
            {
                TechGroup.MapRoomUpgrades,
                new Dictionary<TechCategory, List<TechType>>(sTechCategoryComparer) { 
                {
                    TechCategory.MapRoomUpgrades,
                    new List<TechType>
                    {
                        TechType.MapRoomHUDChip,
                        TechType.MapRoomCamera,
                        TechType.MapRoomUpgradeScanRange,
                        TechType.MapRoomUpgradeScanSpeed
                    }
                } }
            },
            {
                TechGroup.Cyclops,
                new Dictionary<TechCategory, List<TechType>>(sTechCategoryComparer)
                {
                    {
                        TechCategory.Cyclops,
                        new List<TechType>
                        {
                            TechType.CyclopsHullBlueprint,
                            TechType.CyclopsBridgeBlueprint,
                            TechType.CyclopsEngineBlueprint,
                            TechType.Cyclops
                        }
                    },
                    {
                        TechCategory.CyclopsUpgrades,
                        new List<TechType>
                        {
                            TechType.CyclopsHullModule1,
                            TechType.PowerUpgradeModule,
                            TechType.CyclopsShieldModule,
                            TechType.CyclopsSonarModule,
                            TechType.CyclopsSeamothRepairModule,
                            TechType.CyclopsFireSuppressionModule,
                            TechType.CyclopsDecoyModule,
                            TechType.CyclopsThermalReactorModule
                        }
                    }
                }
            },
            {
                TechGroup.BasePieces,
                new Dictionary<TechCategory, List<TechType>>(sTechCategoryComparer)
                {
                    {
                        TechCategory.BasePiece,
                        new List<TechType>
                        {
                            TechType.BaseFoundation,
                            TechType.BaseCorridorI,
                            TechType.BaseCorridorL,
                            TechType.BaseCorridorT,
                            TechType.BaseCorridorX,
                            TechType.BaseCorridorGlassI,
                            TechType.BaseCorridorGlassL,
                            TechType.BaseConnector
                        }
                    },
                    {
                        TechCategory.BaseRoom,
                        new List<TechType>
                        {
                            TechType.BaseRoom,
                            TechType.BaseMapRoom,
                            TechType.BaseMoonpool,
                            TechType.BaseObservatory
                        }
                    },
                    {
                        TechCategory.BaseWall,
                        new List<TechType>
                        {
                            TechType.BaseHatch,
                            TechType.BaseWindow,
                            TechType.BaseReinforcement
                        }
                    }
                }
            },
            {
                TechGroup.ExteriorModules,
                new Dictionary<TechCategory, List<TechType>>(sTechCategoryComparer)
                {
                    {
                        TechCategory.ExteriorModule,
                        new List<TechType>
                        {
                            TechType.SolarPanel,
                            TechType.ThermalPlant,
                            TechType.PowerTransmitter
                        }
                    },
                    {
                        TechCategory.ExteriorLight,
                        new List<TechType>
                        {
                            TechType.Techlight,
                            TechType.Spotlight
                        }
                    },
                    {
                        TechCategory.ExteriorOther,
                        new List<TechType>
                        {
                            TechType.FarmingTray,
                            TechType.BasePipeConnector
                        }
                    }
                }
            },
            {
                TechGroup.InteriorPieces,
                new Dictionary<TechCategory, List<TechType>>(sTechCategoryComparer)
                {
                    {
                        TechCategory.InteriorPiece,
                        new List<TechType>
                        {
                            TechType.BaseLadder,
                            TechType.BaseFiltrationMachine,
                            TechType.BaseBulkhead,
                            TechType.BaseUpgradeConsole
                        }
                    },
                    {
                        TechCategory.InteriorRoom,
                        new List<TechType>
                        {
                            TechType.BaseBioReactor,
                            TechType.BaseNuclearReactor,
                            TechType.BaseWaterPark
                        }
                    }
                }
            },
            {
                TechGroup.InteriorModules,
                new Dictionary<TechCategory, List<TechType>>(sTechCategoryComparer) { 
                {
                    TechCategory.InteriorModule,
                    new List<TechType>
                    {
                        TechType.Fabricator,
                        TechType.Radio,
                        TechType.MedicalCabinet,
                        TechType.SmallLocker,
                        TechType.Locker,
                        TechType.BatteryCharger,
                        TechType.PowerCellCharger,
                        TechType.Aquarium,
                        TechType.Workbench,
                        TechType.Centrifuge,
                        TechType.PlanterPot,
                        TechType.PlanterPot2,
                        TechType.PlanterPot3,
                        TechType.PlanterBox,
                        TechType.PlanterShelf
                    }
                } }
            },
            {
                TechGroup.Miscellaneous,
                new Dictionary<TechCategory, List<TechType>>(sTechCategoryComparer)
                {
                    {
                        TechCategory.Misc,
                        new List<TechType>
                        {
                            TechType.Bench,
                            TechType.Bed1,
                            TechType.Bed2,
                            TechType.NarrowBed,
                            TechType.StarshipDesk,
                            TechType.StarshipChair,
                            TechType.StarshipChair2,
                            TechType.StarshipChair3,
                            TechType.Sign,
                            TechType.PictureFrame,
                            TechType.StarshipCargoCrate,
                            TechType.StarshipCircuitBox,
                            TechType.StarshipMonitor,
                            TechType.BarTable,
                            TechType.Trashcans,
                            TechType.LabTrashcan,
                            TechType.VendingMachine,
                            TechType.CoffeeVendingMachine,
                            TechType.LabCounter,
                            TechType.BasePlanter,
                            TechType.SingleWallShelf,
                            TechType.WallShelves
                        }
                    },
                    {
                        TechCategory.MiscHullplates,
                        new List<TechType>
                        {
                            TechType.DevTestItem,
                            TechType.SpecialHullPlate,
                            TechType.BikemanHullPlate,
                            TechType.EatMyDictionHullPlate,
                            TechType.DioramaHullPlate,
                            TechType.MarkiplierHullPlate,
                            TechType.MuyskermHullPlate,
                            TechType.LordMinionHullPlate,
                            TechType.JackSepticEyeHullPlate,
                            TechType.IGPHullPlate,
                            TechType.GilathissHullPlate,
                            TechType.Marki1,
                            TechType.Marki2,
                            TechType.JackSepticEye,
                            TechType.EatMyDiction
                        }
                    }
                }
            }
        };

        private static readonly List<TechType> buildables = new List<TechType>
        {
            TechType.Fabricator,
            TechType.SpecimenAnalyzer,
            TechType.Workbench,
            TechType.Centrifuge,
            TechType.Locker,
            TechType.SmallLocker,
            TechType.Bench,
            TechType.Bed1,
            TechType.Bed2,
            TechType.NarrowBed,
            TechType.PlanterPot,
            TechType.PlanterPot2,
            TechType.PlanterPot3,
            TechType.PlanterBox,
            TechType.PlanterShelf,
            TechType.Aquarium,
            TechType.Sign,
            TechType.PictureFrame,
            TechType.Techlight,
            TechType.Spotlight,
            TechType.BasePipeConnector,
            TechType.Radio,
            TechType.MedicalCabinet,
            TechType.SingleWallShelf,
            TechType.WallShelves,
            TechType.StarshipDesk,
            TechType.StarshipChair,
            TechType.StarshipChair2,
            TechType.StarshipChair3,
            TechType.BarTable,
            TechType.Trashcans,
            TechType.LabTrashcan,
            TechType.VendingMachine,
            TechType.CoffeeVendingMachine,
            TechType.LabCounter,
            TechType.SpecialHullPlate,
            TechType.BikemanHullPlate,
            TechType.EatMyDictionHullPlate,
            TechType.DevTestItem,
            TechType.DioramaHullPlate,
            TechType.MarkiplierHullPlate,
            TechType.MuyskermHullPlate,
            TechType.LordMinionHullPlate,
            TechType.JackSepticEyeHullPlate,
            TechType.IGPHullPlate,
            TechType.GilathissHullPlate,
            TechType.Marki1,
            TechType.Marki2,
            TechType.JackSepticEye,
            TechType.EatMyDiction,
            TechType.BaseRoom,
            TechType.BaseMoonpool,
            TechType.BaseObservatory,
            TechType.BaseMapRoom,
            TechType.BaseHatch,
            TechType.BaseWall,
            TechType.BaseDoor,
            TechType.BaseLadder,
            TechType.BaseWaterPark,
            TechType.BaseWindow,
            TechType.BaseReinforcement,
            TechType.BaseUpgradeConsole,
            TechType.BasePlanter,
            TechType.BaseFiltrationMachine,
            TechType.BaseBulkhead,
            TechType.BaseCorridor,
            TechType.BaseCorridorGlassI,
            TechType.BaseCorridorGlassL,
            TechType.BaseCorridorI,
            TechType.BaseCorridorL,
            TechType.BaseCorridorT,
            TechType.BaseCorridorX,
            TechType.BaseFoundation,
            TechType.BaseConnector,
            TechType.BaseBioReactor,
            TechType.BaseNuclearReactor,
            TechType.SolarPanel,
            TechType.PowerTransmitter,
            TechType.Bioreactor,
            TechType.ThermalPlant,
            TechType.NuclearReactor,
            TechType.BatteryCharger,
            TechType.PowerCellCharger,
            TechType.FarmingTray
        };

        private static readonly HashSet<TechType> blacklist = new HashSet<TechType>
        {
            TechType.DevTestItemBlueprintOld,
            TechType.DevTestItem,
            TechType.SpecialHullPlateBlueprintOld,
            TechType.BikemanHullPlateBlueprintOld,
            TechType.EatMyDictionHullPlateBlueprintOld,
            TechType.SpecialHullPlate,
            TechType.BikemanHullPlate,
            TechType.EatMyDictionHullPlate,
            TechType.DioramaHullPlate,
            TechType.MarkiplierHullPlate,
            TechType.MuyskermHullPlate,
            TechType.LordMinionHullPlate,
            TechType.JackSepticEyeHullPlate,
            TechType.IGPHullPlate,
            TechType.GilathissHullPlate,
            TechType.Marki1,
            TechType.Marki2,
            TechType.JackSepticEye,
            TechType.EatMyDiction,
            TechType.EnzymeCureBall,
            TechType.TimeCapsule
        };

        private static readonly Dictionary<TechType, HarvestType> harvestTypeList = new Dictionary<TechType, HarvestType>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.BallClusters,
                HarvestType.DamageAlive
            },
            {
                TechType.BarnacleSuckers,
                HarvestType.DamageAlive
            },
            {
                TechType.BlueBarnacle,
                HarvestType.Break
            },
            {
                TechType.BlueBarnacleCluster,
                HarvestType.Break
            },
            {
                TechType.BlueCoralTubes,
                HarvestType.Break
            },
            {
                TechType.Mohawk,
                HarvestType.DamageAlive
            },
            {
                TechType.GenericJeweledDisk,
                HarvestType.Break
            },
            {
                TechType.BlueJeweledDisk,
                HarvestType.Break
            },
            {
                TechType.GreenJeweledDisk,
                HarvestType.Break
            },
            {
                TechType.PurpleJeweledDisk,
                HarvestType.Break
            },
            {
                TechType.RedJeweledDisk,
                HarvestType.Break
            },
            {
                TechType.SmallKoosh,
                HarvestType.DamageAlive
            },
            {
                TechType.MediumKoosh,
                HarvestType.DamageAlive
            },
            {
                TechType.LargeKoosh,
                HarvestType.DamageAlive
            },
            {
                TechType.HugeKoosh,
                HarvestType.DamageAlive
            },
            {
                TechType.BigCoralTubes,
                HarvestType.DamageAlive
            },
            {
                TechType.CoralShellPlate,
                HarvestType.Break
            },
            {
                TechType.CoralChunk,
                HarvestType.Pick
            },
            {
                TechType.TreeMushroom,
                HarvestType.DamageAlive
            },
            {
                TechType.BlueCluster,
                HarvestType.DamageAlive
            },
            {
                TechType.Stalker,
                HarvestType.DamageDead
            },
            {
                TechType.Bladderfish,
                HarvestType.DamageDead
            },
            {
                TechType.Creepvine,
                HarvestType.DamageAlive
            },
            {
                TechType.BulboTree,
                HarvestType.DamageAlive
            },
            {
                TechType.OrangeMushroom,
                HarvestType.DamageAlive
            },
            {
                TechType.PurpleVasePlant,
                HarvestType.DamageAlive
            },
            {
                TechType.AcidMushroom,
                HarvestType.DamageAlive
            },
            {
                TechType.WhiteMushroom,
                HarvestType.DamageAlive
            },
            {
                TechType.PinkMushroom,
                HarvestType.DamageAlive
            },
            {
                TechType.PurpleRattle,
                HarvestType.DamageAlive
            },
            {
                TechType.MelonPlant,
                HarvestType.DamageAlive
            },
            {
                TechType.Melon,
                HarvestType.DamageAlive
            },
            {
                TechType.SmallMelon,
                HarvestType.DamageAlive
            },
            {
                TechType.PurpleBrainCoral,
                HarvestType.DamageAlive
            },
            {
                TechType.SpikePlant,
                HarvestType.DamageAlive
            },
            {
                TechType.BluePalm,
                HarvestType.DamageAlive
            },
            {
                TechType.PurpleFan,
                HarvestType.DamageAlive
            },
            {
                TechType.SmallFan,
                HarvestType.DamageAlive
            },
            {
                TechType.SmallFanCluster,
                HarvestType.DamageAlive
            },
            {
                TechType.PurpleTentacle,
                HarvestType.DamageAlive
            },
            {
                TechType.JellyPlant,
                HarvestType.DamageAlive
            },
            {
                TechType.GabeSFeather,
                HarvestType.DamageAlive
            },
            {
                TechType.SeaCrown,
                HarvestType.DamageAlive
            },
            {
                TechType.MembrainTree,
                HarvestType.DamageAlive
            },
            {
                TechType.PinkFlower,
                HarvestType.DamageAlive
            },
            {
                TechType.FernPalm,
                HarvestType.DamageAlive
            },
            {
                TechType.OrangePetalsPlant,
                HarvestType.DamageAlive
            },
            {
                TechType.EyesPlant,
                HarvestType.DamageAlive
            },
            {
                TechType.RedGreenTentacle,
                HarvestType.DamageAlive
            },
            {
                TechType.PurpleStalk,
                HarvestType.DamageAlive
            },
            {
                TechType.RedBasketPlant,
                HarvestType.DamageAlive
            },
            {
                TechType.RedBush,
                HarvestType.DamageAlive
            },
            {
                TechType.RedConePlant,
                HarvestType.DamageAlive
            },
            {
                TechType.ShellGrass,
                HarvestType.DamageAlive
            },
            {
                TechType.SpottedLeavesPlant,
                HarvestType.DamageAlive
            },
            {
                TechType.RedRollPlant,
                HarvestType.DamageAlive
            },
            {
                TechType.PurpleBranches,
                HarvestType.DamageAlive
            },
            {
                TechType.SnakeMushroom,
                HarvestType.DamageAlive
            },
            {
                TechType.Copper,
                HarvestType.Pick
            },
            {
                TechType.CreepvineSeedCluster,
                HarvestType.Pick
            },
            {
                TechType.Gold,
                HarvestType.Pick
            },
            {
                TechType.GrandReefsEgg,
                HarvestType.Pick
            },
            {
                TechType.GrassyPlateausEgg,
                HarvestType.Pick
            },
            {
                TechType.KelpForestEgg,
                HarvestType.Pick
            },
            {
                TechType.KooshZoneEgg,
                HarvestType.Pick
            },
            {
                TechType.LavaZoneEgg,
                HarvestType.Pick
            },
            {
                TechType.LimestoneChunk,
                HarvestType.Click
            },
            {
                TechType.SandstoneChunk,
                HarvestType.Click
            },
            {
                TechType.ShaleChunk,
                HarvestType.Click
            },
            {
                TechType.BasaltChunk,
                HarvestType.Click
            },
            {
                TechType.ObsidianChunk,
                HarvestType.Click
            },
            {
                TechType.ScrapMetal,
                HarvestType.Pick
            },
            {
                TechType.MushroomForestEgg,
                HarvestType.Pick
            },
            {
                TechType.SandLoot,
                HarvestType.Pick
            },
            {
                TechType.Quartz,
                HarvestType.Pick
            },
            {
                TechType.SafeShallowsEgg,
                HarvestType.Pick
            },
            {
                TechType.Salt,
                HarvestType.Pick
            },
            {
                TechType.TwistyBridgesEgg,
                HarvestType.Pick
            },
            {
                TechType.Signal,
                HarvestType.Pick
            }
        };

        public const string defaultPickupSound = "event:/loot/pickup_default";

        private static readonly Dictionary<TechType, string> pickupSoundList = new Dictionary<TechType, string>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.AcidMushroom,
                "event:/loot/pickup_organic"
            },
            {
                TechType.CreepvinePiece,
                "event:/loot/pickup_organic"
            },
            {
                TechType.OrangeMushroom,
                "event:/loot/pickup_organic"
            },
            {
                TechType.MelonPlant,
                "event:/loot/pickup_organic"
            },
            {
                TechType.PinkMushroom,
                "event:/loot/pickup_organic"
            },
            {
                TechType.WhiteMushroom,
                "event:/loot/pickup_organic"
            },
            {
                TechType.Bladderfish,
                "event:/loot/pickup_fish"
            },
            {
                TechType.Boomerang,
                "event:/loot/pickup_fish"
            },
            {
                TechType.Cutefish,
                "event:/loot/pickup_fish"
            },
            {
                TechType.Eyeye,
                "event:/loot/pickup_fish"
            },
            {
                TechType.GarryFish,
                "event:/loot/pickup_fish"
            },
            {
                TechType.HoleFish,
                "event:/loot/pickup_fish"
            },
            {
                TechType.Hoverfish,
                "event:/loot/pickup_fish"
            },
            {
                TechType.Hoopfish,
                "event:/loot/pickup_fish"
            },
            {
                TechType.LavaLarva,
                "event:/loot/pickup_fish"
            },
            {
                TechType.Oculus,
                "event:/loot/pickup_fish"
            },
            {
                TechType.Peeper,
                "event:/loot/pickup_fish"
            },
            {
                TechType.Reginald,
                "event:/loot/pickup_fish"
            },
            {
                TechType.Spadefish,
                "event:/loot/pickup_fish"
            },
            {
                TechType.SafeShallowsEgg,
                "event:/loot/pickup_egg"
            },
            {
                TechType.KelpForestEgg,
                "event:/loot/pickup_egg"
            },
            {
                TechType.GrassyPlateausEgg,
                "event:/loot/pickup_egg"
            },
            {
                TechType.GrandReefsEgg,
                "event:/loot/pickup_egg"
            },
            {
                TechType.MushroomForestEgg,
                "event:/loot/pickup_egg"
            },
            {
                TechType.KooshZoneEgg,
                "event:/loot/pickup_egg"
            },
            {
                TechType.TwistyBridgesEgg,
                "event:/loot/pickup_egg"
            },
            {
                TechType.LavaZoneEgg,
                "event:/loot/pickup_egg"
            },
            {
                TechType.StalkerEgg,
                "event:/loot/pickup_egg"
            },
            {
                TechType.ReefbackEgg,
                "event:/loot/pickup_egg"
            },
            {
                TechType.SpadefishEgg,
                "event:/loot/pickup_egg"
            },
            {
                TechType.RabbitrayEgg,
                "event:/loot/pickup_egg"
            },
            {
                TechType.RabbitrayEggUndiscovered,
                "event:/loot/pickup_egg"
            },
            {
                TechType.JellyrayEggUndiscovered,
                "event:/loot/pickup_egg"
            },
            {
                TechType.StalkerEggUndiscovered,
                "event:/loot/pickup_egg"
            },
            {
                TechType.ReefbackEggUndiscovered,
                "event:/loot/pickup_egg"
            },
            {
                TechType.JumperEggUndiscovered,
                "event:/loot/pickup_egg"
            },
            {
                TechType.BonesharkEggUndiscovered,
                "event:/loot/pickup_egg"
            },
            {
                TechType.GasopodEggUndiscovered,
                "event:/loot/pickup_egg"
            },
            {
                TechType.MesmerEggUndiscovered,
                "event:/loot/pickup_egg"
            },
            {
                TechType.SandsharkEggUndiscovered,
                "event:/loot/pickup_egg"
            },
            {
                TechType.ShockerEggUndiscovered,
                "event:/loot/pickup_egg"
            },
            {
                TechType.Fins,
                "event:/loot/pickup_fins"
            },
            {
                TechType.UltraGlideFins,
                "event:/loot/pickup_fins"
            },
            {
                TechType.SwimChargeFins,
                "event:/loot/pickup_fins"
            },
            {
                TechType.DiveSuit,
                "event:/loot/pickup_suit"
            },
            {
                TechType.RadiationSuit,
                "event:/loot/pickup_suit"
            },
            {
                TechType.Stillsuit,
                "event:/loot/pickup_suit"
            },
            {
                TechType.Tank,
                "event:/loot/pickup_tank"
            },
            {
                TechType.PlasteelTank,
                "event:/loot/pickup_tank"
            },
            {
                TechType.HighCapacityTank,
                "event:/loot/pickup_tank"
            },
            {
                TechType.Floater,
                "event:/loot/floater/floater_pickup"
            },
            {
                TechType.LEDLight,
                "event:/tools/lights/pick_up"
            },
            {
                TechType.AirBladder,
                "event:/tools/airbladder/airbladder_pickup"
            }
        };

        public const string defaultDropSound = "event:/tools/pda/drop_item";

        private static readonly Dictionary<TechType, string> dropSoundList = new Dictionary<TechType, string>(TechTypeExtensions.sTechTypeComparer) { 
        {
            TechType.Floater,
            "event:/loot/floater/floater_place"
        } };

        public const string defaultEatSound = "event:/player/eat";

        private static readonly Dictionary<TechType, string> useEatSound = new Dictionary<TechType, string>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.FilteredWater,
                "event:/player/drink"
            },
            {
                TechType.BigFilteredWater,
                "event:/player/drink"
            },
            {
                TechType.DisinfectedWater,
                "event:/player/drink"
            },
            {
                TechType.StillsuitWater,
                "event:/player/drink_stillsuit"
            },
            {
                TechType.FirstAidKit,
                "event:/player/use_first_aid"
            }
        };

        public static readonly Dictionary<TechType, TechType> harvestOutputList = new Dictionary<TechType, TechType>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.Creepvine,
                TechType.CreepvinePiece
            },
            {
                TechType.GenericJeweledDisk,
                TechType.JeweledDiskPiece
            },
            {
                TechType.BlueJeweledDisk,
                TechType.JeweledDiskPiece
            },
            {
                TechType.GreenJeweledDisk,
                TechType.JeweledDiskPiece
            },
            {
                TechType.PurpleJeweledDisk,
                TechType.JeweledDiskPiece
            },
            {
                TechType.RedJeweledDisk,
                TechType.JeweledDiskPiece
            },
            {
                TechType.TreeMushroom,
                TechType.TreeMushroomPiece
            },
            {
                TechType.BigCoralTubes,
                TechType.CoralChunk
            },
            {
                TechType.CoralShellPlate,
                TechType.CoralChunk
            },
            {
                TechType.SmallKoosh,
                TechType.KooshChunk
            },
            {
                TechType.MediumKoosh,
                TechType.KooshChunk
            },
            {
                TechType.LargeKoosh,
                TechType.KooshChunk
            },
            {
                TechType.HugeKoosh,
                TechType.KooshChunk
            },
            {
                TechType.BulboTree,
                TechType.BulboTreePiece
            },
            {
                TechType.OrangeMushroom,
                TechType.OrangeMushroomSpore
            },
            {
                TechType.PurpleVasePlant,
                TechType.PurpleVasePlantSeed
            },
            {
                TechType.AcidMushroom,
                TechType.AcidMushroomSpore
            },
            {
                TechType.WhiteMushroom,
                TechType.WhiteMushroomSpore
            },
            {
                TechType.PinkMushroom,
                TechType.PinkMushroomSpore
            },
            {
                TechType.PurpleRattle,
                TechType.PurpleRattleSpore
            },
            {
                TechType.MelonPlant,
                TechType.MelonSeed
            },
            {
                TechType.Melon,
                TechType.MelonSeed
            },
            {
                TechType.SmallMelon,
                TechType.MelonSeed
            },
            {
                TechType.PurpleBrainCoral,
                TechType.PurpleBrainCoralPiece
            },
            {
                TechType.SpikePlant,
                TechType.SpikePlantSeed
            },
            {
                TechType.BluePalm,
                TechType.BluePalmSeed
            },
            {
                TechType.PurpleFan,
                TechType.PurpleFanSeed
            },
            {
                TechType.SmallFan,
                TechType.SmallFanSeed
            },
            {
                TechType.SmallFanCluster,
                TechType.SmallFanSeed
            },
            {
                TechType.PurpleTentacle,
                TechType.PurpleTentacleSeed
            },
            {
                TechType.JellyPlant,
                TechType.JellyPlantSeed
            },
            {
                TechType.GabeSFeather,
                TechType.GabeSFeatherSeed
            },
            {
                TechType.SeaCrown,
                TechType.SeaCrownSeed
            },
            {
                TechType.MembrainTree,
                TechType.MembrainTreeSeed
            },
            {
                TechType.PinkFlower,
                TechType.PinkFlowerSeed
            },
            {
                TechType.FernPalm,
                TechType.FernPalmSeed
            },
            {
                TechType.OrangePetalsPlant,
                TechType.OrangePetalsPlantSeed
            },
            {
                TechType.EyesPlant,
                TechType.EyesPlantSeed
            },
            {
                TechType.RedGreenTentacle,
                TechType.RedGreenTentacleSeed
            },
            {
                TechType.PurpleStalk,
                TechType.PurpleStalkSeed
            },
            {
                TechType.RedBasketPlant,
                TechType.RedBasketPlantSeed
            },
            {
                TechType.RedBush,
                TechType.RedBushSeed
            },
            {
                TechType.RedConePlant,
                TechType.RedConePlantSeed
            },
            {
                TechType.ShellGrass,
                TechType.ShellGrassSeed
            },
            {
                TechType.SpottedLeavesPlant,
                TechType.SpottedLeavesPlantSeed
            },
            {
                TechType.RedRollPlant,
                TechType.RedRollPlantSeed
            },
            {
                TechType.PurpleBranches,
                TechType.PurpleBranchesSeed
            },
            {
                TechType.SnakeMushroom,
                TechType.SnakeMushroomSpore
            }
        };

        private static readonly Dictionary<TechType, int> harvestFinalCutBonusList = new Dictionary<TechType, int>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.BulboTree,
                1
            },
            {
                TechType.OrangeMushroom,
                1
            },
            {
                TechType.PurpleVasePlant,
                1
            },
            {
                TechType.SmallKoosh,
                1
            },
            {
                TechType.MediumKoosh,
                1
            },
            {
                TechType.AcidMushroom,
                3
            },
            {
                TechType.WhiteMushroom,
                3
            },
            {
                TechType.PinkMushroom,
                3
            },
            {
                TechType.PurpleRattle,
                3
            },
            {
                TechType.MelonPlant,
                3
            },
            {
                TechType.SmallMelon,
                3
            },
            {
                TechType.Melon,
                3
            },
            {
                TechType.PurpleBrainCoral,
                1
            },
            {
                TechType.SpikePlant,
                1
            },
            {
                TechType.BluePalm,
                1
            },
            {
                TechType.PurpleFan,
                2
            },
            {
                TechType.SmallFan,
                1
            },
            {
                TechType.SmallFanCluster,
                4
            },
            {
                TechType.PurpleTentacle,
                2
            },
            {
                TechType.JellyPlant,
                1
            },
            {
                TechType.GabeSFeather,
                1
            },
            {
                TechType.SeaCrown,
                2
            },
            {
                TechType.MembrainTree,
                1
            },
            {
                TechType.PinkFlower,
                3
            },
            {
                TechType.FernPalm,
                1
            },
            {
                TechType.OrangePetalsPlant,
                1
            },
            {
                TechType.EyesPlant,
                1
            },
            {
                TechType.RedGreenTentacle,
                1
            },
            {
                TechType.PurpleStalk,
                1
            },
            {
                TechType.RedBasketPlant,
                1
            },
            {
                TechType.RedBush,
                1
            },
            {
                TechType.RedConePlant,
                1
            },
            {
                TechType.ShellGrass,
                1
            },
            {
                TechType.SpottedLeavesPlant,
                1
            },
            {
                TechType.RedRollPlant,
                1
            },
            {
                TechType.PurpleBranches,
                1
            },
            {
                TechType.SnakeMushroom,
                1
            }
        };

        private static readonly Dictionary<TechType, TechType> cookedCreatureList = new Dictionary<TechType, TechType>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.Bladderfish,
                TechType.CookedBladderfish
            },
            {
                TechType.Boomerang,
                TechType.CookedBoomerang
            },
            {
                TechType.LavaBoomerang,
                TechType.CookedLavaBoomerang
            },
            {
                TechType.Eyeye,
                TechType.CookedEyeye
            },
            {
                TechType.LavaEyeye,
                TechType.CookedLavaEyeye
            },
            {
                TechType.GarryFish,
                TechType.CookedGarryFish
            },
            {
                TechType.HoleFish,
                TechType.CookedHoleFish
            },
            {
                TechType.Hoopfish,
                TechType.CookedHoopfish
            },
            {
                TechType.Hoverfish,
                TechType.CookedHoverfish
            },
            {
                TechType.Oculus,
                TechType.CookedOculus
            },
            {
                TechType.Peeper,
                TechType.CookedPeeper
            },
            {
                TechType.Reginald,
                TechType.CookedReginald
            },
            {
                TechType.Spadefish,
                TechType.CookedSpadefish
            },
            {
                TechType.Spinefish,
                TechType.CookedSpinefish
            }
        };

        private static readonly Dictionary<string, TechType> entTechMap = new Dictionary<string, TechType>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<TechType, EquipmentType> equipmentTypes = new Dictionary<TechType, EquipmentType>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.Rebreather,
                EquipmentType.Head
            },
            {
                TechType.RadiationHelmet,
                EquipmentType.Head
            },
            {
                TechType.DiveSuit,
                EquipmentType.Body
            },
            {
                TechType.RadiationSuit,
                EquipmentType.Body
            },
            {
                TechType.Stillsuit,
                EquipmentType.Body
            },
            {
                TechType.ReinforcedDiveSuit,
                EquipmentType.Body
            },
            {
                TechType.Fins,
                EquipmentType.Foots
            },
            {
                TechType.UltraGlideFins,
                EquipmentType.Foots
            },
            {
                TechType.SwimChargeFins,
                EquipmentType.Foots
            },
            {
                TechType.RadiationGloves,
                EquipmentType.Gloves
            },
            {
                TechType.ReinforcedGloves,
                EquipmentType.Gloves
            },
            {
                TechType.Tank,
                EquipmentType.Tank
            },
            {
                TechType.DoubleTank,
                EquipmentType.Tank
            },
            {
                TechType.PlasteelTank,
                EquipmentType.Tank
            },
            {
                TechType.HighCapacityTank,
                EquipmentType.Tank
            },
            {
                TechType.Compass,
                EquipmentType.Chip
            },
            {
                TechType.Thermometer,
                EquipmentType.Chip
            },
            {
                TechType.Signal,
                EquipmentType.Chip
            },
            {
                TechType.HullReinforcementModule,
                EquipmentType.CyclopsModule
            },
            {
                TechType.HullReinforcementModule2,
                EquipmentType.CyclopsModule
            },
            {
                TechType.HullReinforcementModule3,
                EquipmentType.CyclopsModule
            },
            {
                TechType.CyclopsHullModule1,
                EquipmentType.CyclopsModule
            },
            {
                TechType.CyclopsHullModule2,
                EquipmentType.CyclopsModule
            },
            {
                TechType.CyclopsHullModule3,
                EquipmentType.CyclopsModule
            },
            {
                TechType.PowerUpgradeModule,
                EquipmentType.CyclopsModule
            },
            {
                TechType.CyclopsShieldModule,
                EquipmentType.CyclopsModule
            },
            {
                TechType.CyclopsSonarModule,
                EquipmentType.CyclopsModule
            },
            {
                TechType.CyclopsSeamothRepairModule,
                EquipmentType.CyclopsModule
            },
            {
                TechType.CyclopsDecoyModule,
                EquipmentType.CyclopsModule
            },
            {
                TechType.CyclopsFireSuppressionModule,
                EquipmentType.CyclopsModule
            },
            {
                TechType.CyclopsThermalReactorModule,
                EquipmentType.CyclopsModule
            },
            {
                TechType.CyclopsDecoy,
                EquipmentType.DecoySlot
            },
            {
                TechType.VehicleArmorPlating,
                EquipmentType.VehicleModule
            },
            {
                TechType.VehiclePowerUpgradeModule,
                EquipmentType.VehicleModule
            },
            {
                TechType.VehicleStorageModule,
                EquipmentType.VehicleModule
            },
            {
                TechType.LootSensorMetal,
                EquipmentType.VehicleModule
            },
            {
                TechType.LootSensorLithium,
                EquipmentType.VehicleModule
            },
            {
                TechType.LootSensorFragment,
                EquipmentType.VehicleModule
            },
            {
                TechType.VehicleHullModule1,
                EquipmentType.SeamothModule
            },
            {
                TechType.VehicleHullModule2,
                EquipmentType.SeamothModule
            },
            {
                TechType.VehicleHullModule3,
                EquipmentType.SeamothModule
            },
            {
                TechType.SeamothReinforcementModule,
                EquipmentType.SeamothModule
            },
            {
                TechType.SeamothSolarCharge,
                EquipmentType.SeamothModule
            },
            {
                TechType.SeamothElectricalDefense,
                EquipmentType.SeamothModule
            },
            {
                TechType.SeamothTorpedoModule,
                EquipmentType.SeamothModule
            },
            {
                TechType.SeamothSonarModule,
                EquipmentType.SeamothModule
            },
            {
                TechType.ExoHullModule1,
                EquipmentType.ExosuitModule
            },
            {
                TechType.ExoHullModule2,
                EquipmentType.ExosuitModule
            },
            {
                TechType.ExosuitThermalReactorModule,
                EquipmentType.ExosuitModule
            },
            {
                TechType.ExosuitJetUpgradeModule,
                EquipmentType.ExosuitModule
            },
            {
                TechType.ExosuitClawArmModule,
                EquipmentType.ExosuitArm
            },
            {
                TechType.ExosuitPropulsionArmModule,
                EquipmentType.ExosuitArm
            },
            {
                TechType.ExosuitGrapplingArmModule,
                EquipmentType.ExosuitArm
            },
            {
                TechType.ExosuitDrillArmModule,
                EquipmentType.ExosuitArm
            },
            {
                TechType.ExosuitTorpedoArmModule,
                EquipmentType.ExosuitArm
            },
            {
                TechType.ReactorRod,
                EquipmentType.NuclearReactor
            },
            {
                TechType.DepletedReactorRod,
                EquipmentType.NuclearReactor
            },
            {
                TechType.Battery,
                EquipmentType.BatteryCharger
            },
            {
                TechType.LithiumIonBattery,
                EquipmentType.BatteryCharger
            },
            {
                TechType.PrecursorIonBattery,
                EquipmentType.BatteryCharger
            },
            {
                TechType.PowerCell,
                EquipmentType.PowerCellCharger
            },
            {
                TechType.PrecursorIonPowerCell,
                EquipmentType.PowerCellCharger
            },
            {
                TechType.MapRoomHUDChip,
                EquipmentType.Chip
            },
            {
                TechType.MapRoomCamera,
                EquipmentType.Hand
            },
            {
                TechType.MapRoomUpgradeScanRange,
                EquipmentType.None
            },
            {
                TechType.MapRoomUpgradeScanSpeed,
                EquipmentType.None
            },
            {
                TechType.ScrapMetal,
                EquipmentType.Hand
            },
            {
                TechType.Knife,
                EquipmentType.Hand
            },
            {
                TechType.Drill,
                EquipmentType.Hand
            },
            {
                TechType.Flashlight,
                EquipmentType.Hand
            },
            {
                TechType.LEDLight,
                EquipmentType.Hand
            },
            {
                TechType.Beacon,
                EquipmentType.Hand
            },
            {
                TechType.Scanner,
                EquipmentType.Hand
            },
            {
                TechType.Builder,
                EquipmentType.Hand
            },
            {
                TechType.AirBladder,
                EquipmentType.Hand
            },
            {
                TechType.Terraformer,
                EquipmentType.Hand
            },
            {
                TechType.Pipe,
                EquipmentType.Hand
            },
            {
                TechType.PipeSurfaceFloater,
                EquipmentType.Hand
            },
            {
                TechType.DiveReel,
                EquipmentType.Hand
            },
            {
                TechType.Welder,
                EquipmentType.Hand
            },
            {
                TechType.Seaglide,
                EquipmentType.Hand
            },
            {
                TechType.Constructor,
                EquipmentType.Hand
            },
            {
                TechType.Transfuser,
                EquipmentType.Hand
            },
            {
                TechType.Flare,
                EquipmentType.Hand
            },
            {
                TechType.StasisRifle,
                EquipmentType.Hand
            },
            {
                TechType.PropulsionCannon,
                EquipmentType.Hand
            },
            {
                TechType.Gravsphere,
                EquipmentType.Hand
            },
            {
                TechType.SmallStorage,
                EquipmentType.Hand
            },
            {
                TechType.FireExtinguisher,
                EquipmentType.Hand
            },
            {
                TechType.DiamondBlade,
                EquipmentType.Hand
            },
            {
                TechType.HeatBlade,
                EquipmentType.Hand
            },
            {
                TechType.RepulsionCannon,
                EquipmentType.Hand
            },
            {
                TechType.PowerGlide,
                EquipmentType.Hand
            },
            {
                TechType.LaserCutter,
                EquipmentType.Hand
            },
            {
                TechType.HoleFish,
                EquipmentType.Hand
            },
            {
                TechType.Peeper,
                EquipmentType.Hand
            },
            {
                TechType.Oculus,
                EquipmentType.Hand
            },
            {
                TechType.GarryFish,
                EquipmentType.Hand
            },
            {
                TechType.Boomerang,
                EquipmentType.Hand
            },
            {
                TechType.LavaBoomerang,
                EquipmentType.Hand
            },
            {
                TechType.Eyeye,
                EquipmentType.Hand
            },
            {
                TechType.LavaEyeye,
                EquipmentType.Hand
            },
            {
                TechType.Bladderfish,
                EquipmentType.Hand
            },
            {
                TechType.Hoverfish,
                EquipmentType.Hand
            },
            {
                TechType.Reginald,
                EquipmentType.Hand
            },
            {
                TechType.Floater,
                EquipmentType.Hand
            },
            {
                TechType.Hoopfish,
                EquipmentType.Hand
            },
            {
                TechType.Spadefish,
                EquipmentType.Hand
            },
            {
                TechType.Spinefish,
                EquipmentType.Hand
            },
            {
                TechType.Poster,
                EquipmentType.Hand
            },
            {
                TechType.PosterAurora,
                EquipmentType.Hand
            },
            {
                TechType.PosterExoSuit1,
                EquipmentType.Hand
            },
            {
                TechType.PosterExoSuit2,
                EquipmentType.Hand
            },
            {
                TechType.PosterKitty,
                EquipmentType.Hand
            },
            {
                TechType.LuggageBag,
                EquipmentType.Hand
            },
            {
                TechType.ArcadeGorgetoy,
                EquipmentType.Hand
            },
            {
                TechType.LabEquipment1,
                EquipmentType.Hand
            },
            {
                TechType.LabEquipment2,
                EquipmentType.Hand
            },
            {
                TechType.LabEquipment3,
                EquipmentType.Hand
            },
            {
                TechType.Cap1,
                EquipmentType.Hand
            },
            {
                TechType.Cap2,
                EquipmentType.Hand
            },
            {
                TechType.LabContainer,
                EquipmentType.Hand
            },
            {
                TechType.LabContainer2,
                EquipmentType.Hand
            },
            {
                TechType.LabContainer3,
                EquipmentType.Hand
            },
            {
                TechType.StarshipSouvenir,
                EquipmentType.Hand
            },
            {
                TechType.ToyCar,
                EquipmentType.Hand
            }
        };

        private static readonly Dictionary<TechType, QuickSlotType> slotTypes = new Dictionary<TechType, QuickSlotType>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.VehicleHullModule1,
                QuickSlotType.Passive
            },
            {
                TechType.VehicleHullModule2,
                QuickSlotType.Passive
            },
            {
                TechType.VehicleHullModule3,
                QuickSlotType.Passive
            },
            {
                TechType.VehicleArmorPlating,
                QuickSlotType.Passive
            },
            {
                TechType.VehiclePowerUpgradeModule,
                QuickSlotType.Passive
            },
            {
                TechType.VehicleStorageModule,
                QuickSlotType.Passive
            },
            {
                TechType.LootSensorMetal,
                QuickSlotType.Toggleable
            },
            {
                TechType.LootSensorLithium,
                QuickSlotType.Toggleable
            },
            {
                TechType.LootSensorFragment,
                QuickSlotType.Toggleable
            },
            {
                TechType.SeamothReinforcementModule,
                QuickSlotType.Passive
            },
            {
                TechType.SeamothSolarCharge,
                QuickSlotType.Passive
            },
            {
                TechType.SeamothElectricalDefense,
                QuickSlotType.SelectableChargeable
            },
            {
                TechType.SeamothTorpedoModule,
                QuickSlotType.Selectable
            },
            {
                TechType.SeamothSonarModule,
                QuickSlotType.Selectable
            },
            {
                TechType.ExoHullModule1,
                QuickSlotType.Passive
            },
            {
                TechType.ExoHullModule2,
                QuickSlotType.Passive
            },
            {
                TechType.ExosuitThermalReactorModule,
                QuickSlotType.Passive
            },
            {
                TechType.ExosuitJetUpgradeModule,
                QuickSlotType.Passive
            },
            {
                TechType.ExosuitClawArmModule,
                QuickSlotType.Selectable
            },
            {
                TechType.ExosuitPropulsionArmModule,
                QuickSlotType.Selectable
            },
            {
                TechType.ExosuitGrapplingArmModule,
                QuickSlotType.Selectable
            },
            {
                TechType.ExosuitDrillArmModule,
                QuickSlotType.Selectable
            },
            {
                TechType.ExosuitTorpedoArmModule,
                QuickSlotType.Selectable
            }
        };

        private static readonly Dictionary<TechType, float> maxCharges = new Dictionary<TechType, float>(TechTypeExtensions.sTechTypeComparer) { 
        {
            TechType.SeamothElectricalDefense,
            30f
        } };

        private static readonly Dictionary<TechType, float> energyCost = new Dictionary<TechType, float>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.SeamothElectricalDefense,
                5f
            },
            {
                TechType.SeamothTorpedoModule,
                0f
            },
            {
                TechType.SeamothSonarModule,
                1f
            },
            {
                TechType.ExosuitTorpedoArmModule,
                0f
            },
            {
                TechType.ExosuitClawArmModule,
                0.1f
            },
            {
                TechType.LootSensorMetal,
                1f
            },
            {
                TechType.LootSensorLithium,
                1f
            },
            {
                TechType.LootSensorFragment,
                1f
            }
        };

        private static readonly Dictionary<TechType, string> poweredPrefab = new Dictionary<TechType, string>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.PowerTransmitter,
                "Submarine/Build/PowerTransmitter"
            },
            {
                TechType.SolarPanel,
                "Submarine/Build/SolarPanel"
            },
            {
                TechType.Bioreactor,
                "Submarine/Build/Bioreactor"
            },
            {
                TechType.ThermalPlant,
                "Submarine/Build/ThermalPlant"
            },
            {
                TechType.NuclearReactor,
                "Submarine/Build/NuclearReactor"
            }
        };

        private static Dictionary<string, TechType> entClassTechTable = null;

        private static Dictionary<TechType, string> techMapping = null;

        private static bool cacheInitialized = false;

        private static Dictionary<TechType, BackgroundType> backgroundTypes = new Dictionary<TechType, BackgroundType>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.CyclopsHullBlueprint,
                BackgroundType.Blueprint
            },
            {
                TechType.CyclopsEngineBlueprint,
                BackgroundType.Blueprint
            },
            {
                TechType.CyclopsBridgeBlueprint,
                BackgroundType.Blueprint
            },
            {
                TechType.CyclopsDockingBayBlueprint,
                BackgroundType.Blueprint
            },
            {
                TechType.CyclopsBlueprint,
                BackgroundType.Blueprint
            },
            {
                TechType.ExosuitClawArmModule,
                BackgroundType.ExosuitArm
            },
            {
                TechType.ExosuitPropulsionArmModule,
                BackgroundType.ExosuitArm
            },
            {
                TechType.ExosuitGrapplingArmModule,
                BackgroundType.ExosuitArm
            },
            {
                TechType.ExosuitDrillArmModule,
                BackgroundType.ExosuitArm
            },
            {
                TechType.ExosuitTorpedoArmModule,
                BackgroundType.ExosuitArm
            },
            {
                TechType.BluePalm,
                BackgroundType.PlantWater
            },
            {
                TechType.EyesPlant,
                BackgroundType.PlantWater
            },
            {
                TechType.GabeSFeather,
                BackgroundType.PlantWater
            },
            {
                TechType.PurpleTentacle,
                BackgroundType.PlantWater
            },
            {
                TechType.MembrainTree,
                BackgroundType.PlantWater
            },
            {
                TechType.PurpleStalk,
                BackgroundType.PlantWater
            },
            {
                TechType.RedBasketPlant,
                BackgroundType.PlantWater
            },
            {
                TechType.RedBush,
                BackgroundType.PlantWater
            },
            {
                TechType.RedConePlant,
                BackgroundType.PlantWater
            },
            {
                TechType.RedGreenTentacle,
                BackgroundType.PlantWater
            },
            {
                TechType.RedRollPlant,
                BackgroundType.PlantWater
            },
            {
                TechType.SeaCrown,
                BackgroundType.PlantWater
            },
            {
                TechType.ShellGrass,
                BackgroundType.PlantWater
            },
            {
                TechType.SnakeMushroom,
                BackgroundType.PlantWater
            },
            {
                TechType.SpottedLeavesPlant,
                BackgroundType.PlantWater
            },
            {
                TechType.PurpleBranches,
                BackgroundType.PlantWater
            },
            {
                TechType.BrainCoral,
                BackgroundType.PlantWater
            },
            {
                TechType.PurpleBrainCoral,
                BackgroundType.PlantWater
            },
            {
                TechType.BloodRoot,
                BackgroundType.PlantWater
            },
            {
                TechType.BloodVine,
                BackgroundType.PlantWater
            },
            {
                TechType.HugeKoosh,
                BackgroundType.PlantWater
            },
            {
                TechType.LargeKoosh,
                BackgroundType.PlantWater
            },
            {
                TechType.MediumKoosh,
                BackgroundType.PlantWater
            },
            {
                TechType.SmallKoosh,
                BackgroundType.PlantWater
            },
            {
                TechType.WhiteMushroom,
                BackgroundType.PlantWater
            },
            {
                TechType.PurpleBrainCoralPiece,
                BackgroundType.PlantWater
            },
            {
                TechType.AcidMushroom,
                BackgroundType.PlantWater
            },
            {
                TechType.KooshChunk,
                BackgroundType.PlantWater
            },
            {
                TechType.Creepvine,
                BackgroundType.PlantWater
            },
            {
                TechType.CreepvinePiece,
                BackgroundType.PlantWater
            },
            {
                TechType.SpikePlant,
                BackgroundType.PlantWater
            },
            {
                TechType.SmallFan,
                BackgroundType.PlantWater
            },
            {
                TechType.BloodOil,
                BackgroundType.PlantWater
            },
            {
                TechType.JellyPlant,
                BackgroundType.PlantWater
            },
            {
                TechType.PurpleFan,
                BackgroundType.PlantWater
            },
            {
                TechType.CreepvineSeedCluster,
                BackgroundType.PlantWater
            },
            {
                TechType.WhiteMushroomSpore,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.PurpleBranchesSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.MembrainTreeSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.PurpleStalkSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.PurpleTentacleSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.RedBasketPlantSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.RedBushSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.RedConePlantSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.RedGreenTentacleSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.RedRollPlantSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.SeaCrownSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.ShellGrassSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.SnakeMushroomSpore,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.SpottedLeavesPlantSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.AcidMushroomSpore,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.BluePalmSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.EyesPlantSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.GabeSFeatherSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.JellyPlantSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.PurpleFanSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.SmallFanSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.SpikePlantSeed,
                BackgroundType.PlantWaterSeed
            },
            {
                TechType.BulboTree,
                BackgroundType.PlantAir
            },
            {
                TechType.HangingFruitTree,
                BackgroundType.PlantAir
            },
            {
                TechType.Melon,
                BackgroundType.PlantAir
            },
            {
                TechType.PurpleVegetablePlant,
                BackgroundType.PlantAir
            },
            {
                TechType.SmallMelon,
                BackgroundType.PlantAir
            },
            {
                TechType.OrangePetalsPlant,
                BackgroundType.PlantAir
            },
            {
                TechType.FernPalm,
                BackgroundType.PlantAir
            },
            {
                TechType.OrangeMushroom,
                BackgroundType.PlantAir
            },
            {
                TechType.PinkFlower,
                BackgroundType.PlantAir
            },
            {
                TechType.PurpleVasePlant,
                BackgroundType.PlantAir
            },
            {
                TechType.PinkMushroom,
                BackgroundType.PlantAir
            },
            {
                TechType.MelonPlant,
                BackgroundType.PlantAir
            },
            {
                TechType.PurpleRattle,
                BackgroundType.PlantAir
            },
            {
                TechType.HangingFruit,
                BackgroundType.PlantAir
            },
            {
                TechType.BulboTreePiece,
                BackgroundType.PlantAirSeed
            },
            {
                TechType.MelonSeed,
                BackgroundType.PlantAirSeed
            },
            {
                TechType.PurpleVegetable,
                BackgroundType.PlantAirSeed
            },
            {
                TechType.OrangeMushroomSpore,
                BackgroundType.PlantAirSeed
            },
            {
                TechType.PinkMushroomSpore,
                BackgroundType.PlantAirSeed
            },
            {
                TechType.PurpleRattleSpore,
                BackgroundType.PlantAirSeed
            },
            {
                TechType.PurpleVasePlantSeed,
                BackgroundType.PlantAirSeed
            },
            {
                TechType.FernPalmSeed,
                BackgroundType.PlantAirSeed
            },
            {
                TechType.OrangePetalsPlantSeed,
                BackgroundType.PlantAirSeed
            },
            {
                TechType.PinkFlowerSeed,
                BackgroundType.PlantAirSeed
            }
        };

        private static readonly Vector2int seedSize = new Vector2int(2, 2);

        private static readonly Vector2int plantSize = new Vector2int(2, 2);

        private static readonly Dictionary<TechType, Vector2int> itemSizes = new Dictionary<TechType, Vector2int>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.ScrapMetal,
                new Vector2int(2, 2)
            },
            {
                TechType.SeaTreaderPoop,
                new Vector2int(2, 2)
            },
            {
                TechType.Constructor,
                new Vector2int(3, 3)
            },
            {
                TechType.Gravsphere,
                new Vector2int(2, 2)
            },
            {
                TechType.CyclopsDecoy,
                new Vector2int(1, 2)
            },
            {
                TechType.MapRoomCamera,
                new Vector2int(2, 2)
            },
            {
                TechType.StasisRifle,
                new Vector2int(2, 2)
            },
            {
                TechType.PropulsionCannon,
                new Vector2int(2, 2)
            },
            {
                TechType.RepulsionCannon,
                new Vector2int(2, 2)
            },
            {
                TechType.Seaglide,
                new Vector2int(2, 3)
            },
            {
                TechType.PowerGlide,
                new Vector2int(2, 3)
            },
            {
                TechType.SmallStorage,
                new Vector2int(2, 2)
            },
            {
                TechType.Knife,
                new Vector2int(1, 1)
            },
            {
                TechType.DiamondBlade,
                new Vector2int(1, 1)
            },
            {
                TechType.HeatBlade,
                new Vector2int(1, 1)
            },
            {
                TechType.Builder,
                new Vector2int(1, 1)
            },
            {
                TechType.Beacon,
                new Vector2int(1, 1)
            },
            {
                TechType.FireExtinguisher,
                new Vector2int(1, 1)
            },
            {
                TechType.Tank,
                new Vector2int(2, 3)
            },
            {
                TechType.PlasteelTank,
                new Vector2int(2, 3)
            },
            {
                TechType.DoubleTank,
                new Vector2int(2, 3)
            },
            {
                TechType.HighCapacityTank,
                new Vector2int(3, 3)
            },
            {
                TechType.Fins,
                new Vector2int(2, 2)
            },
            {
                TechType.SwimChargeFins,
                new Vector2int(2, 2)
            },
            {
                TechType.UltraGlideFins,
                new Vector2int(2, 2)
            },
            {
                TechType.RadiationGloves,
                new Vector2int(2, 2)
            },
            {
                TechType.ReinforcedGloves,
                new Vector2int(2, 2)
            },
            {
                TechType.RadiationHelmet,
                new Vector2int(2, 2)
            },
            {
                TechType.Rebreather,
                new Vector2int(2, 2)
            },
            {
                TechType.RadiationSuit,
                new Vector2int(2, 2)
            },
            {
                TechType.ReinforcedDiveSuit,
                new Vector2int(2, 2)
            },
            {
                TechType.Stillsuit,
                new Vector2int(2, 2)
            },
            {
                TechType.LabContainer,
                new Vector2int(2, 2)
            },
            {
                TechType.LabEquipment1,
                new Vector2int(2, 2)
            },
            {
                TechType.LabEquipment2,
                new Vector2int(2, 2)
            },
            {
                TechType.LabEquipment3,
                new Vector2int(2, 2)
            },
            {
                TechType.LuggageBag,
                new Vector2int(2, 2)
            },
            {
                TechType.PrecursorKey_White,
                new Vector2int(2, 2)
            },
            {
                TechType.ToyCar,
                new Vector2int(2, 2)
            },
            {
                TechType.BulboTreePiece,
                new Vector2int(2, 2)
            },
            {
                TechType.BulboTree,
                new Vector2int(2, 2)
            },
            {
                TechType.FernPalmSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.FernPalm,
                new Vector2int(2, 2)
            },
            {
                TechType.HangingFruit,
                new Vector2int(2, 2)
            },
            {
                TechType.HangingFruitTree,
                new Vector2int(2, 2)
            },
            {
                TechType.Melon,
                new Vector2int(2, 2)
            },
            {
                TechType.MelonSeed,
                new Vector2int(1, 1)
            },
            {
                TechType.MelonPlant,
                new Vector2int(1, 1)
            },
            {
                TechType.OrangeMushroomSpore,
                new Vector2int(2, 2)
            },
            {
                TechType.OrangeMushroom,
                new Vector2int(2, 2)
            },
            {
                TechType.OrangePetalsPlantSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.OrangePetalsPlant,
                new Vector2int(2, 2)
            },
            {
                TechType.PinkFlowerSeed,
                new Vector2int(1, 1)
            },
            {
                TechType.PinkFlower,
                new Vector2int(1, 1)
            },
            {
                TechType.PinkMushroomSpore,
                new Vector2int(1, 1)
            },
            {
                TechType.PinkMushroom,
                new Vector2int(1, 1)
            },
            {
                TechType.PurpleRattleSpore,
                new Vector2int(1, 1)
            },
            {
                TechType.PurpleRattle,
                new Vector2int(1, 1)
            },
            {
                TechType.PurpleVasePlantSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.PurpleVasePlant,
                new Vector2int(2, 2)
            },
            {
                TechType.PurpleVegetable,
                new Vector2int(2, 2)
            },
            {
                TechType.PurpleVegetablePlant,
                new Vector2int(2, 2)
            },
            {
                TechType.AcidMushroomSpore,
                new Vector2int(1, 1)
            },
            {
                TechType.AcidMushroom,
                new Vector2int(1, 1)
            },
            {
                TechType.BloodOil,
                new Vector2int(2, 2)
            },
            {
                TechType.BloodVine,
                new Vector2int(2, 2)
            },
            {
                TechType.BluePalmSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.BluePalm,
                new Vector2int(2, 2)
            },
            {
                TechType.CreepvinePiece,
                new Vector2int(2, 2)
            },
            {
                TechType.CreepvineSeedCluster,
                new Vector2int(2, 2)
            },
            {
                TechType.Creepvine,
                new Vector2int(2, 2)
            },
            {
                TechType.EyesPlantSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.EyesPlant,
                new Vector2int(2, 2)
            },
            {
                TechType.GabeSFeatherSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.GabeSFeather,
                new Vector2int(2, 2)
            },
            {
                TechType.JellyPlantSeed,
                new Vector2int(1, 1)
            },
            {
                TechType.JellyPlant,
                new Vector2int(1, 1)
            },
            {
                TechType.KooshChunk,
                new Vector2int(2, 2)
            },
            {
                TechType.SmallKoosh,
                new Vector2int(2, 2)
            },
            {
                TechType.MembrainTreeSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.MembrainTree,
                new Vector2int(2, 2)
            },
            {
                TechType.PurpleBrainCoralPiece,
                new Vector2int(2, 2)
            },
            {
                TechType.PurpleBrainCoral,
                new Vector2int(2, 2)
            },
            {
                TechType.PurpleBranchesSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.PurpleBranches,
                new Vector2int(2, 2)
            },
            {
                TechType.PurpleFanSeed,
                new Vector2int(1, 1)
            },
            {
                TechType.PurpleFan,
                new Vector2int(1, 1)
            },
            {
                TechType.PurpleStalkSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.PurpleStalk,
                new Vector2int(2, 2)
            },
            {
                TechType.PurpleTentacleSeed,
                new Vector2int(1, 1)
            },
            {
                TechType.PurpleTentacle,
                new Vector2int(1, 1)
            },
            {
                TechType.RedBasketPlantSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.RedBasketPlant,
                new Vector2int(2, 2)
            },
            {
                TechType.RedBushSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.RedBush,
                new Vector2int(2, 2)
            },
            {
                TechType.RedConePlantSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.RedConePlant,
                new Vector2int(2, 2)
            },
            {
                TechType.RedGreenTentacleSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.RedGreenTentacle,
                new Vector2int(2, 2)
            },
            {
                TechType.RedRollPlantSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.RedRollPlant,
                new Vector2int(2, 2)
            },
            {
                TechType.SeaCrownSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.SeaCrown,
                new Vector2int(2, 2)
            },
            {
                TechType.ShellGrassSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.ShellGrass,
                new Vector2int(2, 2)
            },
            {
                TechType.SmallFanSeed,
                new Vector2int(1, 1)
            },
            {
                TechType.SmallFan,
                new Vector2int(1, 1)
            },
            {
                TechType.SnakeMushroomSpore,
                new Vector2int(2, 2)
            },
            {
                TechType.SnakeMushroom,
                new Vector2int(2, 2)
            },
            {
                TechType.SpikePlantSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.SpikePlant,
                new Vector2int(2, 2)
            },
            {
                TechType.SpottedLeavesPlantSeed,
                new Vector2int(2, 2)
            },
            {
                TechType.SpottedLeavesPlant,
                new Vector2int(2, 2)
            },
            {
                TechType.WhiteMushroomSpore,
                new Vector2int(1, 1)
            },
            {
                TechType.WhiteMushroom,
                new Vector2int(1, 1)
            },
            {
                TechType.BonesharkEgg,
                new Vector2int(2, 2)
            },
            {
                TechType.BonesharkEggUndiscovered,
                new Vector2int(2, 2)
            },
            {
                TechType.CrabsnakeEgg,
                new Vector2int(2, 2)
            },
            {
                TechType.CrabsnakeEggUndiscovered,
                new Vector2int(2, 2)
            },
            {
                TechType.CrabsquidEgg,
                new Vector2int(2, 2)
            },
            {
                TechType.CrabsquidEggUndiscovered,
                new Vector2int(2, 2)
            },
            {
                TechType.GasopodEgg,
                new Vector2int(2, 2)
            },
            {
                TechType.GasopodEggUndiscovered,
                new Vector2int(2, 2)
            },
            {
                TechType.JellyrayEgg,
                new Vector2int(2, 2)
            },
            {
                TechType.JellyrayEggUndiscovered,
                new Vector2int(2, 2)
            },
            {
                TechType.LavaLizardEgg,
                new Vector2int(2, 2)
            },
            {
                TechType.LavaLizardEggUndiscovered,
                new Vector2int(2, 2)
            },
            {
                TechType.ReefbackEgg,
                new Vector2int(3, 3)
            },
            {
                TechType.ReefbackEggUndiscovered,
                new Vector2int(3, 3)
            },
            {
                TechType.SandsharkEgg,
                new Vector2int(2, 2)
            },
            {
                TechType.SandsharkEggUndiscovered,
                new Vector2int(2, 2)
            },
            {
                TechType.ShockerEgg,
                new Vector2int(2, 2)
            },
            {
                TechType.ShockerEggUndiscovered,
                new Vector2int(2, 2)
            },
            {
                TechType.StalkerEgg,
                new Vector2int(2, 2)
            },
            {
                TechType.StalkerEggUndiscovered,
                new Vector2int(2, 2)
            },
            {
                TechType.Jumper,
                new Vector2int(2, 2)
            },
            {
                TechType.RabbitRay,
                new Vector2int(2, 2)
            },
            {
                TechType.Mesmer,
                new Vector2int(2, 2)
            },
            {
                TechType.Crash,
                new Vector2int(2, 2)
            },
            {
                TechType.Cutefish,
                new Vector2int(2, 2)
            },
            {
                TechType.Jellyray,
                new Vector2int(3, 3)
            },
            {
                TechType.Stalker,
                new Vector2int(3, 3)
            },
            {
                TechType.LavaLizard,
                new Vector2int(3, 3)
            },
            {
                TechType.BoneShark,
                new Vector2int(3, 3)
            },
            {
                TechType.Crabsnake,
                new Vector2int(3, 3)
            },
            {
                TechType.Gasopod,
                new Vector2int(3, 3)
            },
            {
                TechType.CrabSquid,
                new Vector2int(3, 3)
            },
            {
                TechType.Shocker,
                new Vector2int(3, 3)
            },
            {
                TechType.Sandshark,
                new Vector2int(3, 3)
            },
            {
                TechType.Reefback,
                new Vector2int(4, 4)
            }
        };

        private static readonly Dictionary<TechType, TechData> techData = new Dictionary<TechType, TechData>(TechTypeExtensions.sTechTypeComparer)
        {
            {
                TechType.AdvancedWiringKit,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.WiringKit,
                        {
                            TechType.Gold,
                            2
                        },
                        TechType.ComputerChip
                    }
                }
            },
            {
                TechType.Aerogel,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.JellyPlant,
                        TechType.AluminumOxide
                    }
                }
            },
            {
                TechType.AramidFibers,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Benzene,
                        TechType.FiberMesh
                    }
                }
            },
            {
                TechType.Battery,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.AcidMushroom,
                            2
                        },
                        TechType.Copper
                    }
                }
            },
            {
                TechType.Benzene,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.BloodOil,
                        3
                    } }
                }
            },
            {
                TechType.Bleach,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Salt,
                        TechType.CoralChunk
                    }
                }
            },
            {
                TechType.ComputerChip,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.JeweledDiskPiece,
                            2
                        },
                        TechType.Gold,
                        TechType.CopperWire
                    }
                }
            },
            {
                TechType.CopperWire,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Copper,
                        2
                    } }
                }
            },
            {
                TechType.EnameledGlass,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.StalkerTooth,
                        TechType.Glass
                    }
                }
            },
            {
                TechType.FiberMesh,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.CreepvinePiece,
                        2
                    } }
                }
            },
            {
                TechType.Glass,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Quartz,
                        2
                    } }
                }
            },
            {
                TechType.HydrochloricAcid,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.WhiteMushroom,
                            3
                        },
                        TechType.Salt
                    }
                }
            },
            {
                TechType.Nanowires,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.MercuryOre,
                        TechType.ComputerChip,
                        TechType.Gold
                    }
                }
            },
            {
                TechType.PlasteelIngot,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.TitaniumIngot,
                        {
                            TechType.Lithium,
                            2
                        }
                    }
                }
            },
            {
                TechType.Polyaniline,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Gold,
                        TechType.HydrochloricAcid
                    }
                }
            },
            {
                TechType.PowerCell,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Battery,
                            2
                        },
                        TechType.Silicone
                    }
                }
            },
            {
                TechType.ReactorRod,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.UraniniteCrystal,
                            3
                        },
                        TechType.Lead,
                        TechType.Titanium,
                        TechType.Glass
                    }
                }
            },
            {
                TechType.Silicone,
                new TechData
                {
                    _craftAmount = 2,
                    _ingredients = new Ingredients { TechType.CreepvineSeedCluster }
                }
            },
            {
                TechType.Titanium,
                new TechData
                {
                    _craftAmount = 4,
                    _ingredients = new Ingredients { TechType.ScrapMetal }
                }
            },
            {
                TechType.TitaniumIngot,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        10
                    } }
                }
            },
            {
                TechType.WiringKit,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Silver,
                        2
                    } }
                }
            },
            {
                TechType.Lubricant,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.CreepvineSeedCluster }
                }
            },
            {
                TechType.PrecursorIonBattery,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PrecursorIonCrystal,
                        TechType.Gold,
                        TechType.Silver
                    }
                }
            },
            {
                TechType.PrecursorIonPowerCell,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.PrecursorIonBattery,
                            2
                        },
                        TechType.Silicone
                    }
                }
            },
            {
                TechType.CookedPeeper,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Peeper }
                }
            },
            {
                TechType.CuredPeeper,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Peeper,
                        TechType.Salt
                    }
                }
            },
            {
                TechType.CookedHoleFish,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.HoleFish }
                }
            },
            {
                TechType.CuredHoleFish,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.HoleFish,
                        TechType.Salt
                    }
                }
            },
            {
                TechType.CookedGarryFish,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.GarryFish }
                }
            },
            {
                TechType.CuredGarryFish,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.GarryFish,
                        TechType.Salt
                    }
                }
            },
            {
                TechType.CookedBladderfish,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Bladderfish }
                }
            },
            {
                TechType.CuredBladderfish,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Bladderfish,
                        TechType.Salt
                    }
                }
            },
            {
                TechType.CookedBoomerang,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Boomerang }
                }
            },
            {
                TechType.CookedLavaBoomerang,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.LavaBoomerang }
                }
            },
            {
                TechType.CuredBoomerang,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Boomerang,
                        TechType.Salt
                    }
                }
            },
            {
                TechType.CuredLavaBoomerang,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.LavaBoomerang,
                        TechType.Salt
                    }
                }
            },
            {
                TechType.CookedReginald,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Reginald }
                }
            },
            {
                TechType.CuredReginald,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Reginald,
                        TechType.Salt
                    }
                }
            },
            {
                TechType.CookedSpadefish,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Spadefish }
                }
            },
            {
                TechType.CuredSpadefish,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Spadefish,
                        TechType.Salt
                    }
                }
            },
            {
                TechType.CookedHoverfish,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Hoverfish }
                }
            },
            {
                TechType.CuredHoverfish,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Hoverfish,
                        TechType.Salt
                    }
                }
            },
            {
                TechType.CookedEyeye,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Eyeye }
                }
            },
            {
                TechType.CookedLavaEyeye,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.LavaEyeye }
                }
            },
            {
                TechType.CuredEyeye,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Eyeye,
                        TechType.Salt
                    }
                }
            },
            {
                TechType.CuredLavaEyeye,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.LavaEyeye,
                        TechType.Salt
                    }
                }
            },
            {
                TechType.CookedOculus,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Oculus }
                }
            },
            {
                TechType.CuredOculus,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Oculus,
                        TechType.Salt
                    }
                }
            },
            {
                TechType.CookedHoopfish,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Hoopfish }
                }
            },
            {
                TechType.CuredHoopfish,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Hoopfish,
                        TechType.Salt
                    }
                }
            },
            {
                TechType.CookedSpinefish,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Spinefish }
                }
            },
            {
                TechType.CuredSpinefish,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Spinefish,
                        TechType.Salt
                    }
                }
            },
            {
                TechType.FilteredWater,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Bladderfish }
                }
            },
            {
                TechType.DisinfectedWater,
                new TechData
                {
                    _craftAmount = 2,
                    _ingredients = new Ingredients { TechType.Bleach }
                }
            },
            {
                TechType.BaseCorridor,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.BaseCorridorGlass,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Glass,
                        2
                    } }
                }
            },
            {
                TechType.BaseCorridorGlassI,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Glass,
                        2
                    } }
                }
            },
            {
                TechType.BaseCorridorGlassL,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Glass,
                        2
                    } }
                }
            },
            {
                TechType.BaseCorridorI,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.BaseCorridorL,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.BaseCorridorT,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        3
                    } }
                }
            },
            {
                TechType.BaseCorridorX,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        3
                    } }
                }
            },
            {
                TechType.BaseFoundation,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            2
                        },
                        {
                            TechType.Lead,
                            2
                        }
                    }
                }
            },
            {
                TechType.BaseHatch,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Quartz,
                        {
                            TechType.Titanium,
                            2
                        }
                    }
                }
            },
            {
                TechType.BaseBulkhead,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            3
                        },
                        TechType.Silicone
                    }
                }
            },
            {
                TechType.BaseWall,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.BaseDoor,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Titanium }
                }
            },
            {
                TechType.BaseLadder,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.BaseWaterPark,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Glass,
                            5
                        },
                        {
                            TechType.Titanium,
                            2
                        }
                    }
                }
            },
            {
                TechType.BaseReinforcement,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Lithium,
                        {
                            TechType.Titanium,
                            3
                        }
                    }
                }
            },
            {
                TechType.BaseWindow,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Glass }
                }
            },
            {
                TechType.BaseUpgradeConsole,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            3
                        },
                        TechType.ComputerChip,
                        TechType.CopperWire
                    }
                }
            },
            {
                TechType.BasePlanter,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Titanium }
                }
            },
            {
                TechType.BaseFiltrationMachine,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            3
                        },
                        TechType.CopperWire,
                        TechType.Aerogel
                    }
                }
            },
            {
                TechType.BaseConnector,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.BaseRoom,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        6
                    } }
                }
            },
            {
                TechType.BaseMoonpool,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.TitaniumIngot,
                            2
                        },
                        TechType.Lubricant,
                        {
                            TechType.Lead,
                            2
                        }
                    }
                }
            },
            {
                TechType.BaseObservatory,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.EnameledGlass,
                            2
                        },
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.BaseMapRoom,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            5
                        },
                        {
                            TechType.Copper,
                            2
                        },
                        TechType.Gold,
                        TechType.JeweledDiskPiece
                    }
                }
            },
            {
                TechType.SolarPanel,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Quartz,
                            2
                        },
                        {
                            TechType.Titanium,
                            2
                        },
                        TechType.Copper
                    }
                }
            },
            {
                TechType.PowerTransmitter,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Gold,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.Accumulator,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Titanium }
                }
            },
            {
                TechType.Bioreactor,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            4
                        },
                        TechType.Silver
                    }
                }
            },
            {
                TechType.BaseBioReactor,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            3
                        },
                        TechType.WiringKit,
                        TechType.Lubricant
                    }
                }
            },
            {
                TechType.ThermalPlant,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            5
                        },
                        {
                            TechType.Magnetite,
                            2
                        },
                        TechType.Aerogel
                    }
                }
            },
            {
                TechType.NuclearReactor,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            5
                        },
                        {
                            TechType.Lithium,
                            2
                        },
                        {
                            TechType.Lead,
                            2
                        },
                        TechType.Silver,
                        TechType.AluminumOxide
                    }
                }
            },
            {
                TechType.BaseNuclearReactor,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PlasteelIngot,
                        TechType.AdvancedWiringKit,
                        {
                            TechType.Lead,
                            3
                        }
                    }
                }
            },
            {
                TechType.Flare,
                new TechData
                {
                    _craftAmount = 5,
                    _ingredients = new Ingredients { TechType.CrashPowder }
                }
            },
            {
                TechType.Flashlight,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Battery,
                        TechType.Glass
                    }
                }
            },
            {
                TechType.Scanner,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Battery,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.Builder,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.ComputerChip,
                        TechType.WiringKit,
                        TechType.Battery
                    }
                }
            },
            {
                TechType.Terraformer,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.AdvancedWiringKit,
                        TechType.Battery
                    }
                }
            },
            {
                TechType.Pipe,
                new TechData
                {
                    _craftAmount = 5,
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.PipeSurfaceFloater,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.DiveReel,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.CreepvineSeedCluster,
                            2
                        },
                        TechType.CopperWire,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.Rebreather,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.WiringKit,
                        TechType.FiberMesh
                    }
                }
            },
            {
                TechType.Fins,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Silicone,
                        2
                    } }
                }
            },
            {
                TechType.RadiationSuit,
                new TechData
                {
                    _linkedItems = new List<TechType>
                    {
                        TechType.RadiationHelmet,
                        TechType.RadiationGloves
                    },
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.FiberMesh,
                            2
                        },
                        {
                            TechType.Lead,
                            2
                        }
                    }
                }
            },
            {
                TechType.ReinforcedDiveSuit,
                new TechData
                {
                    _linkedItems = new List<TechType> { TechType.ReinforcedGloves },
                    _ingredients = new Ingredients
                    {
                        TechType.AramidFibers,
                        {
                            TechType.Diamond,
                            2
                        },
                        {
                            TechType.Titanium,
                            2
                        }
                    }
                }
            },
            {
                TechType.Stillsuit,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.AramidFibers,
                        TechType.Aerogel,
                        TechType.CopperWire
                    }
                }
            },
            {
                TechType.Knife,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Silicone,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.Seaglide,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Battery,
                        TechType.Lubricant,
                        TechType.CopperWire,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.Tank,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        3
                    } }
                }
            },
            {
                TechType.DoubleTank,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Tank,
                        {
                            TechType.Glass,
                            2
                        },
                        {
                            TechType.Titanium,
                            4
                        },
                        TechType.Silver
                    }
                }
            },
            {
                TechType.Gravsphere,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Battery,
                        TechType.Copper,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.SmallStorage,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        4
                    } }
                }
            },
            {
                TechType.AirBladder,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Silicone,
                        TechType.Bladderfish
                    }
                }
            },
            {
                TechType.FirstAidKit,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.FiberMesh }
                }
            },
            {
                TechType.Welder,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Silicone,
                        TechType.CrashPowder,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.LaserCutter,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Diamond,
                            2
                        },
                        TechType.Battery,
                        TechType.Titanium,
                        TechType.CrashPowder
                    }
                }
            },
            {
                TechType.FireExtinguisher,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        3
                    } }
                }
            },
            {
                TechType.Beacon,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Copper,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.CyclopsDecoy,
                new TechData
                {
                    _craftAmount = 3,
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            3
                        },
                        TechType.WiringKit
                    }
                }
            },
            {
                TechType.CurrentGenerator,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Battery,
                        TechType.Lubricant,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.Compass,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.CopperWire,
                        TechType.WiringKit
                    }
                }
            },
            {
                TechType.Thermometer,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.ComputerChip }
                }
            },
            {
                TechType.StasisRifle,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.ComputerChip,
                        TechType.Battery,
                        {
                            TechType.Magnetite,
                            2
                        },
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.PropulsionCannon,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.WiringKit,
                        TechType.Battery,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.Constructor,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.TitaniumIngot,
                        TechType.Lubricant,
                        TechType.PowerCell
                    }
                }
            },
            {
                TechType.LEDLight,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Battery,
                        TechType.Titanium,
                        TechType.Glass
                    }
                }
            },
            {
                TechType.HatchingEnzymes,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.EyesPlantSeed,
                        TechType.SeaCrownSeed,
                        TechType.TreeMushroomPiece,
                        TechType.RedGreenTentacleSeed,
                        TechType.KooshChunk
                    }
                }
            },
            {
                TechType.Transfuser,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.AdvancedWiringKit,
                        TechType.Battery,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.DiamondBlade,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Knife,
                        TechType.Diamond
                    }
                }
            },
            {
                TechType.HeatBlade,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Knife,
                        TechType.Battery
                    }
                }
            },
            {
                TechType.LithiumIonBattery,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Battery,
                        TechType.Lithium
                    }
                }
            },
            {
                TechType.PlasteelTank,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.DoubleTank,
                        TechType.PlasteelIngot
                    }
                }
            },
            {
                TechType.HighCapacityTank,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.DoubleTank,
                        {
                            TechType.Lithium,
                            4
                        }
                    }
                }
            },
            {
                TechType.UltraGlideFins,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Fins,
                        {
                            TechType.Silicone,
                            2
                        },
                        TechType.Titanium,
                        TechType.Lithium
                    }
                }
            },
            {
                TechType.SwimChargeFins,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Fins,
                        TechType.Polyaniline,
                        TechType.WiringKit
                    }
                }
            },
            {
                TechType.RepulsionCannon,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PropulsionCannon,
                        TechType.ComputerChip,
                        {
                            TechType.Magnetite,
                            2
                        }
                    }
                }
            },
            {
                TechType.PowerGlide,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PowerGlide,
                        TechType.WiringKit
                    }
                }
            },
            {
                TechType.VehiclePowerUpgradeModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.ComputerChip,
                        TechType.Polyaniline
                    }
                }
            },
            {
                TechType.VehicleStorageModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            3
                        },
                        TechType.Lithium
                    }
                }
            },
            {
                TechType.VehicleArmorPlating,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            3
                        },
                        TechType.Lithium,
                        {
                            TechType.Diamond,
                            4
                        }
                    }
                }
            },
            {
                TechType.WhirlpoolTorpedo,
                new TechData
                {
                    _craftAmount = 2,
                    _ingredients = new Ingredients
                    {
                        TechType.Titanium,
                        TechType.Magnetite
                    }
                }
            },
            {
                TechType.GasTorpedo,
                new TechData
                {
                    _craftAmount = 2,
                    _ingredients = new Ingredients
                    {
                        TechType.Titanium,
                        TechType.GasPod
                    }
                }
            },
            {
                TechType.SeamothSolarCharge,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.AdvancedWiringKit,
                        TechType.EnameledGlass
                    }
                }
            },
            {
                TechType.SeamothElectricalDefense,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Polyaniline,
                        TechType.WiringKit
                    }
                }
            },
            {
                TechType.SeamothTorpedoModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            3
                        },
                        TechType.Lithium,
                        TechType.Aerogel
                    }
                }
            },
            {
                TechType.SeamothSonarModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.CopperWire,
                        {
                            TechType.Magnetite,
                            2
                        }
                    }
                }
            },
            {
                TechType.VehicleHullModule1,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.TitaniumIngot,
                        {
                            TechType.Glass,
                            2
                        }
                    }
                }
            },
            {
                TechType.VehicleHullModule2,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.VehicleHullModule1,
                        TechType.PlasteelIngot,
                        {
                            TechType.Magnetite,
                            2
                        },
                        TechType.EnameledGlass
                    }
                }
            },
            {
                TechType.VehicleHullModule3,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.VehicleHullModule2,
                        TechType.PlasteelIngot,
                        {
                            TechType.AluminumOxide,
                            3
                        }
                    }
                }
            },
            {
                TechType.ExoHullModule1,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PlasteelIngot,
                        {
                            TechType.Nickel,
                            3
                        },
                        {
                            TechType.AluminumOxide,
                            2
                        }
                    }
                }
            },
            {
                TechType.ExoHullModule2,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.ExoHullModule1,
                        {
                            TechType.Titanium,
                            5
                        },
                        {
                            TechType.Lithium,
                            2
                        },
                        {
                            TechType.Kyanite,
                            3
                        }
                    }
                }
            },
            {
                TechType.ExosuitDrillArmModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            5
                        },
                        TechType.Lithium,
                        {
                            TechType.Diamond,
                            4
                        }
                    }
                }
            },
            {
                TechType.ExosuitGrapplingArmModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.AdvancedWiringKit,
                        TechType.Benzene,
                        {
                            TechType.Titanium,
                            5
                        },
                        TechType.Lithium
                    }
                }
            },
            {
                TechType.ExosuitPropulsionArmModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.ComputerChip,
                        {
                            TechType.Titanium,
                            5
                        },
                        {
                            TechType.Magnetite,
                            2
                        },
                        TechType.Lithium
                    }
                }
            },
            {
                TechType.ExosuitJetUpgradeModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Nickel,
                            2
                        },
                        {
                            TechType.Sulphur,
                            3
                        },
                        {
                            TechType.Titanium,
                            5
                        },
                        TechType.Lithium
                    }
                }
            },
            {
                TechType.ExosuitThermalReactorModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Polyaniline,
                            2
                        },
                        {
                            TechType.Kyanite,
                            2
                        },
                        TechType.WiringKit
                    }
                }
            },
            {
                TechType.ExosuitTorpedoArmModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            5
                        },
                        TechType.Lithium,
                        TechType.Aerogel
                    }
                }
            },
            {
                TechType.SeamothReinforcementModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PlasteelIngot,
                        TechType.ComputerChip
                    }
                }
            },
            {
                TechType.LootSensorMetal,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PlasteelIngot,
                        TechType.ComputerChip
                    }
                }
            },
            {
                TechType.LootSensorLithium,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PlasteelIngot,
                        TechType.ComputerChip
                    }
                }
            },
            {
                TechType.LootSensorFragment,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PlasteelIngot,
                        TechType.ComputerChip
                    }
                }
            },
            {
                TechType.MapRoomHUDChip,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.ComputerChip,
                        TechType.Magnetite
                    }
                }
            },
            {
                TechType.MapRoomCamera,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.ComputerChip,
                        TechType.Battery,
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.MapRoomUpgradeScanRange,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Copper,
                        TechType.Magnetite
                    }
                }
            },
            {
                TechType.MapRoomUpgradeScanSpeed,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Silver,
                        TechType.Gold
                    }
                }
            },
            {
                TechType.Fabricator,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Titanium,
                        TechType.Gold,
                        TechType.JeweledDiskPiece
                    }
                }
            },
            {
                TechType.Spotlight,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        {
                            TechType.Titanium,
                            2
                        }
                    }
                }
            },
            {
                TechType.BasePipeConnector,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.Aquarium,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Glass,
                            2
                        },
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.Bench,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.Bed1,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            2
                        },
                        TechType.FiberMesh
                    }
                }
            },
            {
                TechType.Bed2,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            2
                        },
                        TechType.FiberMesh
                    }
                }
            },
            {
                TechType.NarrowBed,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Titanium,
                        TechType.FiberMesh
                    }
                }
            },
            {
                TechType.PlanterPot,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.PlanterPot2,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.PlanterPot3,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.PlanterBox,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        4
                    } }
                }
            },
            {
                TechType.PlanterShelf,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Titanium }
                }
            },
            {
                TechType.Locker,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Quartz,
                        {
                            TechType.Titanium,
                            2
                        }
                    }
                }
            },
            {
                TechType.SmallLocker,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.HullReinforcementModule,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.PlasteelIngot }
                }
            },
            {
                TechType.HullReinforcementModule2,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.ReefbackShell,
                        TechType.HullReinforcementModule,
                        TechType.ReefbackDNA
                    }
                }
            },
            {
                TechType.HullReinforcementModule3,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.ReefbackAdvancedStructure,
                        TechType.HullReinforcementModule2,
                        TechType.ReefbackDNA
                    }
                }
            },
            {
                TechType.CyclopsHullModule1,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PlasteelIngot,
                        {
                            TechType.AluminumOxide,
                            3
                        }
                    }
                }
            },
            {
                TechType.CyclopsHullModule2,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.CyclopsHullModule1,
                        TechType.PlasteelIngot,
                        {
                            TechType.Nickel,
                            3
                        }
                    }
                }
            },
            {
                TechType.CyclopsHullModule3,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.CyclopsHullModule2,
                        TechType.PlasteelIngot,
                        {
                            TechType.Kyanite,
                            3
                        }
                    }
                }
            },
            {
                TechType.CyclopsShieldModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.AdvancedWiringKit,
                        TechType.Polyaniline,
                        TechType.PowerCell
                    }
                }
            },
            {
                TechType.CyclopsSonarModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.ComputerChip,
                        {
                            TechType.Magnetite,
                            3
                        }
                    }
                }
            },
            {
                TechType.CyclopsSeamothRepairModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Welder,
                        TechType.CopperWire
                    }
                }
            },
            {
                TechType.CyclopsThermalReactorModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Polyaniline,
                            2
                        },
                        {
                            TechType.Kyanite,
                            4
                        },
                        TechType.WiringKit
                    }
                }
            },
            {
                TechType.CyclopsFireSuppressionModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Aerogel,
                            2
                        },
                        {
                            TechType.Sulphur,
                            2
                        }
                    }
                }
            },
            {
                TechType.CyclopsDecoyModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.Titanium,
                            3
                        },
                        {
                            TechType.Lithium,
                            2
                        },
                        TechType.Aerogel
                    }
                }
            },
            {
                TechType.PowerUpgradeModule,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.ComputerChip,
                        TechType.Benzene,
                        TechType.Polyaniline
                    }
                }
            },
            {
                TechType.SpecimenAnalyzer,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.WiringKit,
                        TechType.ComputerChip,
                        {
                            TechType.Titanium,
                            2
                        }
                    }
                }
            },
            {
                TechType.Workbench,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.ComputerChip,
                        TechType.Titanium,
                        TechType.Diamond,
                        TechType.Lead
                    }
                }
            },
            {
                TechType.Centrifuge,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.WiringKit,
                        TechType.ComputerChip,
                        {
                            TechType.Titanium,
                            2
                        }
                    }
                }
            },
            {
                TechType.Sign,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.CopperWire }
                }
            },
            {
                TechType.PictureFrame,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.CopperWire }
                }
            },
            {
                TechType.FarmingTray,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.Techlight,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.StarshipCargoCrate,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Titanium }
                }
            },
            {
                TechType.StarshipCircuitBox,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Titanium }
                }
            },
            {
                TechType.StarshipDesk,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Titanium }
                }
            },
            {
                TechType.StarshipChair,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Titanium }
                }
            },
            {
                TechType.StarshipChair2,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Titanium }
                }
            },
            {
                TechType.StarshipChair3,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Titanium }
                }
            },
            {
                TechType.StarshipMonitor,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Titanium }
                }
            },
            {
                TechType.Radio,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Titanium,
                        TechType.Copper
                    }
                }
            },
            {
                TechType.MedicalCabinet,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.ComputerChip,
                        TechType.FiberMesh,
                        TechType.Silver,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.SingleWallShelf,
                new TechData
                {
                    _ingredients = new Ingredients { TechType.Titanium }
                }
            },
            {
                TechType.WallShelves,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.BarTable,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.Trashcans,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.LabTrashcan,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.VendingMachine,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        {
                            TechType.Titanium,
                            2
                        }
                    }
                }
            },
            {
                TechType.CoffeeVendingMachine,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.LabCounter,
                new TechData
                {
                    _ingredients = new Ingredients { 
                    {
                        TechType.Titanium,
                        2
                    } }
                }
            },
            {
                TechType.BatteryCharger,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.WiringKit,
                        TechType.CopperWire,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.PowerCellCharger,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.AdvancedWiringKit,
                        {
                            TechType.AluminumOxide,
                            2
                        },
                        {
                            TechType.Titanium,
                            2
                        }
                    }
                }
            },
            {
                TechType.Seamoth,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.TitaniumIngot,
                        TechType.PowerCell,
                        {
                            TechType.Glass,
                            2
                        },
                        TechType.Lubricant,
                        TechType.Lead
                    }
                }
            },
            {
                TechType.Cyclops,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.PlasteelIngot,
                            3
                        },
                        {
                            TechType.EnameledGlass,
                            3
                        },
                        TechType.Lubricant,
                        TechType.AdvancedWiringKit,
                        {
                            TechType.Lead,
                            3
                        }
                    }
                }
            },
            {
                TechType.Exosuit,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.PlasteelIngot,
                            2
                        },
                        {
                            TechType.Aerogel,
                            2
                        },
                        TechType.EnameledGlass,
                        {
                            TechType.Diamond,
                            2
                        },
                        {
                            TechType.Lead,
                            2
                        }
                    }
                }
            },
            {
                TechType.RocketBase,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        {
                            TechType.TitaniumIngot,
                            2
                        },
                        TechType.ComputerChip,
                        {
                            TechType.Lead,
                            4
                        }
                    }
                }
            },
            {
                TechType.RocketBaseLadder,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PlasteelIngot,
                        TechType.CopperWire,
                        TechType.Lubricant
                    }
                }
            },
            {
                TechType.RocketStage1,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PlasteelIngot,
                        {
                            TechType.Nickel,
                            3
                        },
                        {
                            TechType.Aerogel,
                            2
                        },
                        TechType.WiringKit
                    }
                }
            },
            {
                TechType.RocketStage2,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PlasteelIngot,
                        {
                            TechType.Sulphur,
                            4
                        },
                        {
                            TechType.Kyanite,
                            4
                        },
                        {
                            TechType.PrecursorIonPowerCell,
                            2
                        }
                    }
                }
            },
            {
                TechType.RocketStage3,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.CyclopsShieldModule,
                        TechType.PlasteelIngot,
                        TechType.EnameledGlass,
                        TechType.ComputerChip
                    }
                }
            },
            {
                TechType.PrecursorKey_Purple,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PrecursorIonCrystal,
                        {
                            TechType.Diamond,
                            2
                        }
                    }
                }
            },
            {
                TechType.PrecursorKey_Blue,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PrecursorIonCrystal,
                        {
                            TechType.Kyanite,
                            2
                        }
                    }
                }
            },
            {
                TechType.PrecursorKey_Orange,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.PrecursorIonCrystal,
                        {
                            TechType.Nickel,
                            2
                        }
                    }
                }
            },
            {
                TechType.FiltrationMachine,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.WiringKit,
                        {
                            TechType.Glass,
                            2
                        },
                        {
                            TechType.Titanium,
                            4
                        }
                    }
                }
            },
            {
                TechType.DevTestItem,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.SpecialHullPlate,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.BikemanHullPlate,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.EatMyDictionHullPlate,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.DioramaHullPlate,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.MarkiplierHullPlate,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.MuyskermHullPlate,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.LordMinionHullPlate,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.JackSepticEyeHullPlate,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.IGPHullPlate,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.GilathissHullPlate,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.Marki1,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.Marki2,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.JackSepticEye,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            },
            {
                TechType.EatMyDiction,
                new TechData
                {
                    _ingredients = new Ingredients
                    {
                        TechType.Glass,
                        TechType.Titanium
                    }
                }
            }
        };

        public static bool IsAllowed(TechType techType)
        {
            if (!Application.isEditor)
            {
                return !blacklist.Contains(techType);
            }
            return true;
        }

        public static HashSet<TechType> FilterAllowed(HashSet<TechType> techTypes)
        {
            if (Application.isEditor)
            {
                return techTypes;
            }
            HashSet<TechType> hashSet = new HashSet<TechType>();
            HashSet<TechType>.Enumerator enumerator = techTypes.GetEnumerator();
            while (enumerator.MoveNext())
            {
                TechType current = enumerator.Current;
                if (!blacklist.Contains(current))
                {
                    hashSet.Add(current);
                }
            }
            return hashSet;
        }

        public static TechType GetTechForEntNameExpensive(string prefabName)
        {
            ProfilingUtils.BeginSample("CraftData.GetTechForEntNameExpensive");
            PrepareEntTechCache();
            TechType orDefault = entTechMap.GetOrDefault(prefabName, TechType.None);
            ProfilingUtils.EndSample();
            return orDefault;
        }

        public static HarvestType GetHarvestTypeExpensive(string prefabName)
        {
            return GetHarvestTypeFromTech(GetTechForEntNameExpensive(prefabName));
        }

        public static void DebugLogDatabase()
        {
            PreparePrefabIDCache();
            string text = "craftdata_log.txt";
            using StreamWriter writer = FileUtils.CreateTextFile(text);
            DebugWrite(writer, "BEGIN DebugLogDatabase (" + entClassTechTable.Count + " prefabs known, " + techMapping.Count + " tech types known)");
            foreach (KeyValuePair<string, TechType> item in entClassTechTable)
            {
                DebugWrite(writer, "  \"" + item.Key + "\" -> " + item.Value);
            }
            DebugWrite(writer, "-------------");
            foreach (KeyValuePair<TechType, string> item2 in techMapping)
            {
                DebugWrite(writer, string.Concat("  \"", item2.Key, "\" -> ", item2.Value));
            }
            DebugWrite(writer, "END DebugLogDatabase (see " + text + ")");
        }

        private static void DebugWrite(StreamWriter writer, string value)
        {
            writer.WriteLine(value);
            Debug.Log(value);
        }

        public static void RebuildDatabase()
        {
            cacheInitialized = false;
            PreparePrefabIDCache();
        }

        public static void PreparePrefabIDCache()
        {
            if (cacheInitialized)
            {
                return;
            }
            entClassTechTable = new Dictionary<string, TechType>();
            techMapping = new Dictionary<TechType, string>(TechTypeExtensions.sTechTypeComparer);
            PrefabDatabase.LoadPrefabDatabase(SNUtils.prefabDatabaseFilename);
            Debug.LogFormat("Caching tech types for {0} prefabs", PrefabDatabase.prefabFiles.Count);
            foreach (KeyValuePair<string, string> prefabFile in PrefabDatabase.prefabFiles)
            {
                AddToCache(prefabFile.Key, prefabFile.Value);
            }
            cacheInitialized = true;
        }

        private static void PrepareEntTechCache()
        {
            if (entTechMap.Count <= 0)
            {
                EntTechData entTechData = Resources.Load<EntTechData>("EntTechData");
                EntTechData.Entry[] array = entTechData.entTechMap;
                foreach (EntTechData.Entry entry in array)
                {
                    entTechMap[entry.prefabName] = entry.techType;
                }
                Resources.UnloadAsset(entTechData);
            }
        }

        private static void AddToCache(string classId, string filename)
        {
            TechType techForEntNameExpensive = GetTechForEntNameExpensive(Path.GetFileName(filename));
            entClassTechTable[classId] = techForEntNameExpensive;
            if (techForEntNameExpensive != 0)
            {
                techMapping[techForEntNameExpensive] = classId;
            }
        }

        public static string GetClassIdForTechType(TechType techType)
        {
            PreparePrefabIDCache();
            return techMapping.GetOrDefault(techType, null);
        }

        public static GameObject GetPrefabForTechType(TechType techType, bool verbose = true)
        {
            PreparePrefabIDCache();
            if (!techMapping.TryGetValue(techType, out var value))
            {
                if (verbose)
                {
                    Debug.LogError(string.Concat("Could not find prefab class id for tech type ", techType, ". Probably missing from CraftData.cs"));
                }
                return null;
            }
            if (!PrefabDatabase.TryGetPrefab(value, out var prefab))
            {
                if (verbose)
                {
                    Debug.LogError(string.Concat("Could not find prefab for class id ", value, " (tech type ", techType, "). Probably mising from prefab database"));
                }
                return null;
            }
            return prefab;
        }

        public static GameObject InstantiateFromPrefab(TechType techType, bool customOnly = false)
        {
            GameObject prefabForTechType = GetPrefabForTechType(techType, customOnly);
            if (prefabForTechType != null)
            {
                return Utils.SpawnFromPrefab(prefabForTechType, null);
            }
            if (!customOnly)
            {
                return Utils.CreateGenericLoot(techType);
            }
            return null;
        }

        public static IEnumerator AddToInventoryRoutine(TechType techType, int num = 1, bool noMessage = false, bool spawnIfCantAdd = true, IOut<GameObject> result = null)
        {
            for (int i = 0; i < num; i++)
            {
                TaskResult<GameObject> currentResult = new TaskResult<GameObject>();
                yield return GetPrefabForTechTypeAsync(techType, verbose: false, currentResult);
                GameObject gameObject = currentResult.Get();
                GameObject gameObject2 = ((!(gameObject != null)) ? Utils.CreateGenericLoot(techType) : Utils.SpawnFromPrefab(gameObject, null));
                if (!(gameObject2 != null))
                {
                    continue;
                }
                gameObject2.transform.position = MainCamera.camera.transform.position + MainCamera.camera.transform.forward * 3f;
                CrafterLogic.NotifyCraftEnd(gameObject2, techType);
                Pickupable component = gameObject2.GetComponent<Pickupable>();
                Inventory inventory = Inventory.Get();
                if (!(component != null) || !(inventory != null))
                {
                    continue;
                }
                if (!inventory.HasRoomFor(component) || !inventory.Pickup(component, noMessage))
                {
                    ErrorMessage.AddError(Language.main.Get("InventoryFull"));
                    if (!spawnIfCantAdd)
                    {
                        global::UnityEngine.Object.Destroy(gameObject2);
                    }
                }
                else
                {
                    result?.Set(gameObject2);
                }
            }
        }

        public static void AddToInventory(TechType techType, int num = 1, bool noMessage = false, bool spawnIfCantAdd = true)
        {
            CoroutineHost.StartCoroutine(AddToInventoryRoutine(techType, num, noMessage, spawnIfCantAdd));
        }

        public static GameObject AddToInventorySync(TechType techType, int num = 1, bool noMessage = false, bool spawnIfCantAdd = true)
        {
            GameObject result = null;
            GameObject gameObject = null;
            for (int i = 0; i < num; i++)
            {
                gameObject = InstantiateFromPrefab(techType);
                if (!(gameObject != null))
                {
                    continue;
                }
                gameObject.transform.position = MainCamera.camera.transform.position + MainCamera.camera.transform.forward * 3f;
                CrafterLogic.NotifyCraftEnd(gameObject, techType);
                Pickupable component = gameObject.GetComponent<Pickupable>();
                Inventory inventory = Inventory.Get();
                if (!(component != null) || !(inventory != null))
                {
                    continue;
                }
                if (!inventory.HasRoomFor(component) || !inventory.Pickup(component, noMessage))
                {
                    ErrorMessage.AddError(Language.main.Get("InventoryFull"));
                    if (!spawnIfCantAdd)
                    {
                        global::UnityEngine.Object.Destroy(gameObject);
                    }
                }
                else
                {
                    result = gameObject;
                }
            }
            return result;
        }

        public static CoroutineTask<GameObject> GetPrefabForTechTypeAsync(TechType techType, bool verbose = true)
        {
            TaskResult<GameObject> result = new TaskResult<GameObject>();
            return new CoroutineTask<GameObject>(GetPrefabForTechTypeAsync(techType, verbose, result), result);
        }

        private static IEnumerator GetPrefabForTechTypeAsync(TechType techType, bool verbose, IOut<GameObject> result)
        {
            PreparePrefabIDCache();
            if (!techMapping.TryGetValue(techType, out var classId))
            {
                if (verbose)
                {
                    Debug.LogError(string.Concat("Could not find prefab class id for tech type ", techType, ". Probably missing from CraftData.cs"));
                }
                result.Set(null);
                yield break;
            }
            IPrefabRequest request = PrefabDatabase.GetPrefabAsync(classId);
            yield return request;
            if (!request.TryGetPrefab(out var prefab))
            {
                if (verbose)
                {
                    Debug.LogError(string.Concat("Could not find prefab for class id ", classId, " (tech type ", techType, "). Probably mising from prefab database"));
                }
                result.Set(null);
            }
            else
            {
                result.Set(prefab);
            }
        }

        public static TechType GetTechType(GameObject obj)
        {
            GameObject go;
            return GetTechType(obj, out go);
        }

        public static TechType GetTechType(GameObject obj, out GameObject go)
        {
            ProfilingUtils.BeginSample("GetTechType");
            try
            {
                PreparePrefabIDCache();
                Transform transform = obj.transform;
                TechTag component;
                PrefabIdentifier component2;
                do
                {
                    component = transform.GetComponent<TechTag>();
                    component2 = transform.GetComponent<PrefabIdentifier>();
                    transform = transform.parent;
                }
                while (transform != null && component == null && component2 == null);
                if (component != null)
                {
                    go = component.gameObject;
                    return component.type;
                }
                if (component2 != null)
                {
                    go = component2.gameObject;
                    return entClassTechTable.GetOrDefault(component2.ClassId, TechType.None);
                }
                go = null;
                return TechType.None;
            }
            finally
            {
                ProfilingUtils.EndSample();
            }
        }

        public static bool IsBuildableTech(TechType recipe)
        {
            return buildables.Contains(recipe);
        }

        public static GameObject GetBuildPrefab(TechType recipe)
        {
            return GetPrefabForTechType(recipe);
        }

        public static HarvestType GetHarvestTypeFromTech(TechType techType)
        {
            return harvestTypeList.GetOrDefault(techType, HarvestType.None);
        }

        public static TechType GetHarvestOutputData(TechType techType)
        {
            return harvestOutputList.GetOrDefault(techType, TechType.None);
        }

        public static int GetHarvestFinalCutBonus(TechType techType)
        {
            return harvestFinalCutBonusList.GetOrDefault(techType, 0);
        }

        public static TechType GetCookedData(TechType techType)
        {
            return cookedCreatureList.GetOrDefault(techType, TechType.None);
        }

        public static float GetResearchTime(TechType techType)
        {
            return 10f;
        }

        public static string GetDropSound(TechType techType)
        {
            return dropSoundList.GetOrDefault(techType, "event:/tools/pda/drop_item");
        }

        public static string GetPickupSound(TechType techType)
        {
            string text = "event:/loot/pickup_" + techType.AsString(lowercase: true);
            EventInstance @event = FMODUWE.GetEvent(text);
            if (!@event.isValid())
            {
                text = pickupSoundList.GetOrDefault(techType, "event:/loot/pickup_default");
                @event = FMODUWE.GetEvent(text);
            }
            if (!@event.isValid())
            {
                Debug.LogWarningFormat("Pickup sound for TechType.{0} is not found at path '{1}'!", techType, text);
            }
            return text;
        }

        public static string GetUseEatSound(TechType techType)
        {
            return useEatSound.GetOrDefault(techType, "event:/player/eat");
        }

        public static string GetPoweredPrefabName(TechType techType)
        {
            return poweredPrefab.GetOrDefault(techType, "");
        }

        public static void ProcessFragment(GameObject original, GameObject fragment)
        {
            TechType techType = GetTechType(original);
            if (techType != 0 && GetHarvestTypeFromTech(techType) == HarvestType.Break)
            {
                TechType harvestOutputData = GetHarvestOutputData(techType);
                if (harvestOutputData != 0)
                {
                    Pickupable pickupable = fragment.AddComponent<Pickupable>();
                    pickupable.SetTechTypeOverride(harvestOutputData);
                    pickupable.cubeOnPickup = true;
                }
            }
        }

        public static QuickSlotType GetQuickSlotType(TechType techType)
        {
            if (slotTypes.TryGetValue(techType, out var value))
            {
                return value;
            }
            if (GetEquipmentType(techType) == EquipmentType.Hand)
            {
                return QuickSlotType.Selectable;
            }
            return QuickSlotType.None;
        }

        public static float GetQuickSlotMaxCharge(TechType techType)
        {
            if (maxCharges.TryGetValue(techType, out var value))
            {
                return value;
            }
            return -1f;
        }

        public static EquipmentType GetEquipmentType(TechType techType)
        {
            if (equipmentTypes.TryGetValue(techType, out var value))
            {
                return value;
            }
            return EquipmentType.None;
        }

        public static bool IsInvUseable(TechType techType)
        {
            if (techType != TechType.Bladderfish)
            {
                return techType == TechType.FirstAidKit;
            }
            return true;
        }

        public static void GetBuilderCategories(TechGroup group, List<TechCategory> result, bool append = false)
        {
            if (!append)
            {
                result.Clear();
            }
            if (groups.TryGetValue(group, out var value))
            {
                Dictionary<TechCategory, List<TechType>>.Enumerator enumerator = value.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    result.Add(enumerator.Current.Key);
                }
            }
        }

        public static void GetBuilderTech(TechGroup group, TechCategory category, List<TechType> result, bool append = false)
        {
            if (!append)
            {
                result.Clear();
            }
            if (groups.TryGetValue(group, out var value) && value.TryGetValue(category, out var value2))
            {
                for (int i = 0; i < value2.Count; i++)
                {
                    TechType item = value2[i];
                    result.Add(item);
                }
            }
        }

        public static void GetBuilderGroupTech(TechGroup group, List<TechType> result, bool append = false)
        {
            if (!append)
            {
                result.Clear();
            }
            if (!groups.TryGetValue(group, out var value))
            {
                return;
            }
            Dictionary<TechCategory, List<TechType>>.Enumerator enumerator = value.GetEnumerator();
            while (enumerator.MoveNext())
            {
                List<TechType> value2 = enumerator.Current.Value;
                for (int i = 0; i < value2.Count; i++)
                {
                    result.Add(value2[i]);
                }
            }
        }

        public static bool GetBuilderIndex(TechType techType, out TechGroup group, out TechCategory category, out int index)
        {
            Dictionary<TechGroup, Dictionary<TechCategory, List<TechType>>>.Enumerator enumerator = groups.GetEnumerator();
            while (enumerator.MoveNext())
            {
                KeyValuePair<TechGroup, Dictionary<TechCategory, List<TechType>>> current = enumerator.Current;
                TechGroup key = current.Key;
                Dictionary<TechCategory, List<TechType>>.Enumerator enumerator2 = current.Value.GetEnumerator();
                while (enumerator2.MoveNext())
                {
                    KeyValuePair<TechCategory, List<TechType>> current2 = enumerator2.Current;
                    TechCategory key2 = current2.Key;
                    int num = current2.Value.IndexOf(techType);
                    if (num != -1)
                    {
                        group = key;
                        category = key2;
                        index = num;
                        return true;
                    }
                }
            }
            group = TechGroup.Miscellaneous;
            category = TechCategory.Misc;
            index = int.MaxValue;
            return false;
        }

        public static bool GetEnergyCost(TechType techType, out float result)
        {
            if (techType != 0)
            {
                return energyCost.TryGetValue(techType, out result);
            }
            result = 0f;
            return false;
        }

        public static bool GetCraftTime(TechType techType, out float result)
        {
            if (craftingTimes.TryGetValue(techType, out result))
            {
                return true;
            }
            result = 0f;
            return false;
        }

        public static BackgroundType GetBackgroundType(TechType techType)
        {
            if (backgroundTypes.TryGetValue(techType, out var value))
            {
                return value;
            }
            return BackgroundType.Normal;
        }

        public static Vector2int GetItemSize(TechType techType)
        {
            if (itemSizes.TryGetValue(techType, out var value))
            {
                return value;
            }
            return new Vector2int(1, 1);
        }

        public static ITechData Get(TechType techType, bool skipWarnings = false)
        {
            if (techData.TryGetValue(techType, out var value))
            {
                return value;
            }
            if (!skipWarnings)
            {
                Debug.LogError("tried to look up ITechData for TechType that does not exist in CraftData: \"" + techType.AsString() + "\"");
            }
            return null;
        }
    }
}
