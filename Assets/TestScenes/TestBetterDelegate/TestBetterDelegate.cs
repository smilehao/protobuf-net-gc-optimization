using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using CustomDataStruct;
using System.Threading;

/// <summary>
/// 说明：对BetterDelegate正确性、性能、GC方面的测试
/// 
/// 结论： 
///     1）完全没有GC产生
///     2）与旧代码相比效率稍高，与直接缓存委托去回调相比慢5倍
/// 
/// by wsh @ 2017-06-19
/// </summary>

public class TestBetterDelegate : MonoBehaviour
{
    // 用于便捷切换正确性和性能测试的开关
    bool isTestingCorrectness = false;

    List<string> inputNames = new List<string>();
    List<string> outputNames = new List<string>();
    DelegateCallback<object, int, string, int> mDel1;
    DelegateCallback<object, int, string, object, int, string> mDel2;
    DelegateCallback<GameObject, string, CallbackInfo, string> mDel3;
    
    TestCallClass testClass = new TestCallClass();
    int callNum = 0;

    Thread mThread;
    bool threadStart;
    Queue<IDelegateAction> invokeQueue = new Queue<IDelegateAction>();

    // Use this for initialization
    void Start()
    {
        for (int i = 0; i < 10000; i++)
        {
            inputNames.Add("Input-" + i.ToString());
            outputNames.Add("Output-" + i.ToString());
        }

        mDel1 = TestCallback;
        mDel2 = TestCallback;
        mDel3 = OnModelLoaded;

        testClass.go = gameObject;
        callNum = 0;

        threadStart = false;
        mThread = new Thread(ThreadUpdate);
        mThread.Start();
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("===================BetterDelegate Callback===================");
        Profiler.BeginSample("BetterDelegate Callback");
        for (int i = 0; i < (isTestingCorrectness ? 10 : 10000); i++)
        {
            if (i % 3 == 0)
            {
                IDelegateAction bDelegate = BetterDelegate.GetAction(mDel1, inputNames[i], i);
                bDelegate.Invoke((object)outputNames[i], 1111);
            }
            else
            {
                IDelegateAction bDelegate = BetterDelegate.GetAction(mDel2, (object)inputNames[i], i, inputNames[i]);
                bDelegate.Invoke((object)outputNames[i], 2222, outputNames[i]);
            }
        }
        Profiler.EndSample();

        Debug.Log("===================Directly Call===================");
        Profiler.BeginSample("Directly Call");
        for (int i = 0; i < (isTestingCorrectness ? 10 : 10000); i++)
        {
            if (i % 3 == 0)
            {
                TestCallback(outputNames[i], 1111, inputNames[i], i);
            }
            else
            {
                TestCallback(outputNames[i], 2222, outputNames[i], inputNames[i], i, inputNames[i]);
            }
        }
        Profiler.EndSample();

        Debug.Log("===================Cache Delegate Genericity Callback===================");
        Profiler.BeginSample("Cache Delegate Genericity Callback");
        for (int i = 0; i < (isTestingCorrectness ? 10 : 10000); i++)
        {
            if (i % 3 == 0)
            {
                TestDe(mDel1, outputNames[i], 1111, inputNames[i], i);
            }
            else
            {
                TestDe(mDel2, outputNames[i], 2222, outputNames[i], inputNames[i], i, inputNames[i]);
            }
        }
        Profiler.EndSample();

        Debug.Log("===================Cache Delegate Function Callback===================");
        Profiler.BeginSample("Cache Delegate Function Callback");
        for (int i = 0; i < (isTestingCorrectness ? 10 : 10000); i++)
        {
            if (i % 3 == 0)
            {
                TestDeCall(mDel1, outputNames[i], 1111, inputNames[i], i);
            }
            else
            {
                TestDeCall(mDel2, outputNames[i], 2222, outputNames[i], inputNames[i], i, inputNames[i]);
            }
        }
        Profiler.EndSample();

        Debug.Log("===================No Cache Delegate Genericity Callback===================");
        Profiler.BeginSample("No Cache Delegate Genericity Callback");
        for (int i = 0; i < (isTestingCorrectness ? 10 : 10000); i++)
        {
            if (i % 3 == 0)
            {
                TestDe(TestCallback, outputNames[i], 1111, inputNames[i], i);
            }
            else
            {
                TestDe(TestCallback, outputNames[i], 2222, outputNames[i], inputNames[i], i, inputNames[i]);
            }
        }
        Profiler.EndSample();

        if (callNum < 10)
        {
            callNum++;
            Debug.Log("===================Test async conrrectness===================");
            CallbackInfo callbackInfo = default(CallbackInfo);
            callbackInfo.forward = new Vector3(callNum, callNum, callNum);
            IDelegateAction btDelegate = BetterDelegate.GetAction(mDel3, callbackInfo, "inTestString");
            testClass.InvokeDelegate(btDelegate);
            // 用协程模拟异步
            StartCoroutine("InvokeTrigger");
        }
        else if (callNum < 20)
        {
            callNum++;
            Debug.Log("===================Test thread conrrectness===================");
            CallbackInfo callbackInfo = default(CallbackInfo);
            callbackInfo.forward = new Vector3(callNum, callNum, callNum);
            IDelegateAction btDelegate = BetterDelegate.GetAction(mDel3, callbackInfo, "inTestString");
            invokeQueue.Enqueue(btDelegate);
        }
        else
        {
            // 启动子线程，测试回调
            threadStart = true;
        }
    }

    void ThreadUpdate()
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        for (;;)
        {
            if (threadStart == true)
            {
                if (invokeQueue.Count > 0)
                {
                    invokeQueue.Dequeue().Invoke((GameObject)null, "thread outTestString");
                }
            }
            Thread.Sleep(1);
        }
    }

    public void OnDestory()
    {
        if (mThread != null)
        {
            mThread.Abort();
            while (mThread.IsAlive) Thread.Sleep(1);
            mThread = null;
        }
    }

    IEnumerator InvokeTrigger()
    {
        yield return new WaitForSeconds(0.5f);
        testClass.DoInvoke();
        yield break;
    }

    private void TestDe<J, K, T, U>(DelegateCallback<J, K, T, U> del, J j, K k, T t, U u)
    {
        del.Invoke(j, k, t, u);
    }

    private void TestDe<J, K, M, T, U, V>(DelegateCallback<J, K, M, T, U, V> del, J j, K k, M m, T t, U u, V v)
    {
        del.Invoke(j, k, m, t, u, v);
    }

    private void TestDeCall(DelegateCallback<object, int, string, int> del, object j, int k, string t, int u)
    {
        del.Invoke(j, k, t, u);
    }

    private void TestDeCall(DelegateCallback<object, int, string, object, int, string> del,
        object j, int k, string m, object t, int u, string v)
    {
        del.Invoke(j, k, m, t, u, v);
    }

    private void TestCallback(object j, int k, string t, int u)
    {
        if (isTestingCorrectness)
        {
            // 用于正确性验证，注意，循环次数调为10即可，否则编辑器会死掉
            Debug.Log(string.Format("j = {0}, k = {1}, t = {2}, u = {3}",
                j, k, t, u));
        }
    }

    private void TestCallback(object j, int k, string m, object t, int u, string v)
    {
        if (isTestingCorrectness)
        {
            // 用于正确性验证，注意，循环次数调为10即可，否则编辑器会死掉
            Debug.Log(string.Format("j = {0}, k = {1}, m = {2}, t = {3}, u = {4}, v = {5}",
                j, k, m, t, u, v));
        }
    }

    private void OnModelLoaded(GameObject prefab, string resID, CallbackInfo callbackInfo, string path)
    {
        if (isTestingCorrectness)
        {
            // 用于正确性验证，注意，循环次数调为10即可，否则编辑器会死掉
            Debug.Log(string.Format("prefab = {0}, resID = {1}, callbackInfo = {2}, path = {3}",
                prefab, resID, callbackInfo.forward, path));
        }
    }


    public struct CallbackInfo
    {
        public IDelegateAction callback;
        public Vector3 pos;
        public Vector3 forward;
        public GameObject parent;

        public CallbackInfo(IDelegateAction callback, Vector3 pos, Vector3 forward, GameObject parent)
        {
            this.callback = callback;
            this.pos = pos;
            this.forward = forward;
            this.parent = parent;
        }
    }
}


public class TestCallClass
{
    public GameObject go;

    Queue<IDelegateAction> invokeQueue = new Queue<IDelegateAction>();

    public void InvokeDelegate(IDelegateAction delFun)
    {
        invokeQueue.Enqueue(delFun);
    }

    public void DoInvoke()
    {
        invokeQueue.Dequeue().Invoke(go, "Async outTestString");
    }
}