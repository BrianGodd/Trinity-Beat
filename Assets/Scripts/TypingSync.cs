using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
public class TypingSync : MonoBehaviour
{
    PhotonView pv;
    public TextMeshPro textMeshPro;

    void Start()
    {
        pv = GetComponent<PhotonView>();
        
    }
    public void ChangeWord(int idx, char c)
    {
        char[] chars = textMeshPro.text.ToCharArray();
        chars[idx * 2] = c;
        pv.RPC(nameof(RPC_ChangeWord), RpcTarget.All, new string(chars));

    }
    public void InitWord()
    {
        pv.RPC(nameof(RPC_ChangeWord), RpcTarget.All, "_ _ _");
    }

    [PunRPC]
    void RPC_ChangeWord(string newText, PhotonMessageInfo info)
    {
        textMeshPro.text = newText;
    }
}
