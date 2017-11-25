#define LOGGERMODE
#define DEBUG
using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System;

public class TestConditional : MonoBehaviour {

    int i = 0;
	// Use this for initialization
	void Start () {
	    
	}
	
	// Update is called once per frame
	void Update () {
        i++;
        // 说明：注释#define LOGGERMODE以后，以下的调用函数都不会参与编译，装箱、字符串拼接等GC问题都不存在
        TestLog.Log("This is a test line with string = <{0}>, and int = <{1}>", i.ToString(), i);
	}

}

class TestLog
{
    [Conditional("LOGGERMODE")]
    static public void Log(string s, params object[] p)
    {
        UnityEngine.Debug.Log(DateTime.Now + " -- " + string.Format(s, p));
    }
}
