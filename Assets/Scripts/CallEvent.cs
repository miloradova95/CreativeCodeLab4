using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CallEvent : MonoBehaviour
{
public void Callevent(string s)
    {
    AkUnitySoundEngine.PostEvent(s, gameObject);
    Debug.Log("Event called: " + s + "Calledat: " + Time.time);
    }
}

