using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using VContainer;

public class DialogueTriggerManager : MonoBehaviour
{
    [Inject] IObjectResolver resolver;

    Action triggerCallBack = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }


    public void ExecuteTriggerCoroutine(TriggerData trigger, Action callBack)
    {
        Debug.Log($"[트리거 시작] 타입: {trigger.type}");
        triggerCallBack = callBack;

        IDialogueCommand command = DialogueCommandFactory.CreateCommand(trigger);
        if (command != null)
        {
            if (resolver == null)
            {
                Debug.LogError("DI 에러] DialogueManager가 resolver를 받지 못했습니다!");
            }

            resolver.Inject(command);
            StartCoroutine(command.Execute());
        }
    }

    public void CompleteTrigger()
    {
        if (triggerCallBack != null)
        {
            triggerCallBack.Invoke();
            triggerCallBack = null;
        }
    }
}
