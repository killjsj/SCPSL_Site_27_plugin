using Exiled.API.Features;
using MapGeneration;
using Mirror;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.Scp049.Zombies;
using RelativePositioning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Next_generationSite_27.UnionP.RoomGraph;
using Object = UnityEngine.Object;

namespace Next_generationSite_27.UnionP
{
    public class PlayerFollower : MonoBehaviour
    {
        private const float DefaultMaxDistance = 20f;

        private const float DefaultMinDistance = 1.75f;

        private const float DefaultSpeed = 30f;

        private ReferenceHub _hub;

        private ReferenceHub _hubToFollow;

        private float _maxDistance;

        private float _minDistance;

        private float _speed;

        public void Init(ReferenceHub playerToFollow, float maxDistance = 20f, float minDistance = 1.75f, float speed = 30f)
        {
            _hub = GetComponent<ReferenceHub>();
            _hubToFollow = playerToFollow;
            _maxDistance = maxDistance;
            _minDistance = minDistance;
            _speed = speed;
        }

        private void Update()
        {
            IFpcRole fpcRole;
            IFpcRole fpcRoleToFollow;

            if (!NetworkServer.active ||
                _hubToFollow == null ||
                _hub == null ||
                !(_hubToFollow.roleManager.CurrentRole is IFpcRole) ||
                !(_hub.roleManager.CurrentRole is IFpcRole))
            {
                Object.Destroy(this);
                return;
            }

            fpcRole = (IFpcRole)_hub.roleManager.CurrentRole;
            fpcRoleToFollow = (IFpcRole)_hubToFollow.roleManager.CurrentRole;
            var speed = _speed;
            float num = Vector3.Distance(_hubToFollow.transform.position, base.transform.position);
            if (num > _maxDistance)
            {
                fpcRole.FpcModule.ServerOverridePosition(_hubToFollow.transform.position);
            }
            else if (!(num < _minDistance))
            {
                if(_hub.roleManager.CurrentRole is ZombieRole ZR)
                {
                    var SubroutineModule = ZR.SubroutineModule;
                    if (!SubroutineModule.TryGetSubroutine<ZombieConsumeAbility>(out var subroutine3))
                    {
                        Log.Error("ZombieConsumeAbility subroutine not found in Scp0492Role::ctor");
                    }

                    ZombieConsumeAbility ConsumeAbility = subroutine3;
                    if (ConsumeAbility.IsInProgress)
                    {
                        return;
                    }
                    speed = fpcRole.FpcModule.MaxMovementSpeed;
                }

                var nav = SimpleRoomNavigation.Nav;
                List<Vector3> re = new List<Vector3>();
                if(Room.FindParentRoom(_hub.gameObject).Identifier == Room.FindParentRoom(_hubToFollow.gameObject).Identifier)
                {
                    re = SimpleRoomNavigation.LocalPathInRoom(_hub.GetPosition(), _hubToFollow.GetPosition(), Room.FindParentRoom(_hub.gameObject));
                    //Log.Info(re.Count);
                } else
                {
                    re = nav.FindPath(_hub.transform.position, Room.FindParentRoom(_hub.gameObject), _hubToFollow.transform.position, Room.FindParentRoom(_hubToFollow.gameObject));
                }
                    Vector3 target = _hubToFollow.transform.position;
                try
                {
                    if (re != null)
                    {
                        if (re.Count > 2)
                        {
                            if (SimpleRoomNavigation.IsDirectPathClear(re[0], re[2]))
                            {
                                _hub.nicknameSync.Network_customPlayerInfoString = "Tracing re2";
                                target = re[2];
                            }
                            else
                            {
                                _hub.nicknameSync.Network_customPlayerInfoString = "Tracing re1";
                                target = re[1];

                            }
                        }
                        else
                        {
                            _hub.nicknameSync.Network_customPlayerInfoString = "Tracing _hubToFollow.transform.position";
                            target = _hubToFollow.transform.position;

                        }
                    }
                }
                catch (Exception ex) { 
                    Log.Warn(ex.StackTrace);
                }
                Vector3 position = base.transform.position;
                Vector3 dir = target - position;
                Vector3 vector = Time.deltaTime * speed * dir.normalized;
                fpcRole.FpcModule.Motor.ReceivedPosition = new RelativePosition(position + vector);
                fpcRole.FpcModule.MouseLook.LookAtDirection(dir);
            }
        }
    }
}
