using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable] // 직렬화
public class Dialogue
{
    public int ID;
    public string charaName;
    public string context;
    public float contextDelay; // 다음 대화로 넘어갈때 딜레이
    public Sprite backgroundSprite;
    public string sceneEffect;
    public float effectTime;

    public Dialogue (int _ID, string _CharaName, string _Context, float _ContextDelay, string _BackgroundImageName, string _SceneEffect, float _EffectTime)
    {
        ID = _ID;
        charaName = _CharaName;
        context = _Context;
        contextDelay = _ContextDelay;
        backgroundSprite = Resources.Load<Sprite>("Images/BackgroundImages/" + _BackgroundImageName);

        if (backgroundSprite == null)
            Debug.LogError("Dialogue : Dialogue " + ID + " sprite is Null");

        sceneEffect = _SceneEffect;
        effectTime = _EffectTime;
    }
}
