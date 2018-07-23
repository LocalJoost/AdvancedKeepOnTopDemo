using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HoloToolkit.Unity.SpatialMapping;
using HoloToolkitExtensions.Utilities;
using UnityEngine;

public class ObjectListToggler : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> _toggleObjects = new List<GameObject>();

    private int _listCount;
    private int _objIdx = 0;
    private DoubleClickPreventer _doubleClickPreventer;

    public GameObject LineDrawer;

    void Start()
    {
        _doubleClickPreventer = new DoubleClickPreventer();
        _listCount = _toggleObjects.Count;

        if (_listCount < 1)
        {
            return;
        }
        foreach (var obj in _toggleObjects)
        {
           obj.SetActive(false);
        }
       
        // Give the spatial mapping some time to start
        StartCoroutine(ShowFirstObject());
    }

    private IEnumerator ShowFirstObject()
    {
        yield return new WaitForSeconds(2f);
        _toggleObjects[0].SetActive(true);
    }

    public void Toggle()
    {
        if (!_doubleClickPreventer.CanClick())
        {
            return;
        }

        foreach (var obj in _toggleObjects)
        {
            obj.SetActive(false);
        }

        StartCoroutine(ShowDelayed());
    }

    private IEnumerator ShowDelayed()
    {
        yield return new WaitForSeconds(0.2f);
        var newActiveObject = _toggleObjects[(++_objIdx) % _listCount];
        newActiveObject.SetActive(true);

        Debug.Log("Activating " + _objIdx % _listCount);
        GetComponent<AudioSource>().Play();
    }
}
