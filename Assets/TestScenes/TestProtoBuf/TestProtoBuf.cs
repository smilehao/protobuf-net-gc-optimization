using UnityEngine;
using battle;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf.Serializers;
using CustomDataStruct;

/// <summary>
/// 说明：protobuf测试脚本，开启deep profile分析
/// 
/// 数据：Test3使用第3套API
/// 
///     优化前：    序列化：        GC 0.7k  0.21ms
///                 反序列化：      GC 1.3k  0.23ms
/// 
///     优化后：    序列化：        GC 80B  0.23ms（两次List枚举器的获取，每次40B）
///                 反序列化：      GC 0B   0.20ms
/// 
/// 结论：
///     1）
/// 
/// @by wsh 2017-06-26
/// </summary>

public class TestProtoBuf : MonoBehaviour
{
    const int DATA_BYTE_LENGTH = 40;//假设一个字段4个字节，共10个字段，已经远远超过游戏实际情况了
    //const int DATA_BYTE_LENGTH = 4000;//大数据量测试

    const int SENF_BUFFER_LEN = 64 * 1024;
    const int REVIVE_BUFFER_LEN = 128 * 1024;

    ntf_battle_frame_data data = new ntf_battle_frame_data();
    ntf_battle_frame_data data2 = new ntf_battle_frame_data();
    req_enter_room data3 = new req_enter_room();// 为了测试List<int>
    req_enter_room data4 = new req_enter_room();
    byte[] sendBuffer = new byte[SENF_BUFFER_LEN];
    byte[] reciveBuffer = new byte[REVIVE_BUFFER_LEN];
    MemoryStream msSend;
    MemoryStream msRecive;
    BinaryWriter bwWriter;
    BinaryReader bwReader;
    ProtoBuf.ProtoWriter pbWriter;
    ProtoBuf.ProtoReader pbReader;
    ProtoBuf.ProtoWriter pbWriter2;
    ProtoBuf.ProtoReader pbReader2;

    // Use this for initialization
    void Start () {
        InitData(data);
        InitData(data2);
        InitData(data3);
        InitData(data4);

        msSend = new MemoryStream(sendBuffer, 0, SENF_BUFFER_LEN, true, true);
        msRecive = new MemoryStream(reciveBuffer, 0, REVIVE_BUFFER_LEN, true, true);
        bwWriter = new BinaryWriter(msSend);
        bwReader = new BinaryReader(msRecive);
        // ********只是为了测试，外部最好不要直接使用ProtoWriter和ProtoReader
        pbWriter = new ProtoBuf.ProtoWriter(msSend, ProtoBuf.Meta.RuntimeTypeModel.Default, null);
        pbReader = new ProtoBuf.ProtoReader(msRecive, ProtoBuf.Meta.RuntimeTypeModel.Default, null);
        pbWriter2 = new ProtoBuf.ProtoWriter(msSend, ProtoBuf.Meta.RuntimeTypeModel.Default, null);
        pbReader2 = new ProtoBuf.ProtoReader(msRecive, ProtoBuf.Meta.RuntimeTypeModel.Default, null);
    }

	// Update is called once per frame
	void Update ()
    {
        // ==============旧代码使用方式==============
        //Test1();

        // ==============MemoryStream缓存==============
        //Test2();

        // ==============双MemoryStream缓存==============
        //Test3();

        // ==============对List<int>的测试==============
        //Test4();

        // ==============对ProtoBufSerializer的测试==============
        Test5();
    }

    void Test1()
    {
        MemoryStream oldms = new MemoryStream();
        ProtoBuf.Serializer.Serialize(oldms, data);
        byte[] newbyte = oldms.ToArray();
        MemoryStream newms = new MemoryStream(newbyte);
        ntf_battle_frame_data data2 = ProtoBuf.Serializer.Deserialize<ntf_battle_frame_data>(newms);
        data.server_curr_frame++;
        Debug.Log(data2.server_curr_frame);
    }

    void Test2()
    {
        msSend.SetLength(SENF_BUFFER_LEN);
        msSend.Seek(0, SeekOrigin.Begin);
        ProtoBuf.Serializer.Serialize(msSend, data);
        // 输出：522，说明Serialize会改变流指针
        //Debug.Log((int)msSend.Position);
        msSend.SetLength(msSend.Position);
        msSend.Seek(0, SeekOrigin.Begin);//指针一定要复位
        ntf_battle_frame_data data2 = ProtoBuf.Serializer.Deserialize<ntf_battle_frame_data>(msSend);
        data.server_curr_frame++;
        Debug.Log(data2.server_curr_frame);
    }

    void Test3()
    {
        msSend.SetLength(SENF_BUFFER_LEN);
        msSend.Seek(0, SeekOrigin.Begin);

        // =============第1套API=============
        // 旧代码：每次调用产生0.9KB垃圾===>垃圾大小和要发送的数据量相关（量大于1KB时缓存失效，大量垃圾产生）
        //ProtoBuf.Serializer.NonGeneric.Serialize(msSend, data);
        //ProtoBuf.Serializer.Serialize(msSend, data);//同样

        // =============第2套API=============
        //ProtoBuf.Serializer.SerializeWithLengthPrefix(msSend, data, ProtoBuf.PrefixStyle.Base128);//同样

        // =============第3套API=============
        // 正确使用方式**************
        // GC小，0.7K，稳定，和数据量大小无关，只和数据域的多少有关，全部为PB内部Reflection产生的GC
        ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(pbWriter, data);

        msSend.SetLength(msSend.Position);//长度一定要设置对
        msSend.Seek(0, SeekOrigin.Begin);//指针一定要复位
        msRecive.SetLength(msSend.Length);//同理
        msRecive.Seek(0, SeekOrigin.Begin);//同理

        //MemoryStream.ToArray() ===> 严重GC！！！！
        //Buffer.BlockCopy(msSend.ToArray(), 0, msRecive.ToArray(), 0, (int)msSend.Position);

        //msSend.GetBuffer()无GC => 推荐 * ******
        Buffer.BlockCopy(msSend.GetBuffer(), 0, msRecive.GetBuffer(), 0, (int)msSend.Length);

        //输出：522、0，流复制不移动指针
        //Debug.Log((int)msSend.Position);
        //Debug.Log((int)msRecive.Position);

        // =============第1套API=============
        // 旧代码：每次调用产生1.4k垃圾===>垃圾大小和接受的数据量有关
        //data2 = (ntf_battle_frame_data)ProtoBuf.Serializer.NonGeneric.Deserialize(typeof(ntf_battle_frame_data), msRecive);
        //data2 = ProtoBuf.Serializer.Deserialize<ntf_battle_frame_data>(msRecive);//同样
        // GC稍微少一点的调用方式：1.2k
        // 必须对最外层List等执行Clear操作，对Array赋值为null，因为Merge会导致List、Array等无限增长，使数据不正确
        // 注意：只需要清理外层就可以了，内层的不需要清理***************
        // 但是：这种使用方式节省的GC很有限，原因在于只是省去了外层数据的Instance，内存的实例PB照样给你New出来
        // 比如下例：slot_list被Clear以后，往里面添加的数据全部需要New出来，所以只是复用了data2这个实例，
        // 它包含的全部其余实例全是New出来的
        //data2.slot_list.Clear();
        //ProtoBuf.Serializer.Merge(msRecive, data2);


        // =============第2套API=============
        //data2 = ProtoBuf.Serializer.DeserializeWithLengthPrefix<ntf_battle_frame_data>(msRecive, ProtoBuf.PrefixStyle.Base128);//同样

        // =============第3套API=============
        // 正确使用方式**************
        // GC1.2K，和ProtoBuf.Serializer.Merge差不多，垃圾大小和数据量有关
        data2.slot_list.Clear();//同理，先清数据
        ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(pbReader, data2, typeof(ntf_battle_frame_data));

        data.server_curr_frame++;
        data.server_to_slot++;
        data.server_from_slot++;
        data.time++;
        data.slot_list[0].slot++;
        data.slot_list[0].cmd_list[0].server_frame ++;
        data.slot_list[0].cmd_list[0].cmd.cmd_id++;
        data.slot_list[0].cmd_list[0].cmd.UID++;
        data.slot_list[0].cmd_list[0].cmd.cmd_data[0]++;
        data.slot_list[0].cmd_list[0].cmd.cmd_data[DATA_BYTE_LENGTH - 1]++;

        PrintData(data2);
    }

    void Test4()
    {
        msSend.SetLength(SENF_BUFFER_LEN);
        msSend.Seek(0, SeekOrigin.Begin);
        
        ProtoBuf.Meta.RuntimeTypeModel.Default.Serialize(pbWriter2, data3);

        msSend.SetLength(msSend.Position);//长度一定要设置对
        msSend.Seek(0, SeekOrigin.Begin);//指针一定要复位
        msRecive.SetLength(msSend.Length);//同理
        msRecive.Seek(0, SeekOrigin.Begin);//同理
        
        Buffer.BlockCopy(msSend.GetBuffer(), 0, msRecive.GetBuffer(), 0, (int)msSend.Length);
        
        data4.card_list.Clear();//同理，先清数据
        ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(pbReader2, data4, typeof(req_enter_room));

        data3.battle_id++;
        for (int i = 0; i < data3.card_list.Count; i++)
        {
            data3.card_list[i]++;
        }

        PrintData(data4);
    }

    void Test5()
    {
        msSend.SetLength(SENF_BUFFER_LEN);
        msSend.Seek(0, SeekOrigin.Begin);

        ntf_battle_frame_data dataTmp = ProtoFactory.Get<ntf_battle_frame_data>();
        ntf_battle_frame_data.one_slot oneSlot = ProtoFactory.Get<ntf_battle_frame_data.one_slot>();
        ntf_battle_frame_data.cmd_with_frame cmdWithFrame = ProtoFactory.Get<ntf_battle_frame_data.cmd_with_frame>();
        one_cmd oneCmd = ProtoFactory.Get<one_cmd>();
        cmdWithFrame.cmd = oneCmd;
        oneSlot.cmd_list.Add(cmdWithFrame);
        dataTmp.slot_list.Add(oneSlot);
        DeepCopyData(data, dataTmp);
        ProtoBufSerializer.Serialize(msSend, dataTmp);
        ProtoFactory.Recycle(dataTmp);//*************回收，很重要

        msSend.SetLength(msSend.Position);//长度一定要设置对
        msSend.Seek(0, SeekOrigin.Begin);//指针一定要复位
        //msRecive.SetLength(msSend.Length);//同理，但是如果Deserialize指定长度，则不需要设置流长度
        msRecive.Seek(0, SeekOrigin.Begin);//同理
        
        Buffer.BlockCopy(msSend.GetBuffer(), 0, msRecive.GetBuffer(), 0, (int)msSend.Length);

        dataTmp = ProtoBufSerializer.Deserialize(msRecive, typeof(ntf_battle_frame_data), (int)msSend.Length) as ntf_battle_frame_data;

        PrintData(dataTmp);
        ProtoFactory.Recycle(dataTmp);//*************回收，很重要

        data.server_curr_frame++;
        data.server_to_slot++;
        data.server_from_slot++;
        data.time++;
        data.slot_list[0].slot++;
        data.slot_list[0].cmd_list[0].server_frame++;
        data.slot_list[0].cmd_list[0].cmd.cmd_id++;
        data.slot_list[0].cmd_list[0].cmd.UID++;
        data.slot_list[0].cmd_list[0].cmd.cmd_data[0]++;
        data.slot_list[0].cmd_list[0].cmd.cmd_data[DATA_BYTE_LENGTH - 1]++;
    }

    void InitData(ntf_battle_frame_data data)
    {
        ntf_battle_frame_data.one_slot oneSlot;
        ntf_battle_frame_data.cmd_with_frame cmdWithFrame;
        one_cmd oneCmd;
        oneCmd = new one_cmd();
        oneCmd.UID = 1;
        oneCmd.cmd_id = 1;
        oneCmd.cmd_data = new byte[DATA_BYTE_LENGTH];
        cmdWithFrame = new ntf_battle_frame_data.cmd_with_frame();
        cmdWithFrame.server_frame = 1;
        cmdWithFrame.cmd = oneCmd;
        oneSlot = new ntf_battle_frame_data.one_slot();
        oneSlot.slot = 1;
        oneSlot.cmd_list.Add(cmdWithFrame);
        data.server_curr_frame = 1;
        data.server_from_slot = 1;
        data.server_to_slot = 1;
        data.time = 1;
        data.slot_list.Add(oneSlot);
    }

    void DeepCopyData(ntf_battle_frame_data source, ntf_battle_frame_data dest)
    {
        dest.slot_list[0].cmd_list[0].cmd.UID = source.slot_list[0].cmd_list[0].cmd.UID;
        dest.slot_list[0].cmd_list[0].cmd.cmd_id = source.slot_list[0].cmd_list[0].cmd.cmd_id;
        dest.slot_list[0].cmd_list[0].cmd.cmd_data = StreamBufferPool.GetBuffer(DATA_BYTE_LENGTH);
        for (int i = 0; i < DATA_BYTE_LENGTH; i++)
        {
            dest.slot_list[0].cmd_list[0].cmd.cmd_data[i] = source.slot_list[0].cmd_list[0].cmd.cmd_data[i];
        }
        dest.slot_list[0].cmd_list[0].server_frame = source.slot_list[0].cmd_list[0].server_frame;
        dest.slot_list[0].slot = source.slot_list[0].slot;
        dest.server_curr_frame = source.server_curr_frame;
        dest.server_from_slot = source.server_from_slot;
        dest.server_to_slot = source.server_to_slot;
        dest.time = source.time;
    }

    void InitData(req_enter_room data)
    {
        data.battle_id = 1;
        data.card_list.Add(1);
        data.card_list.Add(2);
        data.card_list.Add(3);
        data.card_list.Add(4);
        data.card_list.Add(5);
    }

    void PrintData(ntf_battle_frame_data data)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("============================");
        sb.AppendFormat("data.server_curr_frame = {0}", data.server_curr_frame);
        sb.AppendLine();
        sb.AppendFormat("data.server_from_slot = {0}", data.server_from_slot);
        sb.AppendLine();
        sb.AppendFormat("data.server_to_slot = {0}", data.server_to_slot);
        sb.AppendLine();
        sb.AppendFormat("data.time = {0}", data.time);
        sb.AppendLine();
        sb.AppendFormat("data.slot_list.Count = {0}", data.slot_list.Count);
        sb.AppendLine();
        if (data.slot_list.Count > 0)
        {
            ntf_battle_frame_data.one_slot oneSlot = data.slot_list[0];
            sb.AppendFormat("oneSlot[0].slot = {0}", oneSlot.slot);
            sb.AppendLine();
            sb.AppendFormat("oneSlot[0].cmd_list.Count = {0}", oneSlot.cmd_list.Count);
            sb.AppendLine();
            if (oneSlot.cmd_list.Count > 0)
            {
                ntf_battle_frame_data.cmd_with_frame cmdWithFrame = oneSlot.cmd_list[0];
                sb.AppendFormat("oneSlot[0].cmd_list[0].server_frame = {0}", cmdWithFrame.server_frame);
                sb.AppendLine();
                if (cmdWithFrame.cmd != null)
                {
                    sb.AppendFormat("oneSlot[0].cmd_list[0].cmd.cmd_id = {0}", cmdWithFrame.cmd.cmd_id);
                    sb.AppendLine();
                    sb.AppendFormat("oneSlot[0].cmd_list[0].cmd.UID = {0}", cmdWithFrame.cmd.UID);
                    sb.AppendLine();
                    if (cmdWithFrame.cmd.cmd_data != null)
                    {
                        sb.AppendFormat("oneSlot[0].cmd_list[0].cmd.cmd_data.Length = {0}", cmdWithFrame.cmd.cmd_data.Length);
                        sb.AppendLine();
                        sb.AppendFormat("oneSlot[0].cmd_list[0].cmd.cmd_data[0] = {0}", cmdWithFrame.cmd.cmd_data[0]);
                        sb.AppendLine();
                        sb.AppendFormat("oneSlot[0].cmd_list[0].cmd.cmd_data[Length - 1] = {0}", cmdWithFrame.cmd.cmd_data[DATA_BYTE_LENGTH - 1]);
                        sb.AppendLine();
                    }
                }
            }
        }
        Debug.Log(sb.ToString());
    }

    void PrintData(req_enter_room data)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("============================");
        sb.AppendFormat("data.battle_id = {0}", data.battle_id);
        sb.AppendLine();
        sb.AppendFormat("data.card_list.Count = {0}", data.card_list.Count);
        sb.AppendLine();
        if (data.card_list.Count > 0)
        {
            List<int> cardList = data.card_list;
            for (int i = 0; i < cardList.Count; i++)
            {
                sb.AppendFormat("{0},", cardList[i]);
            }
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
    }

    void OnDestory()
    {
        bwReader.Close();
        bwWriter.Close();
        msSend.Dispose();
        msRecive.Dispose();
    }

}

public sealed class ReqEnterRoom : ICustomProtoSerializer
{
    public void SetValue(object target, object value, int fieldNumber)
    {
        req_enter_room data = target as req_enter_room;
        if (data == null)
        {
            return;
        }

        switch (fieldNumber)
        {
            case 1:
                data.battle_id = ValueObject.Value<long>(value);
                break;
            case 2:
                data.card_list.Add(ValueObject.Value<int>(value));
                break;
            //其它值忽略
            default:
                break;
        }
    }

    public object GetValue(object target, int fieldNumber)
    {
        req_enter_room data = target as req_enter_room;
        if (data == null)
        {
            return null;
        }

        switch (fieldNumber)
        {
            case 1:
                return ValueObject.Get(data.battle_id);
            case 2:
                return data.card_list;
        }

        return null;
    }
}