using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;

/// <summary>
/// 说明：MemoryStream测试脚本
/// 
/// 结论：数据正确、无GC；MemoryStream可以重复利用
/// 
/// @by wsh 2017-07-03
/// </summary>

public class TestMemoryStream : MonoBehaviour
{
    MemoryStream msTest;
    BinaryWriter brTestWriter;
    BinaryReader brTestReader;

    public ushort nCardID = 0;
    public int vLogicPosx = 1;
    public int vLogicPosy = 2;
    public int vLogicPosz = 3;

    // Use this for initialization
    void Start () {
        msTest = new MemoryStream();
        brTestReader = new BinaryReader(msTest);
        brTestWriter = new BinaryWriter(msTest);
    }
	
	// Update is called once per frame
	void Update ()
    {
        Profiler.BeginSample("TestWriter");
        TestWriter();
        Profiler.EndSample();
        Debug.Log(msTest.Position);
        Profiler.BeginSample("TestReader");
        TestReader();
        Profiler.EndSample();
        IncreaseData();
        PrintData();
        byte[] bytes = msTest.GetBuffer();
        Debug.Log(msTest.Length);
    }

    void TestWriter()
    {
        msTest.Seek(0, SeekOrigin.Begin);
        brTestWriter.Write(nCardID);
        brTestWriter.Write(vLogicPosx);
        brTestWriter.Write(vLogicPosy);
        brTestWriter.Write(vLogicPosz);
    }

    void TestReader()
    {
        msTest.Seek(0, SeekOrigin.Begin);
        nCardID = brTestReader.ReadUInt16();
        vLogicPosx = brTestReader.ReadInt32();
        vLogicPosy = brTestReader.ReadInt32();
        vLogicPosz = brTestReader.ReadInt32();
    }

    void PrintData()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendFormat("nCardID = {0}, vLogicPos = ({1}, {2}, {3})", nCardID, vLogicPosx, vLogicPosy, vLogicPosz);
        Debug.Log(sb.ToString());
    }

    void IncreaseData()
    {
        nCardID++;
        vLogicPosx++;
        vLogicPosy++;
        vLogicPosz++;
    }
}
