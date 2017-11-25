using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TestLinq : MonoBehaviour {

    bool isTestingCorrectness = false;
    string[] names = new string[] { "Tom", "Dick", "Harry" , "Query"};

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        Test01();
        Test02();
    }

    void Test01()
    {
        //IEnumerable<string> filterNames = Enumerable.Where(names, n => n.Length >= 4);// 80B GC
        IEnumerable<string> filterNames = names.Where(n => n.Length >= 4);// 80B GC
        if (isTestingCorrectness)
        {
            foreach (string var in filterNames)
            {
                Debug.Log("[Test01]" + var);
            }
        }
    }

    void Test02()
    {
        IEnumerable<string> filterNames = names
            .Where(n => n.Contains("a"))
            .OrderBy(n => n.Length)
            .Select(n => n.ToUpper());//216B
        if (isTestingCorrectness)
        {
            foreach (string var in filterNames)
            {
                Debug.Log("[Test02]" + var);
            }
        }
    }
}
