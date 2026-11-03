using UnityEngine;

[CreateAssetMenu(menuName = "Virt-Off/ServerConfig")]
public class ServerConfig : ScriptableObject
{
    public string address = "193.218.141.214";
    public ushort port = 7777;
}
