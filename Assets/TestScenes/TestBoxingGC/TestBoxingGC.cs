using UnityEngine;
using System.Collections;
using CustomDataStruct;

/// <summary>
/// 说明：装箱的GC测试，解决方式：1）使用泛型；2）使用结构体
/// 
/// @by wsh 2017-06-17
/// </summary>

public class TestBoxingGC : MonoBehaviour
{
    public delegate void TestDelegate1(object arg1, object arg2);
    public delegate void TestDelegate2<T,V>(T arg1, V arg2);
    TestDelegate1 mDelegate1;
    TestDelegate2<string, int> mDelegate2;

    // Use this for initialization
    void Start () {
        mDelegate1 = new TestDelegate1(StaticDelegateFun);
        mDelegate2 = new TestDelegate2<string, int>(StaticDelegateFun);
    }
	
	// Update is called once per frame
	void Update () {
        //TestFun1(mDelegate1); //由于int类型的装箱，每次调用产生20B垃圾
        TestFun2(mDelegate2); //无GC
    }

    private void TestFun1(TestDelegate1 de)
    {
        de("test", 1000);
    }

    private void TestFun2(TestDelegate2<string, int> de)
    {
        de("test", 1000);
    }

    private static void StaticDelegateFun(object arg1, object arg2)
    {
        Debug.Log(arg1);
        Debug.Log(arg2);
    }

    private static void StaticDelegateFun(string arg1, int arg2)
    {
        Debug.Log(arg1);
        Debug.Log(arg2);
    }
}
