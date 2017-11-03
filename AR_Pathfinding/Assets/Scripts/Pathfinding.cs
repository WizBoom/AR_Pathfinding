using System;
using System.Collections;
using System.Collections.Generic;
using Tango;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[System.Serializable]
public class Destination
{
    public String name;
    public Transform location;
}

public class Pathfinding : MonoBehaviour
{
    public float m_ArrivalRange = 1f;
    public Color m_LineColor = Color.blue;
    public float m_LineWidth = 1f;
    public Dropdown m_UIDestinationDropdown;
    public Text m_UIDistanceText;
    public GameObject m_LineRendererObject;
    public GameObject m_Level;
    public GameObject m_User;
    public float m_YPos = 0f;
    public MarkerCallibration m_MarkerCallibration;

    private NavMeshAgent _NavmeshAgent;
    private TangoApplication _TangoApplication;
    private TangoPointCloud _PointCloud;
    private TangoPointCloudFloor _PointCloudFloor;
    private bool _FindingFloor = false;

    // Use this for initialization
    void Start ()
	{
        _NavmeshAgent = GetComponent<NavMeshAgent>();
        _PointCloud = FindObjectOfType<TangoPointCloud>();
        _PointCloudFloor = FindObjectOfType<TangoPointCloudFloor>();
        _TangoApplication = FindObjectOfType<TangoApplication>();
    }

    void Update()
    {
        this.transform.position = m_User.transform.position;

        //var nav = GetComponent<NavMeshAgent>();
        if (_NavmeshAgent == null || _NavmeshAgent.path == null)
            return;

        //Set line properties
        var line = m_LineRendererObject.GetComponent<LineRenderer>();
        if (line == null)
        {
            line = m_LineRendererObject.gameObject.AddComponent<LineRenderer>();
            line.material = new Material(Shader.Find("Sprites/Default")) { color = m_LineColor };
        }

        line.startWidth = m_LineWidth;
        line.endWidth = m_LineWidth;
        line.startColor = m_LineColor;
        line.endColor = m_LineColor;

        var path = _NavmeshAgent.path;
        line.positionCount = path.corners.Length;

        //Distance
        if (path.corners.Length > 1)
        {
            m_UIDistanceText.gameObject.SetActive(true);
            //Get Distance
            float distance = _NavmeshAgent.remainingDistance;
            if (float.IsInfinity(distance))
                distance = CalculatePathDistance(path);

            m_UIDistanceText.text = String.Format("{0}m", distance.ToString("0.00"));
        }
        else
        {
            m_UIDistanceText.gameObject.SetActive(false);
        }

        for (int i = 0; i < path.corners.Length; i++)
        {
            Vector3 position = new Vector3(path.corners[i].x, path.corners[i].y + m_YPos, path.corners[i].z);
            line.SetPosition(i, position);
        }

        //Destination arrival
        if ((transform.position - _NavmeshAgent.destination).magnitude <= m_ArrivalRange
            && _NavmeshAgent.remainingDistance <= m_ArrivalRange)
        {
            _NavmeshAgent.ResetPath();
            m_UIDestinationDropdown.value = 0;
        }

        //Find floor
        if (!_FindingFloor)
        {
            return;
        }

        // If the point cloud floor has found a new floor, set the floor position
        if (_PointCloudFloor.m_floorFound && _PointCloud.m_floorFound)
        {
            _FindingFloor = false;

            //Set level height
            m_Level.transform.position = new Vector3(m_Level.transform.position.x, _PointCloudFloor.transform.position.y, 
                m_Level.transform.position.z);
            AndroidHelper.ShowAndroidToastMessage(string.Format("Floor found. Unity world height = {0}", 
                _PointCloudFloor.transform.position.y));
        }

    }

    void OnGUI()
    {
        GUI.color = Color.white;

        if (!_FindingFloor && m_MarkerCallibration.m_Anchored)
        {
            if (GUI.Button(new Rect(10,
                                    230, 
                                    200, 
                                    100), "<size=20>Find Floor</size>"))
            {
                if (_PointCloud == null)
                {
                    Debug.LogError("TangoPointCloud required to find floor.");
                    return;
                }

                _FindingFloor = true;
                _TangoApplication.SetDepthCameraRate(TangoEnums.TangoDepthCameraRate.MAXIMUM);
                _PointCloud.FindFloor();
                m_Level.transform.position = new Vector3(m_Level.transform.position.x, _PointCloud.m_floorPlaneY,
                    m_Level.transform.position.z);
            }
        }
        else if (m_MarkerCallibration.m_Anchored)
        {
            GUI.Label(new Rect(0, Screen.height - 50, Screen.width, 50), "<size=30>Searching for floor position. Make sure the floor is visible.</size>");
        }
    }

    public void ResetDestination()
    {
        _NavmeshAgent.ResetPath();
    }

    public void SetDestination(Vector3 position)
    {
        _NavmeshAgent.SetDestination(position);
        _NavmeshAgent.isStopped = true;
    }

    private float CalculatePathDistance(NavMeshPath path)
    {
        float distance = .0f;
        for (var i = 0; i < path.corners.Length - 1; i++)
        {
            distance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }
        return distance;
    }
}
