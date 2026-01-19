using Exiled.API.Features;
using Exiled.CustomRoles;
using Exiled.CustomRoles.API.Features;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Next_generationSite_27.UnionP.Scp5k.Scp5k_Control;

namespace Next_generationSite_27.UnionP.Scp5k
{
    class Scp5kFFManager : BaseClass,IFFManager
    {
        // 预构建的快速查找表
        public static Dictionary<(uint, uint), float> fastFFLookup = new Dictionary<(uint, uint), float>();
        public static bool isInitialized = false;
        public static Scp5kFFManager Ins;

        public override void Init()
        {
            var ffDataList = new List<FFData>() {
                new FFData(new List<CustomRole>()
                {
                    Scp5k_Control.scp5k_Scp610_mother.instance,
                    Scp5k_Control.scp5k_Scp610.instance,
                },new List<RoleTypeId>()
                {
                    RoleTypeId.ClassD,
                    RoleTypeId.Scientist,
                    RoleTypeId.FacilityGuard,
                    RoleTypeId.ChaosConscript,
                    RoleTypeId.ChaosMarauder,
                    RoleTypeId.ChaosRepressor,
                    RoleTypeId.ChaosRifleman,
                    RoleTypeId.CustomRole,
                    RoleTypeId.Tutorial,
                    RoleTypeId.NtfCaptain,
                RoleTypeId.NtfPrivate,
                RoleTypeId.NtfSergeant,
                RoleTypeId.NtfSpecialist,
                RoleTypeId.Scp049,
                RoleTypeId.Scp079,
                RoleTypeId.Scp096,
                RoleTypeId.Scp3114,
                RoleTypeId.Scp0492,
                RoleTypeId.Scp939,
                RoleTypeId.Scp173,
                RoleTypeId.Scp106
                },1),
                new FFData(new List<RoleTypeId>()
                {
                RoleTypeId.NtfCaptain,
                RoleTypeId.NtfPrivate,
                RoleTypeId.NtfSergeant,
                RoleTypeId.NtfSpecialist,
                RoleTypeId.Scp049,
                RoleTypeId.Scp079,
                RoleTypeId.Scp096,
                RoleTypeId.Scp3114,
                RoleTypeId.Scp0492,
                RoleTypeId.Scp939,
                RoleTypeId.Scp173,
                RoleTypeId.Scp106
                },new List<CustomRole>()
                {
                    scp5k_Scp1440.instance
                },0f),
                new FFData(new List<RoleTypeId>()
                {
                    RoleTypeId.ClassD,
                    RoleTypeId.Scientist,
                    RoleTypeId.FacilityGuard,
                    RoleTypeId.ChaosConscript,
                    RoleTypeId.ChaosMarauder,
                    RoleTypeId.ChaosRepressor,
                    RoleTypeId.ChaosRifleman,
                    RoleTypeId.Tutorial,
                    RoleTypeId.CustomRole

                },new List<CustomRole>()
                {
                    scp5k_Scp1440.instance
                },1f),
                new FFData(new List<RoleTypeId>()
                {
                    RoleTypeId.ClassD,
                    RoleTypeId.Scientist,
                    RoleTypeId.FacilityGuard,
                    RoleTypeId.ChaosConscript,
                    RoleTypeId.ChaosMarauder,
                    RoleTypeId.ChaosRepressor,
                    RoleTypeId.ChaosRifleman,
                    RoleTypeId.Tutorial,
                    RoleTypeId.CustomRole
                },new List<RoleTypeId>()
                {
                    RoleTypeId.ClassD,
                    RoleTypeId.Scientist,
                    RoleTypeId.FacilityGuard,
                    RoleTypeId.ChaosConscript,
                    RoleTypeId.ChaosMarauder,
                    RoleTypeId.ChaosRepressor,
                    RoleTypeId.ChaosRifleman,
                    RoleTypeId.CustomRole,
                    RoleTypeId.Tutorial,
                },0),
                new FFData(new List<RoleTypeId>()
                {
                    RoleTypeId.ClassD,
                    RoleTypeId.Scientist,
                    RoleTypeId.FacilityGuard,
                    RoleTypeId.ChaosConscript,
                    RoleTypeId.ChaosMarauder,
                    RoleTypeId.ChaosRepressor,
                    RoleTypeId.ChaosRifleman,
                    RoleTypeId.Tutorial,
                    RoleTypeId.CustomRole

                },new List<RoleTypeId>()
                {
                RoleTypeId.NtfCaptain,
                RoleTypeId.NtfPrivate,
                RoleTypeId.NtfSergeant,
                RoleTypeId.NtfSpecialist,
                RoleTypeId.Scp049,
                RoleTypeId.Scp079,
                RoleTypeId.Scp096,
                RoleTypeId.Scp3114,
                RoleTypeId.Scp0492,
                RoleTypeId.Scp939,
                RoleTypeId.Scp173,
                RoleTypeId.Scp106
                },1f),
                new FFData(new List<RoleTypeId>()
                {
                RoleTypeId.NtfCaptain,
                RoleTypeId.NtfPrivate,
                RoleTypeId.NtfSergeant,
                RoleTypeId.NtfSpecialist,
                RoleTypeId.Scp049,
                RoleTypeId.Scp079,
                RoleTypeId.Scp096,
                RoleTypeId.Scp3114,
                RoleTypeId.Scp0492,
                RoleTypeId.Scp939,
                RoleTypeId.Scp173,
                RoleTypeId.Scp106
                },new List<RoleTypeId>()
                {
                RoleTypeId.NtfCaptain,
                RoleTypeId.NtfPrivate,
                RoleTypeId.NtfSergeant,
                RoleTypeId.NtfSpecialist,
                RoleTypeId.Scp049,
                RoleTypeId.Scp079,
                RoleTypeId.Scp096,
                RoleTypeId.Scp3114,
                RoleTypeId.Scp0492,
                RoleTypeId.Scp939,
                RoleTypeId.Scp173,
                RoleTypeId.Scp106
                },0f),
            };
            fastFFLookup.Clear();
            // 构建快速查找表
            foreach (var ff in ffDataList)
            {
                if (ff.IsCustomRoleA && !ff.IsCustomRoleB)
                {
                    // Custom vs Type
                    foreach (var customRole in ff.CustomRoleA)
                    {
                        var customRoleId = customRole.Id;
                        foreach (var roleType in ff.TypeB)
                        {
                            var roleTypeId = (uint)roleType;
                            fastFFLookup[(customRoleId, roleTypeId)] = ff.ff;
                            fastFFLookup[(roleTypeId, customRoleId)] = ff.ff; // 双向映射
                        }
                    }
                }
                else if (!ff.IsCustomRoleA && ff.IsCustomRoleB)
                {
                    // Type vs Custom
                    foreach (var roleType in ff.TypeA)
                    {
                        var roleTypeId = (uint)roleType;
                        foreach (var customRole in ff.CustomRoleB)
                        {
                            var customRoleId = customRole.Id;
                            fastFFLookup[(roleTypeId, customRoleId)] = ff.ff;
                            fastFFLookup[(customRoleId, roleTypeId)] = ff.ff; // 双向映射
                        }
                    }
                }
                else if (ff.IsCustomRoleA && ff.IsCustomRoleB)
                {
                    // Custom vs Custom
                    foreach (var customRoleA in ff.CustomRoleA)
                    {
                        var customRoleIdA = customRoleA.Id;
                        foreach (var customRoleB in ff.CustomRoleB)
                        {
                            var customRoleIdB = customRoleB.Id;
                            fastFFLookup[(customRoleIdA, customRoleIdB)] = ff.ff;
                            fastFFLookup[(customRoleIdB, customRoleIdA)] = ff.ff; // 双向映射
                        }
                    }
                }
                else
                {
                    // Type vs Type
                    foreach (var roleTypeA in ff.TypeA)
                    {
                        var roleTypeIdA = (uint)roleTypeA;
                        foreach (var roleTypeB in ff.TypeB)
                        {
                            var roleTypeIdB = (uint)roleTypeB;
                            fastFFLookup[(roleTypeIdA, roleTypeIdB)] = ff.ff;
                            fastFFLookup[(roleTypeIdB, roleTypeIdA)] = ff.ff; // 双向映射
                        }
                    }
                }
            }
            Log.Info($"FF 快速查找表已初始化，包含 {fastFFLookup.Count} 条记录。");
            Ins = this;
            isInitialized = true;
        }

        public bool IsDamaging(Player a, Player b)
        {
            var roleA = a.Role.Type;
            var roleB = b.Role.Type;

            var customA = GetCustomRole(a);
            var customB = GetCustomRole(b);

            // 生成查找键
            uint keyA = customA != null ? customA.Id : (uint)roleA;
            uint keyB = customB != null ? customB.Id : (uint)roleB;

            // 确保较小的ID在前，保持一致性
            var lookupKey = keyA <= keyB ? (keyA, keyB) : (keyB, keyA);

            if (fastFFLookup.TryGetValue(lookupKey, out float ffValue))
            {
                return ffValue > 0f;
            }

            // 默认无友伤
            return false;
        }

        public float GetFF(Player a, Player b)
        {
            var roleA = a.Role.Type;
            var roleB = b.Role.Type;

            var customA = GetCustomRole(a);
            var customB = GetCustomRole(b);

            // 生成查找键
            uint keyA = customA != null ? customA.Id : (uint)roleA;
            uint keyB = customB != null ? customB.Id : (uint)roleB;

            // 确保较小的ID在前，保持一致性
            var lookupKey = keyA <= keyB ? (keyA, keyB) : (keyB, keyA);

            if (fastFFLookup.TryGetValue(lookupKey, out float ffValue))
            {
                return ffValue;
            }

            return -1f; // 默认无友伤
        }

        private static CustomRole GetCustomRole(Player p)
        {
            if (string.IsNullOrEmpty(p.UniqueRole))
                return null;

            return CustomRole.Get(p.UniqueRole);
        }

        public override void Delete()
        {
            //throw new NotImplementedException();
        }
    }

    public class FFData
    {
        public bool IsCustomRoleA { get; private set; }
        public bool IsCustomRoleB { get; private set; }
        public List<RoleTypeId> TypeA { get; private set; } = new();
        public List<RoleTypeId> TypeB { get; private set; } = new();
        public List<CustomRole> CustomRoleA { get; private set; } = new();
        public List<CustomRole> CustomRoleB { get; private set; } = new();
        public float ff;

        // === 构造函数 ===
        public FFData(List<RoleTypeId> A, List<RoleTypeId> B, float FF)
        {
            TypeA = A; TypeB = B; ff = FF;
        }
        public FFData(List<CustomRole> A, List<RoleTypeId> B, float FF)
        {
            CustomRoleA = A; TypeB = B; ff = FF; IsCustomRoleA = true;
        }
        public FFData(List<RoleTypeId> A, List<CustomRole> B, float FF)
        {
            TypeA = A; CustomRoleB = B; ff = FF; IsCustomRoleB = true;
        }
        public FFData(List<CustomRole> A, List<CustomRole> B, float FF)
        {
            CustomRoleA = A; CustomRoleB = B; ff = FF; IsCustomRoleA = true; IsCustomRoleB = true;
        }

        // === 匹配判定 ===
        public bool Match(RoleTypeId A, RoleTypeId B)
            => (TypeA.Contains(A) && TypeB.Contains(B)) || (TypeA.Contains(B) && TypeB.Contains(A));

        public bool Match(CustomRole A, RoleTypeId B)
            => (CustomRoleA.Any(x => x.Id == A.Id) && TypeB.Contains(B))
            || (TypeA.Contains(B) && CustomRoleB.Any(x => x.Id == A.Id));

        public bool Match(RoleTypeId A, CustomRole B)
            => (TypeA.Contains(A) && CustomRoleB.Any(x => x.Id == B.Id))
            || (TypeB.Contains(A) && CustomRoleA.Any(x => x.Id == B.Id));

        public bool Match(CustomRole A, CustomRole B)
            => (CustomRoleA.Any(x => x.Id == A.Id) && CustomRoleB.Any(x => x.Id == B.Id))
            || (CustomRoleA.Any(x => x.Id == B.Id) && CustomRoleB.Any(x => x.Id == A.Id));
    }
}



