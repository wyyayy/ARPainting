using UnityEngine;
using System.Collections;

public class ManagerComponent : MonoBehaviour {

    void Awake()
    {
        GameObject.DontDestroyOnLoad(this.gameObject);

        _onInit();
    }    

    virtual protected void _onInit()
    {

    }
}
