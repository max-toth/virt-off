using UnityEngine;

namespace Game.Scripts.Net.Config
{
    [CreateAssetMenu(menuName = "Virt-Off/ServerConfig")]
    public sealed class ServerConfig : ScriptableObject
    {
        public const string ResourcePath = "ServerConfig";
        public const string DefaultAddress = "193.218.141.214";
        public const ushort DefaultPort = 7777;

        [SerializeField] private string address = DefaultAddress;
        [SerializeField] private ushort port = DefaultPort;

        public string Address => address;
        public ushort Port => port;
    }
}
