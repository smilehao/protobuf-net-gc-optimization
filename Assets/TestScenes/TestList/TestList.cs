using UnityEngine;
using System.Collections;
using System;
using System.Text;
using CustomDataStruct;
using System.Collections.Generic;

/// <summary>
/// 说明：ListGC测试脚本
/// 
/// 结论：
///     1）GetEnumerator无GC产生，foreach有GC
///     2）对于自定义结构，必须继承IEquatable<T>接口，否则Roemove、Cotains、IndexOf、sort每次都有GC产生
///     3）对于Sort，需要传递一个委托，关于委托的无GC使用方式参考TestDelegateGC
///     4）BetterList相比较于List，在GC上没有任何优化效果，反而由于Sort必须要委托，更加容易产生GC，GetEnumerator也有GC
/// 
/// @by wsh 2017-06-16
/// </summary>

public class TestList : MonoBehaviour
{
    // 重要：对于自定义结构一定要继承IEquatable<T>接口并实现它，
    // 否则BetterList内部的Equality<T>比较器的获取将会把CustomStruct当作object去比较
    // 因为装箱而导致垃圾
    // 此外：对于Sort，实现IComparable<T>接口，则在传入委托的时候可以和系统简单值类型一样
    public struct CustomStruct : IEquatable<CustomStruct>, IComparable<CustomStruct>
    {
        public int a;
        public string b;

        public CustomStruct(int a, string b)
        {
            this.a = a;
            this.b = b;
        }

        public bool Equals(CustomStruct other)
        {
            return a == other.a && b == other.b;
        }

        public int CompareTo(CustomStruct other)
        {
            if (a != other.a)
            {
                return a.CompareTo(other.a);
            }

            if (b != other.b)
            {
                return b.CompareTo(other.b);
            }

            return 0;
        }

        // 说明：测试正确性用的，不是必须
        public override string ToString()
        {
            return string.Format("<a = {0}, b = {1}>", a, b);
        }
    }

    public class CustomClass : IComparable<CustomClass>
    {
        public int a;
        public string b;
        CustomStruct c;

        public int CompareTo(CustomClass other)
        {
            if (a != other.a)
            {
                return a.CompareTo(other.a);
            }

            if (b != other.b)
            {
                return b.CompareTo(other.b);
            }

            if (!c.Equals(other.c))
            {
                return c.CompareTo(other.c);
            }

            return 0;
        }

        // 说明：测试正确性用的，不是必须
        public override string ToString()
        {
            return string.Format("<a = {0}, b = {1}, c = {2}>",
                a, b, c.ToString());
        }
    }

    List<int> mList1 = new List<int>();
    List<string> mList2 = new List<string>();
    List<CustomStruct> mList3 = new List<CustomStruct>();
    List<CustomClass> mList4 = new List<CustomClass>();
    CustomClass mTestClass1;
    CustomClass mTestClass2;
    BetterStringBuilder mBsb;

    Comparison<int> mSortInt;
    Comparison<string> mSortString;
    Comparison<CustomStruct> mSortStruct;
    Comparison<CustomClass> mSortClass;

    // Use this for initialization
    void Start()
    {
        mBsb = new BetterStringBuilder(30, 0, BetterStringBuilderBufferType.None);
        mTestClass1 = new CustomClass();
        mTestClass1.a = 2;
        mTestClass2 = new CustomClass();
        mTestClass2.a = 1;

        mSortInt = TestSort;
        mSortString = TestSort;
        mSortStruct = TestSort;
        mSortClass = TestSort;
    }

    // Update is called once per frame
    void Update()
    {
        Profiler.BeginSample("TestList.TestAdd");
        TestAdd(mList1, 2);
        TestAdd(mList2, "test2");
        TestAdd(mList3, new CustomStruct(2, "test2"));
        TestAdd(mList4, mTestClass1);
        Profiler.EndSample();

        LogList(mList1);
        LogList(mList2);
        LogList(mList3);
        LogList(mList4);

        Profiler.BeginSample("TestList.TestAdd");
        TestAdd(mList1, 1);
        TestAdd(mList2, "test1");
        TestAdd(mList3, new CustomStruct(1, "test1"));
        TestAdd(mList4, mTestClass2);
        Profiler.EndSample();

        LogList(mList1);
        LogList(mList2);
        LogList(mList3);
        LogList(mList4);

        Profiler.BeginSample("TestList.TestSort(no cache delegate)");
        // 对于下面的每次调用，产生104B垃圾
        TestSort(mList1, TestSort);
        TestSort(mList2, TestSort);
        TestSort(mList3, TestSort);
        TestSort(mList4, TestSort);
        Profiler.EndSample();

        Profiler.BeginSample("TestList.TestSort(cached delegate)");
        // 正确使用方式，无GC
        TestSort(mList1, mSortInt);
        TestSort(mList2, mSortString);
        TestSort(mList3, mSortStruct);
        TestSort(mList4, mSortClass);
        Profiler.EndSample();

        Profiler.BeginSample("TestList.TestSort(original)");
        // 正确使用方式，无GC
        mList1.Sort();
        mList2.Sort();
        mList3.Sort();
        mList4.Sort();
        Profiler.EndSample();

        LogList(mList1);
        LogList(mList2);
        LogList(mList3);
        LogList(mList4);

        Profiler.BeginSample("TestList.TestGetEnumerator");
        // 无GC
        TestGetEnumerator(mList1);
        TestGetEnumerator(mList2);
        TestGetEnumerator(mList3);
        TestGetEnumerator(mList4);
        Profiler.EndSample();

        Profiler.BeginSample("TestList.TestForeach");
        // 对于下面的每次调用，产生40B垃圾
        TestForeach(mList1);
        TestForeach(mList2);
        TestForeach(mList3);
        TestForeach(mList4);
        Profiler.EndSample();

        Profiler.BeginSample("TestList.TestRemove");
        TestRemove(mList1, 1);
        TestRemove(mList2, "test1");
        TestRemove(mList3, new CustomStruct(1, "test1"));
        TestRemove(mList4, mTestClass2);
        Profiler.EndSample();

        LogList(mList1);
        LogList(mList2);
        LogList(mList3);
        LogList(mList4);

        Profiler.BeginSample("TestList.TestClear");
        TestClear(mList1);
        TestClear(mList2);
        TestClear(mList3);
        TestClear(mList4);
        Profiler.EndSample();

        LogList(mList1);
        LogList(mList2);
        LogList(mList3);
        LogList(mList4);

        Profiler.BeginSample("TestList.TestHugeAdd");
        for (int i = 0; i < 999; i++)
        {
            TestAdd(mList1, i);
            TestAdd(mList2, "TestHuge");
            TestAdd(mList3, new CustomStruct(i, "TestHuge"));
            TestAdd(mList4, mTestClass1);
        }
        Profiler.EndSample();

        Profiler.BeginSample("TestList.TestHugeAdd&Remove");
        for (int i = 0; i < 999; i++)
        {
            TestAdd(mList1, i);
            TestAdd(mList2, "TestHuge");
            TestAdd(mList3, new CustomStruct(i, "TestHuge"));
            TestAdd(mList4, mTestClass1);
            mList1.RemoveAt(mList1.Count - 1);
            mList2.RemoveAt(mList2.Count - 1);
            mList3.RemoveAt(mList3.Count - 1);
            mList4.RemoveAt(mList4.Count - 1);
        }
        Profiler.EndSample();

        Profiler.BeginSample("TestList.TestHugeInsertHead");
        mList1.Insert(0, 0);
        mList2.Insert(0, "0");
        mList3.Insert(0, new CustomStruct(0, "0"));
        mList4.Insert(0, mTestClass1);
        Profiler.EndSample();

        Profiler.BeginSample("TestList.TestHugeSort");
        mList1.Sort();
        mList2.Sort();
        mList3.Sort();
        mList4.Sort();
        Profiler.EndSample();

        Profiler.BeginSample("TestList.TestHugeRemoveHead");
        mList1.RemoveAt(0);
        mList2.RemoveAt(0);
        mList3.RemoveAt(0);
        mList4.RemoveAt(0);
        Profiler.EndSample();

        Profiler.BeginSample("TestList.TestHugeRemoveTail");
        mList1.RemoveAt(mList1.Count - 1);
        mList2.RemoveAt(mList2.Count - 1);
        mList3.RemoveAt(mList3.Count - 1);
        mList4.RemoveAt(mList4.Count - 1);
        Profiler.EndSample();

        Profiler.BeginSample("TestList.TestHugeClear");
        TestClear(mList1);
        TestClear(mList2);
        TestClear(mList3);
        TestClear(mList4);
        Profiler.EndSample();
    }

    void TestAdd<T>(List<T> list, T item)
    {
        list.Add(item);
    }

    void TestSort<T>(List<T> list, Comparison<T> sortFunc)
    {
        list.Sort(sortFunc);
    }

    void TestGetEnumerator<T>(List<T> list)
    {
        var item = list.GetEnumerator();
        while (item.MoveNext())
        {
            // 说明：用于正确性测试；测试GC时需要注释
            //Debug.Log("TestGetEnumerator:" + item.Current.ToString());
        }
    }

    void TestForeach<T>(List<T> list)
    {
        foreach (T cur in list)
        {
            // 说明：用于正确性测试；测试GC时需要注释
            //Debug.Log("TestForeach:" + cur.ToString());
        }
    }

    void TestRemove<T>(List<T> list, T item)
    {
        list.Remove(item);
    }

    void TestClear<T>(List<T> list)
    {
        list.Clear();
    }

    void LogList<T>(List<T> list)
    {
        // 说明：用于测试正确性
        mBsb.Clear();
        mBsb.AppendFormat("---------------------  {0}", typeof(T).Name);
        mBsb.AppendLine();
        mBsb.AppendFormat("size = {0}", list.Count);
        mBsb.AppendLine();
        for (int i = 0; i < list.Count; i++)
        {
            mBsb.Append(list[i].ToString());
            mBsb.AppendLine();
        }
        mBsb.AppendLine();
        Debug.Log(mBsb.ToString());
    }

    int TestSort<T>(T x, T y) where T : IComparable<T>
    {
        return x.CompareTo(y);
    }
}
