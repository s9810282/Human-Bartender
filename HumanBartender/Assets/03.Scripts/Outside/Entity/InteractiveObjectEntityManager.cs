using System.Collections.Generic;
using UnityEngine;

public struct ObjectEntity
{
    public string id;
    public InteractiveObjectEntity entity;
}

public class InteractiveObjectEntityManager : MonoBehaviour
{
    [SerializeField] protected OutsideObjectDataSO obejctData;
    [SerializeField] protected List<ObjectEntity> entitys;

    //day 값 보고 검사 하기.
    void Start()
    {
        
    }
    
}
