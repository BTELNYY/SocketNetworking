using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Meziantou.Framework.Win32;
using SocketNetworking.Client;
using SocketNetworking.Misc;
using SocketNetworking.Shared;
using SocketNetworking.Shared.Authentication;
using SocketNetworking.Shared.PacketSystem;
using SocketNetworking.Shared.PacketSystem.Packets;
using SocketNetworking.Shared.Serialization;

namespace SocketNetworking.Extras
{
    /// <summary>
    /// A basic <see cref="AuthenticationProvider"/> which allows a server to require a client to authenticate.
    /// </summary>
    public class CredentialsRequestAuthenticationProvider : AuthenticationProvider
    {
        public event Action<AuthenticationReceivedEvent> Responded;

        public Action<CredentialsRequestAuthenticationProvider> DisconnectClientAction;

        public override bool ClientInitiate => false;

        public bool RequireUsername { get; set; } = true;

        public bool RequirePassword { get; set; } = true;

        public string DefaultUsername { get; set; } = "";

        public string Message { get; set; } = "";

        public string Caption { get; set; } = "Enter your credentials to access {hostname}";

        public CredentialsRequestAuthenticationProvider(NetworkClient client, bool requireUsername, bool requirePassword) : base(client)
        {
            RequireUsername = requireUsername;
            RequirePassword = requirePassword;
        }

        public override (AuthenticationResult, byte[]) Authenticate(NetworkHandle handle, AuthenticationPacket packet)
        {
            ByteReader reader = new ByteReader(packet.ExtraAuthenticationData);
            AuthenticationRequest request = reader.ReadPacketSerialized<AuthenticationRequest>();
            string username = null;
            string password = null;
            string message = Message;
            string caption = Caption.Replace("{hostname}", handle.Client.ConnectedHostname + ":" + handle.Client.ConnectedPort);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                CredentialResult creds = CredentialManager.PromptForCredentials(messageText: message, captionText: caption, userName: DefaultUsername);
                username = creds.UserName;
                password = creds.Password;
            }
            else
            {
                UniversalCredentialsWindow credentials = new UniversalCredentialsWindow();
                credentials.lblCaption.Text = caption;
                credentials.lblMessage.Text = message;
                DialogResult res = credentials.ShowDialog();
                username = credentials.txtUsername.Text;
                password = credentials.txtPassword.Text;
            }
            AuthenticationResponseStruct response = new AuthenticationResponseStruct()
            {
                Password = username ?? "",
                Username = password ?? "",
            };
            ByteWriter writer = new ByteWriter();
            writer.WritePacketSerialized<AuthenticationResponseStruct>(response);
            return (new AuthenticationResult()
            {
                Approved = true,
                Message = ""
            }, writer.Data);
        }

        public override AuthenticationPacket BeginAuthentication()
        {
            ByteWriter writer = new ByteWriter();
            AuthenticationRequest req = new AuthenticationRequest()
            {
                NeedsPassword = RequirePassword,
                NeedsUsername = RequireUsername,
            };
            writer.WritePacketSerialized<AuthenticationRequest>(req);
            AuthenticationPacket packet = new AuthenticationPacket()
            {
                IsResult = false,
                Result = new AuthenticationResult() { Approved = false, Message = "" },
                ExtraAuthenticationData = writer.Data
            };
            return packet;
        }

        public override void HandleAuthenticationResult(NetworkHandle handle, AuthenticationPacket packet)
        {
            ByteReader reader = new ByteReader(packet.ExtraAuthenticationData);
            AuthenticationResponseStruct resp = reader.ReadPacketSerialized<AuthenticationResponseStruct>();
            AuthenticationReceivedEvent evt = new AuthenticationReceivedEvent(resp);
            Responded?.Invoke(evt);
            if (!evt.Accepted)
            {
                if (DisconnectClientAction == null)
                {
                    handle.Client.Disconnect();
                    return;
                }
                DisconnectClientAction?.Invoke(this);
            }
        }
    }


    public class AuthenticationReceivedEvent : ChoiceEvent
    {
        public AuthenticationReceivedEvent(AuthenticationResponseStruct response) : base(false)
        {
            Response = response;
        }

        public AuthenticationResponseStruct Response { get; }
    }

    public struct AuthenticationResponseStruct : IPacketSerializable
    {
        public string Username;

        public string Password;

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            Username = reader.ReadString();
            Password = reader.ReadString();
            return reader;
        }

        public int GetLength()
        {
            return Serialize().Length;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteString(Username);
            writer.WriteString(Password);
            return writer;
        }
    }

    public struct AuthenticationRequest : IPacketSerializable
    {
        public bool NeedsPassword;

        public bool NeedsUsername;

        public ByteReader Deserialize(byte[] data)
        {
            ByteReader reader = new ByteReader(data);
            NeedsPassword = reader.ReadBool();
            NeedsUsername = reader.ReadBool();
            return reader;
        }

        public int GetLength()
        {
            return Serialize().Length;
        }

        public ByteWriter Serialize()
        {
            ByteWriter writer = new ByteWriter();
            writer.WriteBool(NeedsPassword);
            writer.WriteBool(NeedsUsername);
            return writer;
        }
    }
}
