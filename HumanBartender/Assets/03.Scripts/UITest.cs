using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

public class UITest : MonoBehaviour
{
    [SerializeField] UIDialogueTextView textView;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textView.StartType(
            new TypingData(
            "측정 너비에 추가할 안전 마진 (wrap 경계 흔들림 방지)\n측정 너비에 추가할 안전 마진 (wrap 경계 흔들림 방지)측정 너비에 추가할 안전 마진 (wrap 경계 흔들림 방지)", 
            "Luna", 
            Vector2.zero,
            Color.white, 
            true
            ));
    }
}
