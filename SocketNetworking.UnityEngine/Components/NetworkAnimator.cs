using System.Collections.Generic;
using SocketNetworking.Client;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Attributes;
using SocketNetworking.UnityEngine.Packets.NetworkAnimator;
using UnityEngine;

namespace SocketNetworking.UnityEngine.Components
{
    public class NetworkAnimator : NetworkComponent
    {
        private Animator _animator;

        public Animator Animator
        {
            get { return _animator; }
        }

        private Dictionary<int, string> HashToName = new Dictionary<int, string>();

        public bool ShouldIgnoreDuplicates = true;

        public string GetNameFromHash(int hash)
        {
            if (HashToName.ContainsKey(hash)) return HashToName[hash];
            return "NO SUCH HASH";
        }

        void Awake()
        {
            UnityNetworkManager.Register(this);
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Logger.Error("NetworkAnimator is attached to an object with no Animator! Object Name: " + gameObject.name);
                return;
            }
            foreach (AnimatorControllerParameter param in _animator.parameters)
            {
                if (HashToName.ContainsKey(param.nameHash))
                {
                    return;
                }
                else
                {
                    HashToName.Add(param.nameHash, param.name);
                }
            }
        }

        void OnDestroy()
        {
            UnityNetworkManager.Unregister(this);
        }

        [PacketListener(typeof(NetworkAnimatorPlayAnimPacket), NetworkDirection.Any)]
        public void OnAnimtorUpdatePlaybackPacket(NetworkAnimatorPlayAnimPacket packet, NetworkClient client)
        {
            if (!ShouldBeReceivingPacketsFrom(client))
            {
                Logger.Warning("Incorrect packet direction!");
                return;
            }
            if (!packet.DoNotPlayAnything)
            {
                if (packet.IsStateHash)
                {
                    _animator.Play(int.Parse(packet.StateName), packet.Layer, packet.NormalizedTime);
                }
                else
                {
                    _animator.Play(packet.StateName, packet.Layer, packet.NormalizedTime);
                }
            }
        }

        [PacketListener(typeof(NetworkAnimatorSpeedUpdatePacket), NetworkDirection.Any)]
        public void OnAnimatorSpeedPacket(NetworkAnimatorSpeedUpdatePacket packet, NetworkClient client)
        {
            if (!ShouldBeReceivingPacketsFrom(client))
            {
                return;
            }
            Logger.Debug($"Changing animator speed: " + packet.AnimatorSpeed);
            _animator.speed = packet.AnimatorSpeed;
        }

        public float NetworkAnimatorSpeed
        {
            get
            {
                return _animator.speed;
            }
            set
            {
                if (IsOwner)
                {
                    //optimization to avoid sending extra data.
                    if (_animator.speed == value)
                    {
                        return;
                    }
                    NetworkAnimatorSpeedUpdatePacket packet = new NetworkAnimatorSpeedUpdatePacket(value);
                    Send(packet, this);
                }
            }
        }

        public override void OnNetworkSpawned(NetworkClient spawner)
        {
            base.OnNetworkSpawned(spawner);
            NetworkAnimatorSpeed = NetworkAnimatorSpeed;
            for (int i = -1; i < _animator.layerCount; i++)
            {
                AnimatorStateInfo info = _animator.GetCurrentAnimatorStateInfo(i);
                NetworkPlay(info.fullPathHash, i, info.normalizedTime);
            }
        }

        public void NetworkPlay(string name, int layer = -1, float normalizedTime = float.NegativeInfinity)
        {
            NetworkInvoke(nameof(GetPlayData), new object[] { Animator.StringToHash(name), layer, normalizedTime });
        }

        public void NetworkPlay(int hash, int layer = -1, float normalizedTime = float.NegativeInfinity)
        {
            NetworkInvoke(nameof(GetPlayData), new object[] { hash, layer, normalizedTime });
        }

        [NetworkInvokable]
        private void GetPlayData(NetworkClient client, int hash, int layer, float normalizedTime)
        {
            if (!ShouldBeReceivingPacketsFrom(client))
            {
                return;
            }
            if (NetworkManager.WhereAmI == ClientLocation.Remote)
            {
                NetworkPlay(hash, layer, normalizedTime);
            }
            _animator.Play(hash, layer, normalizedTime);
        }

        [NetworkInvokable]
        private void GetPlayData(int hash, int layer, float normalizedTime)
        {
            _animator.Play(hash, layer, normalizedTime);
        }

        [PacketListener(typeof(NetworkAnimatorTriggerPacket), NetworkDirection.Any)]
        public virtual void OnAnimatorTriggerPacket(NetworkClient client, NetworkAnimatorTriggerPacket packet)
        {
            if (!ShouldBeReceivingPacketsFrom(client))
            {
                return;
            }
            Logger.Debug($"Trigger Update: {GetNameFromHash(packet.Hash)}, Setting trigger?: {packet.SetTrigger}, From: {client.ClientID}");
            if (packet.SetTrigger)
            {
                SetTrigger(packet.Hash);
            }
            else
            {
                ResetTrigger(packet.Hash);
            }
        }

        [PacketListener(typeof(NetworkAnimatorFloatValueUpdatePacket), NetworkDirection.Any)]
        public virtual void OnAnimatorFloatUpdatePacket(NetworkClient client, NetworkAnimatorFloatValueUpdatePacket packet)
        {
            if (!ShouldBeReceivingPacketsFrom(client))
            {
                Logger.Warning("Incorrect packet direction!");
                return;
            }
            Logger.Debug($"State Update: {GetNameFromHash(packet.ValueHash)}, Value: {packet.Value}");
            if (packet.ReadFloatSpecificValues)
            {
                SetFloat(packet.ValueHash, packet.Value, packet.DampTime, packet.DeltaTime);
            }
            else
            {
                SetFloat(packet.ValueHash, packet.Value);
            }
        }

        [PacketListener(typeof(NetworkAnimatorBoolValueUpdatePacket), NetworkDirection.Any)]
        public void OnAnimatorBoolUpdatePacket(NetworkClient client, NetworkAnimatorBoolValueUpdatePacket packet)
        {
            if (!ShouldBeReceivingPacketsFrom(client))
            {
                Logger.Warning("Incorrect packet direction!");
                return;
            }
            Logger.Debug($"State Update: {GetNameFromHash(packet.ValueHash)}, Value: {packet.Value}");
            SetBool(packet.ValueHash, packet.Value);
        }

        [PacketListener(typeof(NetworkAnimatorIntValueUpdatePacket), NetworkDirection.Any)]
        public void OnAnimatorBoolUpdatePacket(NetworkClient client, NetworkAnimatorIntValueUpdatePacket packet)
        {
            if (!ShouldBeReceivingPacketsFrom(client))
            {
                Logger.Warning("Incorrect packet direction!");
                return;
            }
            Logger.Debug($"State Update: {GetNameFromHash(packet.ValueHash)}, Value: {packet.Value}");
            SetInteger(packet.ValueHash, packet.Value);
        }

        private Dictionary<int, float> _animStateFloat = new Dictionary<int, float>();

        private Dictionary<int, int> _animStateInt = new Dictionary<int, int>();

        private Dictionary<int, bool> _animStateBool = new Dictionary<int, bool>();

        public void ResetCache()
        {
            _animStateFloat.Clear();
            _animStateInt.Clear();
            _animStateBool.Clear();
        }

        public void OnDisable()
        {
            ResetCache();
        }

        void SetFloat(string name, float value)
        {
            _animator.SetFloat(name, value);
        }

        public void NetworkSetFloat(string name, float value)
        {
            if (!IsOwner)
            {
                return;
            }
            int hash = Animator.StringToHash(name);
            if (_animStateFloat.ContainsKey(hash) && _animStateFloat[hash] == value && ShouldIgnoreDuplicates)
            {
                Logger.Debug($"State: {GetNameFromHash(hash)} has been ignored because it is a duplicate. Value: {value}");
                return;
            }
            else
            {
                if (_animStateFloat.ContainsKey(hash))
                {
                    _animStateFloat[hash] = value;
                }
                else
                {
                    _animStateFloat.Add(hash, value);
                }
            }
            NetworkAnimatorFloatValueUpdatePacket packet = new NetworkAnimatorFloatValueUpdatePacket(name, value);
            packet.NetworkIDTarget = NetworkID;
            Send(packet);
        }

        void SetFloat(string name, float value, float dampTime, float deltaTime)
        {
            _animator.SetFloat(name, value, dampTime, deltaTime);
        }

        public void NetworkSetFloat(string name, float value, float dampTime, float deltaTime)
        {
            if (!IsOwner)
            {
                return;
            }
            int hash = Animator.StringToHash(name);
            if (_animStateFloat.ContainsKey(hash) && _animStateFloat[hash] == value && ShouldIgnoreDuplicates)
            {
                Logger.Debug($"State: {GetNameFromHash(hash)} has been ignored because it is a duplicate. Value: {value}");
                return;
            }
            else
            {
                if (_animStateFloat.ContainsKey(hash))
                {
                    _animStateFloat[hash] = value;
                }
                else
                {
                    _animStateFloat.Add(hash, value);
                }
            }
            NetworkAnimatorFloatValueUpdatePacket packet = new NetworkAnimatorFloatValueUpdatePacket(name, value, dampTime, deltaTime);
            packet.NetworkIDTarget = NetworkID;
            Send(packet);
        }

        void SetFloat(int id, float value)
        {
            _animator.SetFloat(id, value);
        }

        public void NetworkSetFloat(int id, float value)
        {
            if (!IsOwner)
            {
                return;
            }
            int hash = id;
            if (_animStateFloat.ContainsKey(hash) && _animStateFloat[hash] == value && ShouldIgnoreDuplicates)
            {
                Logger.Debug($"State: {GetNameFromHash(hash)} has been ignored because it is a duplicate. Value: {value}");
                return;
            }
            else
            {
                if (_animStateFloat.ContainsKey(hash))
                {
                    _animStateFloat[hash] = value;
                }
                else
                {
                    _animStateFloat.Add(hash, value);
                }
            }
            NetworkAnimatorFloatValueUpdatePacket packet = new NetworkAnimatorFloatValueUpdatePacket(id, value);
            packet.NetworkIDTarget = NetworkID;
            Send(packet);
        }

        void SetFloat(int id, float value, float dampTime, float deltaTime)
        {
            _animator.SetFloat(id, value, dampTime, deltaTime);
        }

        public void NetworkSetFloat(int id, float value, float dampTime, float deltaTime)
        {
            if (!IsOwner)
            {
                return;
            }
            int hash = id;
            if (_animStateFloat.ContainsKey(hash) && _animStateFloat[hash] == value && ShouldIgnoreDuplicates)
            {
                Logger.Debug($"State: {GetNameFromHash(hash)} has been ignored because it is a duplicate. Value: {value}");
                return;
            }
            else
            {
                if (_animStateFloat.ContainsKey(hash))
                {
                    _animStateFloat[hash] = value;
                }
                else
                {
                    _animStateFloat.Add(hash, value);
                }
            }
            NetworkAnimatorFloatValueUpdatePacket packet = new NetworkAnimatorFloatValueUpdatePacket(id, value, dampTime, deltaTime);
            packet.NetworkIDTarget = NetworkID;
            Send(packet);
        }

        void SetBool(string name, bool value)
        {
            _animator.SetBool(name, value);
        }

        public void NetworkSetBool(string name, bool value)
        {
            if (!IsOwner)
            {
                return;
            }
            int hash = Animator.StringToHash(name);
            if (_animStateBool.ContainsKey(hash) && _animStateBool[hash] == value && ShouldIgnoreDuplicates)
            {
                Logger.Debug($"State: {GetNameFromHash(hash)} has been ignored because it is a duplicate. Value: {value}");
                return;
            }
            else
            {
                if (_animStateBool.ContainsKey(hash))
                {
                    _animStateBool[hash] = value;
                }
                else
                {
                    _animStateBool.Add(hash, value);
                }
            }
            NetworkAnimatorBoolValueUpdatePacket packet = new NetworkAnimatorBoolValueUpdatePacket(name, value)
            {
                NetworkIDTarget = NetworkID
            };
            Send(packet);
        }

        void SetBool(int id, bool value)
        {
            _animator.SetBool(id, value);
        }

        public void NetworkSetBool(int id, bool value)
        {
            if (!IsOwner)
            {
                return;
            }
            int hash = id;
            if (_animStateBool.ContainsKey(hash) && _animStateBool[hash] == value && ShouldIgnoreDuplicates)
            {
                Logger.Debug($"State: {GetNameFromHash(hash)} has been ignored because it is a duplicate. Value: {value}");
                return;
            }
            else
            {
                if (_animStateBool.ContainsKey(hash))
                {
                    _animStateBool[hash] = value;
                }
                else
                {
                    _animStateBool.Add(hash, value);
                }
            }
            NetworkAnimatorBoolValueUpdatePacket packet = new NetworkAnimatorBoolValueUpdatePacket(id, value)
            {
                NetworkIDTarget = NetworkID
            };
            Send(packet);
        }

        void SetInteger(string name, int value)
        {
            _animator.SetInteger(name, value);
        }

        public void NetworkSetInteger(string name, int value)
        {
            if (!IsOwner)
            {
                return;
            }
            int hash = Animator.StringToHash(name);
            if (_animStateInt.ContainsKey(hash) && _animStateInt[hash] == value && ShouldIgnoreDuplicates)
            {
                Logger.Debug($"State: {GetNameFromHash(hash)} has been ignored because it is a duplicate. Value: {value}");
                return;
            }
            else
            {
                if (_animStateInt.ContainsKey(hash))
                {
                    _animStateInt[hash] = value;
                }
                else
                {
                    _animStateInt.Add(hash, value);
                }
            }
            NetworkAnimatorIntValueUpdatePacket packet = new NetworkAnimatorIntValueUpdatePacket(name, value);
            packet.NetworkIDTarget = NetworkID;
            Send(packet);
        }

        void SetInteger(int id, int value)
        {
            _animator.SetInteger(id, value);
        }

        public void NetworkSetInteger(int id, int value)
        {
            if (!IsOwner)
            {
                return;
            }
            int hash = id;
            if (_animStateInt.ContainsKey(hash) && _animStateInt[hash] == value && ShouldIgnoreDuplicates)
            {
                Logger.Debug($"State: {GetNameFromHash(hash)} has been ignored because it is a duplicate. Value: {value}");
                return;
            }
            else
            {
                if (_animStateInt.ContainsKey(hash))
                {
                    _animStateInt[hash] = value;
                }
                else
                {
                    _animStateInt.Add(hash, value);
                }
            }
            NetworkAnimatorIntValueUpdatePacket packet = new NetworkAnimatorIntValueUpdatePacket(id, value);
            packet.NetworkIDTarget = NetworkID;
            Send(packet);
        }

        void SetTrigger(string name)
        {
            _animator.SetTrigger(name);
        }

        public void NetworkSetTrigger(string name)
        {
            if (!IsOwner)
            {
                return;
            }
            NetworkAnimatorTriggerPacket packet = new NetworkAnimatorTriggerPacket();
            packet.NetworkIDTarget = NetworkID;
            packet.WriteName(name);
            packet.SetTrigger = true;
            Send(packet);
        }

        void SetTrigger(int id)
        {
            _animator.SetTrigger(id);
        }

        public void NetworkSetTrigger(int id)
        {
            if (!IsOwner)
            {
                return;
            }
            NetworkAnimatorTriggerPacket packet = new NetworkAnimatorTriggerPacket();
            packet.NetworkIDTarget = NetworkID;
            packet.Hash = id;
            packet.SetTrigger = true;
            Send(packet);
        }

        void ResetTrigger(string name)
        {
            _animator.SetTrigger(name);
        }

        public void NetworkResetTrigger(string name)
        {
            if (!IsOwner)
            {
                return;
            }
            NetworkAnimatorTriggerPacket packet = new NetworkAnimatorTriggerPacket();
            packet.NetworkIDTarget = NetworkID;
            packet.WriteName(name);
            packet.SetTrigger = false;
            Send(packet);
        }

        void ResetTrigger(int id)
        {
            _animator.ResetTrigger(id);
        }

        public void NetworkResetTrigger(int id)
        {
            if (!IsOwner)
            {
                return;
            }
            NetworkAnimatorTriggerPacket packet = new NetworkAnimatorTriggerPacket();
            packet.NetworkIDTarget = NetworkID;
            packet.Hash = id;
            packet.SetTrigger = false;
            Send(packet);
        }
    }
}
