using UnityEngine;
using CustomDataStruct;
using System.Collections.Generic;

/// <summary>
/// 说明：ValueObject测试
/// 
/// 结论：
///     1）无GC产生
///     2）结构体不要求实现IEquatable<T>，IComparable<T>接口
///     3）相比于object装箱，效率稍低：2到5倍左右，浮动不定
/// 
/// by wsh @ 2017-06-30
/// </summary>

public class TestValueObject : MonoBehaviour
{
    // 切换正确性和性能测试
    bool isTestingCorrectness = false;
    
    public struct CustomStruct /*: IEquatable<CustomStruct>, IComparable<CustomStruct>*/
    {
        public int a;
        public string b;

        public CustomStruct(int a, string b)
        {
            this.a = a;
            this.b = b;
        }

        //public bool Equals(CustomStruct other)
        //{
        //    return a == other.a && b == other.b;
        //}

        //public int CompareTo(CustomStruct other)
        //{
        //    if (a != other.a)
        //    {
        //        return a.CompareTo(other.a);
        //    }

        //    if (b != other.b)
        //    {
        //        return b.CompareTo(other.b);
        //    }

        //    return 0;
        //}
        
        // 说明：测试正确性用的，不是必须
        public override string ToString()
        {
            return string.Format("<a = {0}, b = {1}>", a, b);
        }
    }

    List<int> mTestIntList = new List<int>();
    List<CustomStruct> mTestStructList = new List<CustomStruct>();

    // Use this for initialization
    void Start()
    {
        for ( int i = 0; i < 10000; i++)
        {
            mTestIntList.Add(i);
            mTestStructList.Add(new CustomStruct(i, i.ToString()));
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < (isTestingCorrectness ? 10 : 10000); i++)
        {
            Profiler.BeginSample("ValueObject<int> Using");
            TestUsing(mTestIntList[i]);
            Profiler.EndSample();

            Profiler.BeginSample("ValueObject<int> Dispose");
            TestDispose(mTestIntList[i]);
            Profiler.EndSample();

            Profiler.BeginSample("ValueObject<int> Static");
            TestStatic(mTestIntList[i]);
            Profiler.EndSample();

            Profiler.BeginSample("Object<int> Boxing");
            TestBoxing(mTestIntList[i]);
            Profiler.EndSample();

            Profiler.BeginSample("ValueObject<int> Return");
            TestReturn(mTestIntList[i]);
            Profiler.EndSample();

            Profiler.BeginSample("ValueObject<int> ToObject");
            TestToObject(mTestIntList[i]);
            Profiler.EndSample();
        }

        for (int i = 0; i < (isTestingCorrectness ? 10 : 10000); i++)
        {
            Profiler.BeginSample("ValueObject<struct> Using");
            TestUsing(mTestStructList[i]);
            Profiler.EndSample();

            Profiler.BeginSample("ValueObject<struct> Dispose");
            TestDispose(mTestStructList[i]);
            Profiler.EndSample();

            Profiler.BeginSample("ValueObject<struct> Static");
            TestStatic(mTestStructList[i]);
            Profiler.EndSample();

            Profiler.BeginSample("Object<struct> Boxing");
            TestBoxing(mTestStructList[i]);
            Profiler.EndSample();

            Profiler.BeginSample("ValueObject<struct> Return");
            TestReturn(mTestStructList[i]);
            Profiler.EndSample();

            Profiler.BeginSample("ValueObject<struct> ToObject");
            TestToObject(mTestStructList[i]);
            Profiler.EndSample();
        }

        // 用于测试内存泄露检测功能
        for (int i = 0; i < 100; i++)
        {
            //ValueObject.Get(i);
        }

        // 用于异常测试
        // 结论：即使发生异常也不会内存泄漏
        //ValueObject valObj = ValueObject.Get(10);
        //uint uintValue = ValueObject.Value<uint>(valObj);
        //string strValue = ValueObject.Value<string>(valObj);
    }

    private void TestUsing<T>(T value)
    {
        using (ValueObject obj = ValueObject.Get(value))
        {
            ValueObjectFun<T>(obj);
        }
    }

    private void TestDispose<T>(T value)
    {
        ValueObject obj = ValueObject.Get(value);
        ValueObjectFun<T>(obj);
        obj.Dispose();
    }

    private void TestStatic<T>(T value)
    {
        ValueObject obj = ValueObject.Get(value);
        StaticValueObjectFun<T>(obj);
    }

    private void TestBoxing<T>(T value)
    {
        ObjectFun<T>(value);
    }

    private void TestReturn<T>(T value)
    {
        //**********推荐使用方式：使用object传参，函数体不用改**********
        //1）在函数调用前对值类型形参使用ValueObject.Get手动装箱
        //2）在函数调用后对值类型实参使用ValueObject.Value<T>手动拆箱
        ValueObject obj = GetValue(value);
        StaticValueObjectFun<T>(obj);
    }
    
    private void TestToObject<T>(T value)
    {
        ValueObject obj = ValueObject.Get(value);
        object objVal = ValueObject.ToObject(obj);
        if (isTestingCorrectness) Debug.Log(objVal.ToString());
    }


    private ValueObject GetValue<T>(T value)
    {
        return ValueObject.Get(value);
    }
    
    private void ValueObjectFun<T>(ValueObject arg)
    {
        T value = arg.Value<T>();
        if (isTestingCorrectness) Debug.Log(value.ToString());
    }

    private void StaticValueObjectFun<T>(object arg)
    {
        T value = ValueObject.Value<T>(arg);
        if (isTestingCorrectness) Debug.Log(value.ToString());
    }

    private void ObjectFun<T>(object arg)
    {
        T value = (T)arg;
        if (isTestingCorrectness) Debug.Log(value.ToString());
    }
}
