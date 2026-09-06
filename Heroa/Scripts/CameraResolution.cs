using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraResolution : MonoBehaviour
{

    [SerializeField]
    private bool isOnPreCull = true;

    // 해상도에 따른 카메라 조정
    private void Awake()
    {

        Camera camera = GetComponent<Camera>();
        Rect rect = camera.rect;
        float scaleHeight = ((float)Screen.width / Screen.height) / ((float)9 / 16); // (가로 / 세로) , portrait 일때
        float scaleWidth = 1f / scaleHeight;

        if (scaleHeight < 1)
        {
            rect.height = scaleHeight;
            rect.y = (1f - scaleHeight) / 2f;
        }
        else
        {
            rect.width = scaleWidth;
            rect.x = (1f - scaleWidth) / 2f;
        }
        camera.rect = rect;
    }

    private void OnPreCull()
    {
        if (isOnPreCull == true)
            GL.Clear(true, true, Color.black);
    }

    //void OnPreCull() => GL.Clear(true, true, Color.black); // 배경 검게 칠해주는건데 UI랑 스프라이트랑 같이 안나옴

}

