//-----------------------------------------------------------------------
// <copyright file="MarkerDetectionUIController.cs" company="Google">
//
// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Mime;
using Tango;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Detect a single AR Tag marker and place a virtual reference object on the
/// physical marker position.
/// </summary>
public class MarkerDetectionUIController : MonoBehaviour, ITangoVideoOverlay
{
    /// <summary>
    /// The prefabs of marker.
    /// </summary>
    public GameObject m_markerPrefab;
    public GameObject m_Level;
    public Pathfinding m_Pathfinder;
    [Range(-360f, 360f)]
    public float m_AdditionalAngle = 90f;

    /// <summary>
    /// Length of side of the physical AR Tag marker in meters.
    /// </summary>
    private const double MARKER_SIZE = 0.1397;

    public Text m_UIText;
    public Button m_UIButton;
    public Dropdown m_UIDestinationDropdown;

    private bool m_Anchored = false;
    private int m_CurrentMarker = -1;
    public List<Marker> m_Markers;
    public List<Destination> m_Destinations;
    public GameObject m_TangoCamera;

    /// <summary>
    /// The objects of all markers.
    /// </summary>
    private Dictionary<String, GameObject> m_markerObjects;

    /// <summary>
    /// The list of markers detected in each frame.
    /// </summary>
    private List<TangoSupport.Marker> m_markerList;

    /// <summary>
    /// A reference to TangoApplication in current scene.
    /// </summary>
    private TangoApplication m_tangoApplication;

    /// <summary>
    /// Unity Start function.
    /// </summary>
    public void Start()
    {
        m_tangoApplication = FindObjectOfType<TangoApplication>();
        if (m_tangoApplication != null)
        {
            m_tangoApplication.Register(this);
        }
        else
        {
            Debug.Log("No Tango Manager found in scene.");
        }

        m_markerList = new List<TangoSupport.Marker>();
        m_markerObjects = new Dictionary<String, GameObject>();

        List<String> stringMarkers = new List<String>();
        stringMarkers.Add("");
        foreach (var dest in m_Destinations)
        {
            stringMarkers.Add(dest.name);
        }
        m_UIDestinationDropdown.ClearOptions();
        m_UIDestinationDropdown.AddOptions(stringMarkers);
        //m_UIDestinationDropdown.gameObject.SetActive(false);
    }

    /// <summary>
    /// Detect one or more markers in the input image.
    /// </summary>
    /// <param name="cameraId">
    /// Returned camera ID.
    /// </param>
    /// <param name="imageBuffer">
    /// Color camera image buffer.
    /// </param>
    public void OnTangoImageAvailableEventHandler(TangoEnums.TangoCameraId cameraId,
        TangoUnityImageData imageBuffer)
    {
        if (!m_Anchored)
        {
            TangoSupport.DetectMarkers(imageBuffer, cameraId,
                TangoSupport.MarkerType.ARTAG, MARKER_SIZE, m_markerList);

            if (m_markerList.Count <= 0)
            {
                m_UIText.text = "Scanning";
                m_UIButton.enabled = false;
                m_UIButton.gameObject.SetActive(false);
                m_CurrentMarker = -1;

                foreach (var go in m_markerObjects)
                {
                    Destroy(go.Value);
                }
                m_markerObjects.Clear();

            }
            for (int i = 0; i < m_markerList.Count; ++i)
            {
                TangoSupport.Marker marker = m_markerList[i];

                if (m_markerObjects.ContainsKey(marker.m_content))
                {
                    GameObject markerObject = m_markerObjects[marker.m_content];
                    markerObject.GetComponent<MarkerVisualizationObject>().SetMarker(marker);
                }
                else
                {
                    GameObject markerObject = Instantiate<GameObject>(m_markerPrefab);
                    m_markerObjects.Add(marker.m_content, markerObject);
                    markerObject.GetComponent<MarkerVisualizationObject>().SetMarker(marker);
                }

                m_UIButton.enabled = true;
                m_UIButton.gameObject.SetActive(true);
                int currentMarkerInt = 0;
                bool success = int.TryParse(marker.m_content, out currentMarkerInt);
                if (success)
                    m_CurrentMarker = currentMarkerInt;
                m_UIText.text = string.Format("Found marker {0}",m_CurrentMarker);
            }
        }
    }

    public void OnButton()
    {
        if (!m_Anchored && m_CurrentMarker >= 0)
        {
            //Find corresponding marker
            foreach (var marker in m_Markers)
            {
                if (marker.index == m_CurrentMarker)
                {
                    //Update position
                    //m_TangoCamera.transform.position = marker.position.position;
                    m_Level.transform.rotation = Quaternion.Euler(m_Level.transform.rotation.eulerAngles.x,
                        -marker.markerTransform.rotation.eulerAngles.y + m_AdditionalAngle, m_Level.transform.rotation.eulerAngles.z);
                    m_Level.transform.position -= marker.markerTransform.position;
                    m_Level.transform.position -= marker.markerTransform.position;

                    //Update UI
                    m_Anchored = true;
                    //m_UIDestinationDropdown.gameObject.SetActive(true);
                    m_UIText.text = string.Format("Anchored (Marker {0})", m_CurrentMarker);
                    m_UIButton.gameObject.SetActive(false);
                    m_UIButton.enabled = true;
                    return;
                }
            }

            //Marker isn't valid
            //TODO: Debug

        }
    }

    private void OnGUI()
    {

        if (GUI.Button(new Rect(0,
                                110,
                                200,
                                100), "<size=20>Anchor Reset</size>"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            //m_Anchored = false;
            //m_CurrentMarker = -1;
            //m_Level.transform.position = Vector3.zero;
            //m_Level.transform.rotation = Quaternion.identity;
            //m_TangoCamera.transform.position = Vector3.zero;
            //PoseProvider.ResetMotionTracking();
            //m_TangoCamera.GetComponent<TangoApplication>().Shutdown();
            //m_TangoCamera.GetComponent<TangoApplication>().Startup(null);
        }
    }

    public void DestinationSelection()
    {
        int index = m_UIDestinationDropdown.value - 1;
        if (index >= 0)
            m_Pathfinder.SetDestination(m_Markers[index].markerTransform.position);
        else
            m_Pathfinder.ResetDestination();
    }
}