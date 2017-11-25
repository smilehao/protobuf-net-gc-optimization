using UnityEngine;
using System.Collections;
using System;
using CustomDataStruct;
using System.Collections.Generic;

/// <summary>
/// 说明：BetterLinkedList测试脚本
/// 
/// 结论：
///     1）无GC产生
///     2）对于自定义结构，必须继承IEquatable<T>接口，否则Roemove、Cotains、Find、FindLast每次都有GC产生
/// 
/// @by wsh 2017-06-16
/// </summary>

public class TestBetterLinkedList : MonoBehaviour
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

        public CustomClass(int a, string b, CustomStruct c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }

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

    BetterLinkedList<int> mBetterLinkedList1;
    BetterLinkedList<string> mBetterLinkedList2;
    BetterLinkedList<CustomStruct> mBetterLinkedList3;
    BetterLinkedList<CustomClass> mBetterLinkedList4;

    // 测试数据缓存，以免造成干扰
    List<string> mTestString = new List<string>();
    List<CustomStruct> mTestCustomStruct = new List<CustomStruct>();
    List<CustomClass> mTestCustomClass = new List<CustomClass>();
    BetterStringBuilder mBsb;

    // Use this for initialization
    void Start()
    {
        mBetterLinkedList1 = new BetterLinkedList<int>();
        mBetterLinkedList2 = new BetterLinkedList<string>();
        mBetterLinkedList3 = new BetterLinkedList<CustomStruct>();
        mBetterLinkedList4 = new BetterLinkedList<CustomClass>();
        mBsb = new BetterStringBuilder(30, 0, BetterStringBuilderBufferType.None);
        for (int i = 0; i < 100; i++)
        {
            mTestString.Add("test" + i.ToString());
            mTestCustomStruct.Add(new CustomStruct(i, mTestString[i]));
            mTestCustomClass.Add(new CustomClass(i, mTestString[i], mTestCustomStruct[i]));
        }
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("TestBetterLinkedList.TestAddFirst\n");
        Profiler.BeginSample("TestBetterLinkedList.TestAddFirst");
        for (int i = 0; i < 10; i++)
        {
            TestAddFirst(mBetterLinkedList1, i);
            TestAddFirst(mBetterLinkedList2, mTestString[i]);
            TestAddFirst(mBetterLinkedList3, mTestCustomStruct[i]);
            TestAddFirst(mBetterLinkedList4, mTestCustomClass[i]);
        }
        Profiler.EndSample();
        LogList();

        Debug.Log("\n*********************************\n");
        Debug.Log("TestBetterLinkedList.TestRemoveFirst\n");
        Profiler.BeginSample("TestBetterLinkedList.TestRemoveFirst");
        TestRemoveFirst(mBetterLinkedList1);
        TestRemoveFirst(mBetterLinkedList2);
        TestRemoveFirst(mBetterLinkedList3);
        TestRemoveFirst(mBetterLinkedList4);
        Profiler.EndSample();
        LogList();

        Debug.Log("\n*********************************\n");
        Debug.Log("TestBetterLinkedList.TestRemoveLast\n");
        Profiler.BeginSample("TestBetterLinkedList.TestRemoveLast");
        TestRemoveLast(mBetterLinkedList1);
        TestRemoveLast(mBetterLinkedList2);
        TestRemoveLast(mBetterLinkedList3);
        TestRemoveLast(mBetterLinkedList4);
        Profiler.EndSample();
        LogList();

        Debug.Log("\n*********************************\n");
        Debug.Log("TestBetterLinkedList.TestRemoveValue<4>\n");
        Profiler.BeginSample("TestBetterLinkedList.TestRemoveValue<4>");
        TestRemoveValue(mBetterLinkedList1, 4);
        TestRemoveValue(mBetterLinkedList2, mTestString[4]);
        TestRemoveValue(mBetterLinkedList3, mTestCustomStruct[4]);
        TestRemoveValue(mBetterLinkedList4, mTestCustomClass[4]);
        Profiler.EndSample();
        LogList();

        Debug.Log("\n*********************************\n");
        Debug.Log("TestBetterLinkedList.TestFind <4>\n");
        Profiler.BeginSample("TestBetterLinkedList.TestFind <4>");
        BetterLinkedListNode<int> nodeInt = TestFind(mBetterLinkedList1, 4);
        BetterLinkedListNode<string> nodeString = TestFind(mBetterLinkedList2, mTestString[4]);
        BetterLinkedListNode<CustomStruct> nodeStruct = TestFind(mBetterLinkedList3, mTestCustomStruct[4]);
        BetterLinkedListNode<CustomClass> nodeClass = TestFind(mBetterLinkedList4, mTestCustomClass[4]);
        Profiler.EndSample();

        Debug.Log("nodeInt : " + nodeInt + "\n");
        Debug.Log("nodeString : " + nodeString + "\n");
        Debug.Log("nodeStruct : " + nodeStruct + "\n");
        Debug.Log("nodeClass : " + nodeClass + "\n");

        Debug.Log("\n*********************************\n");
        Debug.Log("TestBetterLinkedList.TestFind <6>\n");
        Profiler.BeginSample("TestBetterLinkedList.TestFind <6>");
        nodeInt = TestFind(mBetterLinkedList1, 6);
        nodeString = TestFind(mBetterLinkedList2, mTestString[6]);
        nodeStruct = TestFind(mBetterLinkedList3, mTestCustomStruct[6]);
        nodeClass = TestFind(mBetterLinkedList4, mTestCustomClass[6]);
        Profiler.EndSample();

        Debug.Log("nodeInt : " + nodeInt.Value + "\n");
        Debug.Log("nodeString : " + nodeString.Value + "\n");
        Debug.Log("nodeStruct : " + nodeStruct.Value + "\n");
        Debug.Log("nodeClass : " + nodeClass.Value + "\n");

        Debug.Log("\n*********************************\n");
        Debug.Log("TestBetterLinkedList.TestAddBefore\n");
        Profiler.BeginSample("TestBetterLinkedList.TestAddBefore");
        TestAddBefore(mBetterLinkedList1, nodeInt, 99);
        TestAddBefore(mBetterLinkedList2, nodeString, mTestString[99]);
        TestAddBefore(mBetterLinkedList3, nodeStruct, mTestCustomStruct[99]);
        TestAddBefore(mBetterLinkedList4, nodeClass, mTestCustomClass[99]);
        Profiler.EndSample();
        LogList();

        Debug.Log("\n*********************************\n");
        Debug.Log("TestBetterLinkedList.TestAddAfter\n");
        Profiler.BeginSample("TestBetterLinkedList.TestAddAfter");
        TestAddAfter(mBetterLinkedList1, nodeInt, 99);
        TestAddAfter(mBetterLinkedList2, nodeString, mTestString[99]);
        TestAddAfter(mBetterLinkedList3, nodeStruct, mTestCustomStruct[99]);
        TestAddAfter(mBetterLinkedList4, nodeClass, mTestCustomClass[99]);
        Profiler.EndSample();
        LogList();

        Debug.Log("\n*********************************\n");
        Debug.Log("TestBetterLinkedList.TestRemoveNode\n");
        Profiler.BeginSample("TestBetterLinkedList.TestRemoveNode");
        TestRemoveNode(mBetterLinkedList1, nodeInt);
        TestRemoveNode(mBetterLinkedList2, nodeString);
        TestRemoveNode(mBetterLinkedList3, nodeStruct);
        TestRemoveNode(mBetterLinkedList4, nodeClass);
        Profiler.EndSample();
        LogList();

        Debug.Log("\n*********************************\n");
        Debug.Log("TestBetterLinkedList.TestAddLast\n");
        Profiler.BeginSample("TestBetterLinkedList.TestAddLast");
        TestAddLast(mBetterLinkedList1, 98);
        TestAddLast(mBetterLinkedList2, mTestString[98]);
        TestAddLast(mBetterLinkedList3, mTestCustomStruct[98]);
        TestAddLast(mBetterLinkedList4, mTestCustomClass[98]);
        Profiler.EndSample();
        LogList();

        Debug.Log("\n*********************************\n");
        Debug.Log("TestBetterLinkedList.TestGetEnumerator\n");
        Profiler.BeginSample("TestBetterLinkedList.TestGetEnumerator");
        TestGetEnumerator(mBetterLinkedList1);
        TestGetEnumerator(mBetterLinkedList2);
        TestGetEnumerator(mBetterLinkedList3);
        TestGetEnumerator(mBetterLinkedList4);
        Profiler.EndSample();

        Debug.Log("\n*********************************\n");
        Debug.Log("TestBetterLinkedList.TestForeach\n");
        Profiler.BeginSample("TestBetterLinkedList.TestForeach");
        TestForeach(mBetterLinkedList1);
        TestForeach(mBetterLinkedList2);
        TestForeach(mBetterLinkedList3);
        TestForeach(mBetterLinkedList4);
        Profiler.EndSample();

        Debug.Log("\n*********************************\n");
        Debug.Log("TestBetterLinkedList.TestClear\n");
        Profiler.BeginSample("TestBetterLinkedList.TestClear");
        TestClear(mBetterLinkedList1);
        TestClear(mBetterLinkedList2);
        TestClear(mBetterLinkedList3);
        TestClear(mBetterLinkedList4);
        Profiler.EndSample();
        LogList();

        Debug.Log("\n*********************************\n");
        Debug.Log("TestBetterLinkedList.TestAddLast\n");
        Profiler.BeginSample("TestBetterLinkedList.TestAddLast");
        for (int i = 50; i < 60; i++)
        {
            TestAddLast(mBetterLinkedList1, i);
            TestAddLast(mBetterLinkedList2, mTestString[i]);
            TestAddLast(mBetterLinkedList3, mTestCustomStruct[i]);
            TestAddLast(mBetterLinkedList4, mTestCustomClass[i]);
        }
        Profiler.EndSample();
        LogList();

        TestClear(mBetterLinkedList1);
        TestClear(mBetterLinkedList2);
        TestClear(mBetterLinkedList3);
        TestClear(mBetterLinkedList4);
        Debug.Log("\n*********************************\n");
        Debug.Log("TestBetterLinkedList.TestHugeAddFirst\n");
        Profiler.BeginSample("TestList.TestHugeAddFirst");
        for (int i = 0; i < 999; i++)
        {
            TestAddFirst(mBetterLinkedList1, i % 100);
            TestAddFirst(mBetterLinkedList2, mTestString[i % 100]);
            TestAddFirst(mBetterLinkedList3, mTestCustomStruct[i % 100]);
            TestAddFirst(mBetterLinkedList4, mTestCustomClass[i % 100]);
        }
        Profiler.EndSample();
        Debug.Log("mBetterLinkedList1 Count = " + mBetterLinkedList1.Count);
        Debug.Log("mBetterLinkedList2 Count = " + mBetterLinkedList2.Count);
        Debug.Log("mBetterLinkedList3 Count = " + mBetterLinkedList3.Count);
        Debug.Log("mBetterLinkedList4 Count = " + mBetterLinkedList4.Count);

        Debug.Log("\n*********************************\n");
        Debug.Log("TestBetterLinkedList.TestHugeClear\n");
        Profiler.BeginSample("TestList.TestHugeClear");
        TestClear(mBetterLinkedList1);
        TestClear(mBetterLinkedList2);
        TestClear(mBetterLinkedList3);
        TestClear(mBetterLinkedList4);
        Profiler.EndSample();
        LogList();
    }
    
    void LogList()
    {
        LogList(mBetterLinkedList1);
        LogList(mBetterLinkedList2);
        LogList(mBetterLinkedList3);
        LogList(mBetterLinkedList4);
    }

    void TestAddFirst<T>(BetterLinkedList<T> list, T item)
    {
        list.AddFirst(item);
    }

    void TestAddLast<T>(BetterLinkedList<T> list, T item)
    {
        list.AddLast(item);
    }

    void TestAddBefore<T>(BetterLinkedList<T> list, BetterLinkedListNode<T> node, T item)
    {
        list.AddBefore(node, item);
    }

    void TestAddBefore<T>(BetterLinkedList<T> list, BetterLinkedListNode<T> node, BetterLinkedListNode<T> newNode)
    {
        list.AddBefore(node, newNode);
    }

    void TestAddAfter<T>(BetterLinkedList<T> list, BetterLinkedListNode<T> node, T item)
    {
        list.AddAfter(node, item);
    }

    void TestAddAfter<T>(BetterLinkedList<T> list, BetterLinkedListNode<T> node, BetterLinkedListNode<T> newNode)
    {
        list.AddAfter(node, newNode);
    }
    
    void TestGetEnumerator<T>(BetterLinkedList<T> list)
    {
        var item = list.GetEnumerator();
        while (item.MoveNext())
        {
            // 说明：用于正确性测试；测试GC时需要注释
            //Debug.Log("TestGetEnumerator:" + (item.Current as BetterLinkedListNode<T>).Value.ToString());
        }
    }

    void TestForeach<T>(BetterLinkedList<T> list)
    {
        foreach (BetterLinkedListNode<T> cur in list)
        {
            // 说明：用于正确性测试；测试GC时需要注释
            //Debug.Log("TestForeach:" + cur.Value.ToString());
        }
    }

    void TestRemoveFirst<T>(BetterLinkedList<T> list)
    {
        list.RemoveFirst();
    }

    void TestRemoveLast<T>(BetterLinkedList<T> list)
    {
        list.RemoveLast();
    }

    void TestRemoveValue<T>(BetterLinkedList<T> list, T value)
    {
        list.Remove(value);
    }

    void TestRemoveNode<T>(BetterLinkedList<T> list, BetterLinkedListNode<T> node)
    {
        list.Remove(node);
    }

    BetterLinkedListNode<T> TestFind<T>(BetterLinkedList<T> list, T value)
    {
        BetterLinkedListNode<T> node = list.Find(value);
        return node;
    }

    void TestClear<T>(BetterLinkedList<T> list)
    {
        list.Clear();
    }

    void LogList<T>(BetterLinkedList<T> list)
    {
        // 说明：用于测试正确性
        mBsb.Clear();
        mBsb.AppendFormat("---------------------  {0}", typeof(T).Name);
        mBsb.AppendLine();
        mBsb.AppendFormat("Count = {0}", list.Count);
        mBsb.AppendLine();
        BetterLinkedListNode<T> node = list.First;
        while (node != null)
        {
            mBsb.Append(node.Value.ToString());
            mBsb.AppendLine();
            node = node.Next;
        }
        mBsb.AppendLine();
        Debug.Log(mBsb.ToString());
    }
}
