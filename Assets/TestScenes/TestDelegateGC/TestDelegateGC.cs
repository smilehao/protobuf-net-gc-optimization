using UnityEngine;
using System.Collections;
using System;
using System.Reflection;
using System.Collections.Generic;

/// <summary>
/// 说明：委托使用方式的GC测试
/// 
/// 结论：
///     1）所有委托必须缓存，产生GC的测试一律是因为每次调用都生成了一个新的委托
/// 
/// @by wsh 2017-06-17
/// </summary>

public class TestDelegateGC : MonoBehaviour
{
    public delegate void TestDelegate(GameObject go, string str, int num);
    public delegate void TestTDelegate<T,U,V>(T go, U str, V num);

    Delegate mDelegate1;
    Delegate mDelegate2;
    TestDelegate mDelegate3;
    TestTDelegate<GameObject, string, int> mDelegate4;
    TestDelegate mDelegate5;
    Comparison<int> mDelegate6;
    Comparison<int> mDelegate7;

    int mTestPriviteData;
    List<int> mTestList = new List<int>();

    // Use this for initialization
    void Start () {
        mTestPriviteData = 100;

        mDelegate1 = (TestDelegate)DelegateFun;
        mDelegate2 = Delegate.CreateDelegate(typeof(TestDelegate), this, "DelegateFun");
        mDelegate3 = DelegateFun;

        mDelegate4 = TDelegateFun;

        //static
        mDelegate5 = new TestDelegate(StaticDelegateFun);
        mDelegate6 = SortByXXX;
        mDelegate7 = TSortByXXX<int>;

        mTestList.Add(1);
        mTestList.Add(2);
        mTestList.Add(3);
    }
	
	// Update is called once per frame
	void Update () {
        // 不使用泛型
        Profiler.BeginSample("no cached");
        TestFun(DelegateFun); //每次调用产生104B垃圾
        Profiler.EndSample();

        Profiler.BeginSample("imp reflection covert cached");
        TestFun(mDelegate1 as TestDelegate); //无GC
        Profiler.EndSample();

        Profiler.BeginSample("reflection covert cached");
        TestFun(mDelegate2 as TestDelegate); //无GC
        Profiler.EndSample();

        Profiler.BeginSample("private cached");
        TestFun(mDelegate3); //无GC，推荐
        Profiler.EndSample();

        Profiler.BeginSample("static cached");
        TestFun(mDelegate5); //无GC
        Profiler.EndSample();

        // 使用泛型，更加通用
        Profiler.BeginSample("genericity no cached");
        TestTFun(TDelegateFun, gameObject, "test", 1000);//每次调用产生104B垃圾
        Profiler.EndSample();

        Profiler.BeginSample("genericity cached");
        TestTFun(mDelegate4, gameObject, "test", 1000);// 无GC，更通用，极力推荐***********
        Profiler.EndSample();

        // Sort测试
        Profiler.BeginSample("sort original");
        mTestList.Sort();//无GC
        Profiler.EndSample();

        Profiler.BeginSample("sort no cached");
        TestSort(SortByXXX);//每次调用产生104B垃圾
        Profiler.EndSample();

        Profiler.BeginSample("sort cached");
        TestSort(mDelegate6);//无GC
        Profiler.EndSample();

        Profiler.BeginSample("sort genericity fun no cached");
        TestSort(TSortByXXX);//每次调用产生104B垃圾
        Profiler.EndSample();

        Profiler.BeginSample("sort genericity call&fun no cached");
        TestSort(TSortByXXX);//每次调用产生104B垃圾
        Profiler.EndSample();

        Profiler.BeginSample("sort genericity call&fun cached");
        TestSort(mDelegate7);//无GC
        Profiler.EndSample();
    }

    private void TestFun(TestDelegate de)
    {
        de(gameObject, "test", 1000);
    }

    private void TestTFun<T, U, V>(TestTDelegate<T, U, V> de, T arg0, U arg1, V arg2)
    {
        de(arg0, arg1, arg2);
    }

    private void TestSort<T>(List<T> list, Comparison<T> sortFunc)
    {
        list.Sort(sortFunc);
    }

    private void TestSort(Comparison<int> sortFunc)
    {
        mTestList.Sort(sortFunc);
    }

    private void DelegateFun(GameObject go, string str, int num)
    {
        // 说明：用于正确性测试；测试GC时需要注释掉
        //Debug.Log("DelegateFun");
        //Debug.Log(mTestPriviteData);
        //Debug.Log(go.name);
        //Debug.Log(str);
        //Debug.Log(num);
    }

    private void TDelegateFun<T, U, V>(T go, U str, V num)
    {
        // 说明：用于正确性测试；测试GC时需要注释掉
        //Debug.Log("TDelegateFun");
        //Debug.Log(mTestPriviteData);
        //Debug.Log((go as GameObject).name);
        //Debug.Log(str);
        //Debug.Log(num);
    }

    private static void StaticDelegateFun(GameObject go, string str, int num)
    {
        // 说明：用于正确性测试；测试GC时需要注释掉
        //Debug.Log("StaticDelegateFun");
        //无法访问非静态变量，不建议使用，如果非要用，解决方式有：
        //  1）如果变量为公有，可以简单地传递this指针过来（注意：异步回调时一定要先判空）；
        //  2）如果变量为私有，可以定义一个结构体，将数据传递过来；
        //  3）如果回调为Comparison<T>这类不能自定义参数的，可以自定义IComparer<T>类；
        //Debug.Log(mTestPriviteData);
    }

    private int SortByXXX(int x, int y)
    {
        return x.CompareTo(y);
    }

    private int TSortByXXX<T>(T x, T y) where T : IComparable<T>
    {
        return x.CompareTo(y);
    }
}
