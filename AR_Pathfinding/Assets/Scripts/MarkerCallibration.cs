using System;
using System.Collections.Generic;
using System.Net.Mime;
using Tango;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class Marker
{
    public int index;
    public Transform markerTransform;
}

public class MarkerCallibration : MonoBehaviour, ITangoVideoOverlay
{
    private const double MARKER_SIZE = 0.1397;

    public GameObject m_MarkerPrefab;
    public GameObject m_Level;
    public Pathfinding m_Pathfinder;
    [Range(-360f, 360f)]
    public float m_AdditionalAngle = 90f;
    //public Text m_UIText;
    public GameObject m_UICanvas;
    public Button m_UIButton;
    public Dropdown m_UIDestinationDropdown;
    public List<Marker> m_Markers;
    public List<Destination> m_Destinations;
    public GameObject m_TangoCamera;
    public bool m_Anchored = false;

    private Dictionary<String, GameObject> _MarkerObjects;
    private int _CurrentMarker = -1;
    private Vector3 _TranslationCurrentMarker = Vector3.zero;
    private Quaternion _RotationCurrentMarker = Quaternion.identity;
    private List<TangoSupport.Marker> _MarkerList;
    private TangoApplication m_TangoApplication;

    public void Start()
    {
        m_TangoApplication = FindObjectOfType<TangoApplication>();
        if (m_TangoApplication != null)
        {
            m_TangoApplication.Register(this);
        }
        else
        {
            Debug.Log("No Tango Manager found in scene.");
        }

        _MarkerList = new List<TangoSupport.Marker>();
        _MarkerObjects = new Dictionary<String, GameObject>();

        List<String> stringMarkers = new List<String>();
        stringMarkers.Add("");
        foreach (var dest in m_Destinations)
        {
            stringMarkers.Add(dest.name);
        }
        m_UIDestinationDropdown.ClearOptions();
        m_UIDestinationDropdown.AddOptions(stringMarkers);
    }

    public void OnTangoImageAvailableEventHandler(TangoEnums.TangoCameraId cameraId,
        TangoUnityImageData imageBuffer)
    {
        if (!m_Anchored)
        {
            //Detect markers
            TangoSupport.DetectMarkers(imageBuffer, cameraId,
                TangoSupport.MarkerType.ARTAG, MARKER_SIZE, _MarkerList);

            //Set text to scanning
            if (_MarkerList.Count <= 0)
            {
                m_UICanvas.SetActive(true);
                m_UIButton.enabled = false;
                m_UIButton.gameObject.SetActive(false);
                _CurrentMarker = -1;

                foreach (var go in _MarkerObjects)
                {
                    Destroy(go.Value);
                }
                _MarkerObjects.Clear();

            }
            
            //Loop over all the markers found and put a prefab on them
            for (int i = 0; i < _MarkerList.Count; ++i)
            {
                TangoSupport.Marker marker = _MarkerList[i];

                //Check if object already exists
                if (_MarkerObjects.ContainsKey(marker.m_content))
                {
                    //Update marker
                    GameObject markerObject = _MarkerObjects[marker.m_content];
                    markerObject.GetComponent<MarkerVisualizationObject>().SetMarker(marker);
                }
                //if object doesn't exist, make a new one
                else
                {
                    //Instaniate and update
                    GameObject markerObject = Instantiate<GameObject>(m_MarkerPrefab);
                    _MarkerObjects.Add(marker.m_content, markerObject);
                    markerObject.GetComponent<MarkerVisualizationObject>().SetMarker(marker);
                }

                //Enable anchor button
                m_UIButton.enabled = true;
                m_UIButton.gameObject.SetActive(true);

                //Set current marker id
                int currentMarkerInt = 0;
                bool success = int.TryParse(marker.m_content, out currentMarkerInt);
                if (success)
                {
                    if (_CurrentMarker != currentMarkerInt)
                        AndroidHelper.ShowAndroidToastMessage(string.Format("Found marker {0}", currentMarkerInt),
                            AndroidHelper.ToastLength.SHORT);
                    _CurrentMarker = currentMarkerInt;
                }
                _TranslationCurrentMarker = marker.m_translation;
                _RotationCurrentMarker = marker.m_orientation;
                m_UICanvas.SetActive(false);
            }
        }
        else
        {
            foreach (var go in _MarkerObjects)
            {
                Destroy(go.Value);
            }
            _MarkerObjects.Clear();
        }
    }

    public void OnButton()
    {
        //If button is pressed, check if user isn't already anchored and there is a current marker
        if (!m_Anchored && _CurrentMarker >= 0)
        {
            //Find corresponding marker
            foreach (var marker in m_Markers)
            {
                if (marker.index == _CurrentMarker)
                {
                    //Update position by moving the level
                    m_Level.transform.rotation = Quaternion.Euler(m_Level.transform.rotation.eulerAngles.x,
                        -marker.markerTransform.rotation.eulerAngles.y + m_AdditionalAngle + _RotationCurrentMarker.eulerAngles.y, 
                        m_Level.transform.rotation.eulerAngles.z);
                    m_Level.transform.position -= marker.markerTransform.position;
                    m_Level.transform.position += _TranslationCurrentMarker;

                    //Update UI
                    m_Anchored = true;

                    AndroidHelper.ShowAndroidToastMessage(string.Format("Anchored on Marker {0}", _CurrentMarker));
                    m_UICanvas.SetActive(false);
                    m_UIButton.gameObject.SetActive(false);
                    m_UIButton.enabled = true;
                    return;
                }
            }
            AndroidHelper.ShowAndroidToastMessage(string.Format("Marker {0} isn't valid!", _CurrentMarker));
        }
    }

    private void OnGUI()
    {

        if (GUI.Button(new Rect(10,
                                120,
                                200,
                                100), "<size=20>Anchor Reset</size>"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void DestinationSelection()
    {
        int index = m_UIDestinationDropdown.value - 1;
        if (index >= 0)
            m_Pathfinder.SetDestination(m_Destinations[index].location.position);
        else
            m_Pathfinder.ResetDestination();
    }
}
