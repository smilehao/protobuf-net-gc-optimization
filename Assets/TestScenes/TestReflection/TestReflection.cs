using UnityEngine;
using System.Collections;
using battle;
using System.Text;
using System.Reflection;
using System;

public class TestReflection : MonoBehaviour
{
    const int DATA_BYTE_LENGTH = 40;

    ntf_battle_frame_data data = new ntf_battle_frame_data();
    byte[] testByte = new byte[40];

    // Use this for initialization
    void Start () {
        InitData(data);
    }
	
	// Update is called once per frame
	void Update () {
        Test1();
        Test2();
    }

    void Test1()
    {
        Type tp = typeof(ntf_battle_frame_data);
        PropertyInfo pi = tp.GetProperty("server_curr_frame", typeof(int));//48B
        Test1_1(pi);
        PrintData(data);
        Test1_2(pi);
        PrintData(data);
        Test1_3(pi);
    }

    void Test2()
    {
        Type tp = typeof(one_cmd);
        PropertyInfo pi = tp.GetProperty("cmd_data", typeof(byte[]));//48B
        Test2_1(pi);
        PrintData(data);
    }

    void Test1_1(PropertyInfo pi)
    {
        int value = (int)pi.GetValue(data, null);// 60B
        pi.SetValue(data, ++value, null);// 48B
    }

    void Test1_2(PropertyInfo pi)
    {
        object obj = pi.GetValue(data, null);// 60B
        pi.SetValue(data, 0, null);// 48B
    }

    void Test1_3(PropertyInfo pi)
    {

        MethodInfo mi = pi.GetGetMethod();
        mi.Invoke(data, null);// 60B
    }

    void Test2_1(PropertyInfo pi)
    {
        pi.GetValue(data.slot_list[0].cmd_list[0].cmd, null);//40B
        pi.SetValue(data.slot_list[0].cmd_list[0].cmd, testByte, null);//48B
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
        data.slot_list.Add(oneSlot);
    }

    void PrintData(ntf_battle_frame_data data)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("============================");
        sb.AppendFormat("data.server_curr_frame = {0}", data.server_curr_frame);
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
                sb.AppendFormat("oneSlot[0].cmd_list[0].cmd.cmd_id = {0}", cmdWithFrame.cmd.cmd_id);
                sb.AppendLine();
                sb.AppendFormat("oneSlot[0].cmd_list[0].cmd.UID = {0}", cmdWithFrame.cmd.UID);
                sb.AppendLine();
                sb.AppendFormat("oneSlot[0].cmd_list[0].cmd.cmd_data.Length = {0}", cmdWithFrame.cmd.cmd_data.Length);
                sb.AppendLine();
                sb.AppendFormat("oneSlot[0].cmd_list[0].cmd.cmd_data[0] = {0}", cmdWithFrame.cmd.cmd_data[0]);
                sb.AppendLine();
                sb.AppendFormat("oneSlot[0].cmd_list[0].cmd.cmd_data[Length - 1] = {0}", cmdWithFrame.cmd.cmd_data[DATA_BYTE_LENGTH - 1]);
                sb.AppendLine();
            }
        }
        Debug.Log(sb.ToString());
    }

    void PrintValue<T>(T value)
    {
        Debug.Log(value.ToString());
    }
}
