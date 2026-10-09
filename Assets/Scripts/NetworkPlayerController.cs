using UnityEngine;

public class NetworkPlayerController : MonoBehaviour
{
    void Start()
    {
        InvokeRepeating(nameof(CheckConnection), 0, 0.1f);
    }

    void CheckConnection()
    {
        var nm = GameNet.Instance;
        if (nm == null || !nm.IsConnected) return;

        CancelInvoke(nameof(CheckConnection));
        GetComponent<PlayerController>().enabled = true;
        Debug.Log($"[Player] Ready! LocalId={nm.LocalId}");
    }
}
