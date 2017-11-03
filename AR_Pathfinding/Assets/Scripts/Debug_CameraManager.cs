using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debug_CameraManager : MonoBehaviour {

    public List<GameObject> m_DebugObjects;
    public Material m_DebugMaterial;
    public Material m_DebugFloorMaterial;
    public Material m_DebugWallMaterial;
    public Material m_DepthMaskMaterial;
    public GameObject m_Blockout;
    public Camera m_TangoCamera;
    private bool _DebugMode = false;

    void Start()
    {
        DebugMode();
    }

    void DebugMode()
    {
        //Loop over all objects and enable / disable them
        foreach (var o in m_DebugObjects)
        {
            o.SetActive(_DebugMode);
        }

        //Enable / disable the tango camera
        m_TangoCamera.enabled = !_DebugMode;

        //Loop over all renderers and disable / enable them
        foreach (var child in m_Blockout.GetComponentsInChildren<Renderer>())
        {
            if (child)
            {
                if (_DebugMode)
                {
                    if (child.gameObject.tag == "Floor")
                    {
                        child.enabled = true;
                    }
                    else if (child.gameObject.tag == "Wall")
                    {
                        child.material = m_DebugWallMaterial;
                    }
                    else
                    {
                        child.material = m_DebugMaterial;
                    }
                }
                else
                {
                    child.material = m_DepthMaskMaterial;
                    if (child.gameObject.tag == "Floor")
                        child.enabled = false;
                }
            }
        }
    }

    private void OnGUI()
    {
        //Button to go into debug mode
        if (GUI.Button(new Rect(10,
                                10,
                                200,
                                100), "<size=20>Debug</size>"))
        {
            _DebugMode = !_DebugMode;
            DebugMode();
        }
    }
}
