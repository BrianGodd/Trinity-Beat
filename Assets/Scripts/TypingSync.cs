using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;
public class TypingSync : MonoBehaviour
{
    ComboRecorder comboRecorder;
    PhotonView pv;
    public TextMeshProUGUI textMeshPro;

    void Start()
    {
        pv = GetComponent<PhotonView>();
        comboRecorder = FindObjectOfType<ComboRecorder>();
        comboRecorder.typingSync = this;
    }
    public void ChangeWord(int idx, char c)
    {
        if(!pv.IsMine)return;
        char[] chars = textMeshPro.text.ToCharArray();
        chars[idx * 2] = c;
        pv.RPC(nameof(RPC_ChangeWord), RpcTarget.All, new string(chars));

    }
    public void InitWord()
    {
        if(!pv.IsMine)return;
        pv.RPC(nameof(RPC_ChangeWord), RpcTarget.All, "_ _ _");
    }

    [PunRPC]
    void RPC_ChangeWord(string newText, PhotonMessageInfo info)
    {
        textMeshPro.text = newText;
    }
}
