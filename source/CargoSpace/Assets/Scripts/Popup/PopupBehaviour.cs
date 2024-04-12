using System;
using TMPro;
using UnityEngine;

public class PopupBehaviour : MonoBehaviour
{
    public TMP_Text Output;

    public void ButtonClick()
    {
        Output.text = DateTime.Now.ToString("HH:mm:ss.ffff");
    }
}
