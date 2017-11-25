using UnityEngine;
using CustomDataStruct;
using System.IO;
using battle;
using System;
using System.Text;

/// <summary>
/// 说明：MemoryStream测试脚本
/// 
/// 结论：数据正确、无GC
/// 
/// @by wsh 2017-07-04
/// </summary>

public class TestStreamBuffer : MonoBehaviour
{
    const int DATA_BYTE_LENGTH = 40;
    StreamBuffer sendStreamBuffer;
    StreamBuffer mReceiveStreamBuffer;
    HjSendMsgDef msg;
    ntf_battle_frame_data data = new ntf_battle_frame_data();

    // Use this for initialization
    void Start () {
        sendStreamBuffer = StreamBufferPool.GetStream(1024 * 100, true, false);
        mReceiveStreamBuffer = StreamBufferPool.GetStream(1024 * 100, false, true);

        InitData(data);
        msg = new HjSendMsgDef(1000, data, 1204, 0, 0);
    }
	
	// Update is called once per frame
	void Update ()
    {
        msg.requestSeq++;
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
        msg.msgProto = data;

        int realLen = SerializeMessage(msg, sendStreamBuffer, 0);
        if (realLen == 0)
        {
            Debug.LogError("realLen == 0");
        }
        sendStreamBuffer.CopyTo(mReceiveStreamBuffer.GetBuffer(), 0, 0, realLen);
        int msgLen = 0;
        DeserializeMessage(mReceiveStreamBuffer, 0, realLen, ref msgLen, ref msg);
        if (msgLen == 0)
        {
            Debug.LogError("msgLen == 0");
        }
        Debug.Log("msg.requestSeq = " + msg.requestSeq);
        PrintData(msg.msgProto as ntf_battle_frame_data);
        ProtoFactory.Recycle(msg.msgProto as ntf_battle_frame_data);
    }

    public int SerializeMessage(HjSendMsgDef realMsg, StreamBuffer streamBuffer, uint seq)
    {
        streamBuffer.ResetStream();
        MemoryStream ms = streamBuffer.memStream;
        BinaryWriter br = streamBuffer.binaryWriter;
        
        br.Write((int)0);
        br.Write(realMsg.requestSeq);
        
        long currPosition = ms.Position;
        ProtoBufSerializer.Serialize(ms, realMsg.msgProto);

        int position = (int)ms.Position;
        br.Seek(0, SeekOrigin.Begin);
        br.Write(position - sizeof(int));

        return position;
    }

    public bool DeserializeMessage(StreamBuffer streamBuffer, int start, int len,
        ref int msgLen, ref HjSendMsgDef msgObj)
    {
        msgObj = default(HjSendMsgDef);
        msgLen = 0;
        if (len < sizeof(int))
        {
            return false;
        }

        byte[] data = streamBuffer.GetBuffer();
        int _len = BitConverter.ToInt32(data, start);
        if (len < _len + sizeof(int))
        {
            return false;
        }
        msgLen = _len + sizeof(int);

        start += sizeof(int);
        MemoryStream ms = streamBuffer.memStream;
        BinaryReader br = streamBuffer.binaryReader;
        ms.Seek(start, SeekOrigin.Begin);

        uint requestSeq = br.ReadUInt32();
        msgObj.requestSeq = requestSeq;
        msgObj.msgProto = ProtoBufSerializer.Deserialize(ms, typeof(ntf_battle_frame_data), _len - sizeof(int));
        return true;
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
    
    public struct HjSendMsgDef
    {
        public int msgID;
        public byte version;
        public object msgProto;

        public uint requestSeq;

        public uint UID;
        public int destSvrID;
        public string gateToken;
        public HjSendMsgDef(int _msgID, object _msgProto, uint _UID, int _destSvrid, uint _requestSeq)
        {
            msgID = _msgID;
            msgProto = _msgProto;
            requestSeq = _requestSeq;
            UID = _UID;
            destSvrID = _destSvrid;
            gateToken = null;
            version = 1;
        }

        public HjSendMsgDef(int _msgID, object _msgProto, uint _UID, int _destSvrid, uint _requestSeq, string _gateToken)
        {
            msgID = _msgID;
            msgProto = _msgProto;
            requestSeq = _requestSeq;
            UID = _UID;
            destSvrID = _destSvrid;
            gateToken = _gateToken;
            version = 1;
        }
    }

}
