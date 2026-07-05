using AdminToys;
using Exiled.API.Features;
using Exiled.API.Features.Core.UserSettings;
using Exiled.Events.EventArgs.Player;
using Mirror;
using MEC;
using Next_generationSite_27.UnionP.UI;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UserSettings.ServerSpecific;
using VoiceChat.Networking;
using Log = Exiled.API.Features.Log;
using Player = Exiled.API.Features.Player;

namespace Next_generationSite_27.UnionP
{
    class ScpVoiceManager : BaseClass
    {
        public override void Init()
        {
            Exiled.Events.Handlers.Player.VoiceChatting += VoiceChatting;
            Plugin.MenuCache.AddRange(Menu());
        }

        public override void Delete()
        {
            Exiled.Events.Handlers.Player.VoiceChatting -= VoiceChatting;
            Plugin.MenuCache.RemoveAll(x => x.Id == Plugin.Instance.Config.SettingIds[Features.ScpTalk]);
        }

        public static List<Player> TalkTohumanScp = new List<Player>();
        public static Dictionary<Player, SpeakerToy> ScpToSpeaker = new Dictionary<Player, SpeakerToy>();
        private static SpeakerToy _speakerPrefab;

        private static SpeakerToy GetSpeakerPrefab()
        {
            if (_speakerPrefab != null) return _speakerPrefab;
            foreach (var prefab in NetworkClient.prefabs.Values)
            {
                if (prefab.TryGetComponent(out SpeakerToy toy))
                {
                    _speakerPrefab = toy;
                    break;
                }
            }
            return _speakerPrefab;
        }

        public static void VoiceChatting(VoiceChattingEventArgs ev)
        {
            if (ev.Player.IsScp && TalkTohumanScp.Contains(ev.Player))
            {
                var id = (byte)(120 + ev.Player.Id);
                if (!ScpToSpeaker.TryGetValue(ev.Player, out var sp))
                {
                    var prefab = GetSpeakerPrefab();
                    if (prefab == null) return;

                    var newInstance = GameObject.Instantiate(prefab, ev.Player.Position, Quaternion.identity);
                    newInstance.NetworkControllerId = id;
                    newInstance.NetworkVolume = 1f;
                    newInstance.IsSpatial = false;
                    newInstance.MinDistance = 0f;
                    newInstance.MaxDistance = 20f;
                    newInstance.transform.parent = ev.Player.Transform;

                    NetworkServer.Spawn(newInstance.gameObject);

                    ScpToSpeaker.Add(ev.Player, newInstance);
                    sp = newInstance;
                }

                sp.transform.position = ev.Player.Position;
                sp.MaxDistance = 20f;
                sp.MinDistance = 0f;

                var vm = new AudioMessage()
                {
                    ControllerId = id,
                    Data = ev.VoiceMessage.Data,
                    DataLength = ev.VoiceMessage.DataLength,
                };

                foreach (var hub in ReferenceHub.AllHubs.Where(x =>
                    x.roleManager.CurrentRole is FpcStandardRoleBase i &&
                    Vector3.Distance(i.CameraPosition, ev.Player.Position) <= 20 && x != ev.Player.ReferenceHub && x.roleManager.CurrentRole.Team != Team.SCPs))
                {
                    hub.connectionToClient.Send(vm, 0);
                }
            }
        }

        public static List<SettingBase> Menu()
        {
            List<SettingBase> settings = new List<SettingBase>();
            settings.Add(new KeybindSetting(
                Plugin.Instance.Config.SettingIds[Features.ScpTalk], "SCP切换语音频道", KeyCode.V, false, false, "与人类沟通",
                onChanged: (player, SB) =>
                {
                    try
                    {
                        if (SB is KeybindSetting ks)
                        {
                            if (ks.IsPressed)
                            {
                                if (player != null)
                                {
                                    if (player.IsScp)
                                    {
                                        if (!TalkTohumanScp.Contains(player))
                                        {
                                            TalkTohumanScp.Add(player);
                                            player.AddMessage($"EnableScpToHumanTalk-{DateTime.Now}", "<voffset=-1em><size=29><b>已启用scp对人类讲话</b></size></voffset>", duration: 3f);
                                        }
                                        else
                                        {
                                            TalkTohumanScp.Remove(player);
                                            player.AddMessage($"DisableScpToHumanTalk-{DateTime.Now}", "<voffset=-1em><size=29><b>已禁用scp对人类讲话</b></size></voffset>", duration: 3f);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex) { Log.Error(ex.ToString()); }
                }));

            return settings;
        }

        public static void CleanupPlayer(Player player)
        {
            ScpToSpeaker.Remove(player);
            TalkTohumanScp.Remove(player);
        }
    }
}
