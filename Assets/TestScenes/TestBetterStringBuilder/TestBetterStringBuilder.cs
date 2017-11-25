using UnityEngine;
using System.Collections;
using CustomDataStruct;

/// <summary>
/// 说明：BetterStringBuilder正确性、性能、GC测试脚本
/// 
/// @by wsh 2017-06-16
/// </summary>

public class TestBetterStringBuilder : MonoBehaviour {

    BetterStringBuilder bsb;
    char[] testCharArr = new char[3] { '1', '4', '5' };

    // Use this for initialization
    void Start () {
        bsb = new BetterStringBuilder(512, 1024);
    }
	
	// Update is called once per frame
	void Update () {
        Profiler.BeginSample("TestBetterStringBuilder");
        bsb.Clear();
        bsb.Append(testCharArr);
        bsb.AppendLine("---");
        bsb.Append('9', 100);
        bsb.AppendLine();
        bsb.Append(4785);
        bsb.AppendLine();
        bsb.Append(66666666666L);
        bsb.AppendLine();
        bsb.Append(-66666666666L);
        bsb.AppendLine();
        bsb.Append("dddstgppp", 2, 3);
        bsb.AppendLine();
        bsb.Append(bsb.Length);
        bsb.Append(testCharArr, 1, 2);
        bsb.Append(bsb.Length);
        bsb.AppendLine();
        bsb.Append(-10.12355, 3);
        bsb.AppendLine();
        bsb.CopyTo(0, testCharArr, 0, 3);
        bsb.Remove(0,4);
        bsb.Insert(1, "7777777");
        bsb.Replace("77777","88888");
        bsb.Replace('5', '*');
        Profiler.EndSample();
        Debug.Log(bsb.ToString());
    }
}
