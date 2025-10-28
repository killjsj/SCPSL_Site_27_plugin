using Exiled.API.Features;
using Exiled.CustomRoles;
using Exiled.CustomRoles.API.Features;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Next_generationSite_27.UnionP.Scp5k
{
    class FFManager
    {
        public static List<FFData> TypeFFList = new List<FFData>() {
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
        public static List<FFData> CustomRoleList = new List<FFData>() {
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
        };

        public static float GetFF(RoleTypeId A, RoleTypeId B)
        {
            foreach (FFData ffdata in TypeFFList)
            {
                if (!ffdata.IsCustomRoleA && !ffdata.IsCustomRoleB)
                {
                    if (ffdata.TypeA.Contains(A) && ffdata.TypeB.Contains(B))
                    {
                        return ffdata.ff;
                    }
                }
            }
            return -1f;
        }
        public static float GetFF(CustomRole A, RoleTypeId B)
        {
            foreach (FFData ffdata in TypeFFList)
            {
                if (ffdata.IsCustomRoleA && !ffdata.IsCustomRoleB)
                {
                    if (ffdata.CustomRoleA.Any(x => x.Id == A.Id) && ffdata.TypeB.Contains(B))
                    {
                        return ffdata.ff;
                    }
                }
            }
            return -1f;
        }
        public static float GetFF(CustomRole A, CustomRole B)
        {
            foreach (FFData ffdata in TypeFFList)
            {
                if (ffdata.IsCustomRoleA && ffdata.IsCustomRoleB)
                {
                    if (ffdata.CustomRoleA.Any(x => x.Id == A.Id) && ffdata.CustomRoleB.Any(x => x.Id == B.Id))
                    {
                        return ffdata.ff;
                    }
                }
            }
            return -1f;
        }
        public static float GetFF(Player A, Player B)
        {
            if (A.UniqueRole == "" && B.UniqueRole == "")
            {
                return GetFF(A.Role.Type, B.Role.Type);
            }
            else if (A.UniqueRole != "" && B.UniqueRole == "")
            {
                CustomRole customRoleA = CustomRole.Get(A.UniqueRole);
                var FF = GetFF(customRoleA, A.Role.Type);
                if (FF == -1f)
                {
                    FF = GetFF(A.Role.Type, B.Role.Type);
                }
                return FF;
            }
            else if (A.UniqueRole == "" && B.UniqueRole != "")
            {
                CustomRole customRoleB = CustomRole.Get(B.UniqueRole);
                //return 
                var FF = GetFF(customRoleB, A.Role.Type);
                if(FF == -1f)
                {
                    FF = GetFF(A.Role.Type, B.Role.Type);
                }
                return FF;
            }
            else if (A.UniqueRole != "" && B.UniqueRole != "")
            {
                CustomRole customRoleA = CustomRole.Get(A.UniqueRole);
                CustomRole customRoleB = CustomRole.Get(B.UniqueRole);
                var FF = GetFF(customRoleA, customRoleB);
                if(FF == -1f)
                {
                    FF = GetFF(A.Role.Type, B.Role.Type);
                }
                return FF;
            }
            return 0f;
        }
    }
    struct FFData
    {
        public bool IsCustomRoleA = false;
        public bool IsCustomRoleB = false;
        public List<CustomRole> CustomRoleA = new List<CustomRole>();
        public List<CustomRole> CustomRoleB = new List<CustomRole>();
        public List<RoleTypeId> TypeA = new List<RoleTypeId>();
        public List<RoleTypeId> TypeB = new List<RoleTypeId>();
        public float ff = 0f;
        public FFData(CustomRole A, CustomRole B, float FF)
        {
            ff = FF;
            IsCustomRoleB = true;
            IsCustomRoleA = true;
            CustomRoleA.Add(A);
            CustomRoleB.Add(B);
        }
        public FFData(RoleTypeId A, CustomRole B, float FF)
        {
            ff = FF;
            IsCustomRoleB = true;
            IsCustomRoleA = false;
            TypeA.Add(A);
            CustomRoleB.Add(B);
        }
        public FFData(CustomRole A, RoleTypeId B, float FF)
        {
            ff = FF;
            IsCustomRoleB = true;
            IsCustomRoleA = false;
            TypeA.Add(B);
            CustomRoleB.Add(A);
        }
        public FFData(RoleTypeId A, RoleTypeId B, float FF)
        {
            ff = FF;
            IsCustomRoleA = false;
            IsCustomRoleB = false;
            TypeA.Add(A);
            TypeB.Add(B);
        }
        public FFData(List<CustomRole> A, List<CustomRole> B, float FF)
        {
            ff = FF;
            IsCustomRoleB = true;
            IsCustomRoleA = true;
            CustomRoleA = A;
            CustomRoleB = B;
        }
        public FFData(List<RoleTypeId> A, List<CustomRole> B, float FF)
        {
            ff = FF;
            IsCustomRoleB = true;
            IsCustomRoleA = false;
            TypeA = A;
            CustomRoleB = B;
        }
        public FFData(List<CustomRole> A, List<RoleTypeId> B, float FF)
        {
            ff = FF;
            IsCustomRoleB = true;
            IsCustomRoleA = false;
            TypeA = B;
            CustomRoleB = A;
        }
        public FFData(List<RoleTypeId> A, List<RoleTypeId> B, float FF)
        {
            ff = FF;
            IsCustomRoleA = false;
            IsCustomRoleB = false;
            TypeA = A;
            TypeB = B;
        }
    }
}
