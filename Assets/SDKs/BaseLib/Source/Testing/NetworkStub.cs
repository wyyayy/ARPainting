using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Text;

using JsonFx;

///#if DEBUG_SERVER

/// 在正式版本中要被移除掉
public class NetworkStub
{
    public const byte XOR_CONST = 0X12;


    public static ushort GetCheckCode(byte[] arrBytes)
    {
        ushort sIOutcome = 0;
        ushort sTotal = 0;
        byte bLoop;

        for (int i = 0; i < arrBytes.Length; i++)
        {
            bLoop = arrBytes[i];
            if (arrBytes.Length % 2 == 0)
            {
                sTotal += sIOutcome;
                sIOutcome = 0;
            }
            if (i % 2 == 0)
            {
                sIOutcome += (ushort)((bLoop & 0xFF) << 8);
            }
            else
            {
                sIOutcome += (ushort)((bLoop & 0xFF));
            }
        }

        sTotal += sIOutcome;
        return sTotal;
    }

    public static byte[] BuildEncryptedSessionKey(int nUserID)
    {
        byte[] sessionKey = BuildSessionKey(nUserID);

        sessionKey = _addRandomPrefixSuffix(sessionKey);

        int nMaxLength = sessionKey.Length;
        byte[] arrNewByte = new byte[nMaxLength];
        int lastIndex = nMaxLength - 11;
        for (int i = 0; i < sessionKey.Length; i++)
        {
            if (i <= 10)
            {
                arrNewByte[lastIndex + i] = (byte)(sessionKey[i] ^ XOR_CONST);
            }
            else
            {
                arrNewByte[i - 11] = (byte)(sessionKey[i] ^ XOR_CONST);
            }
        }

        return arrNewByte;
    }

    public static byte[] BuildSessionKey(int nUserID)
    {
        /// 转换成+8时区
        int nTimestamp = CurrentTimeSeconds() + 28800;

        long lCheckCode = _getSeesionKeyCheckCode(nUserID, nTimestamp);

        byte[] sessionKeyBytes = new byte[16];
        ByteUtils.Int2Byte(sessionKeyBytes, nUserID, 0);
        ByteUtils.Int2Byte(sessionKeyBytes, nTimestamp, 4);
        ByteUtils.Long2Byte(sessionKeyBytes, lCheckCode, 8);

        return sessionKeyBytes;
    }

    private static byte[] _addRandomPrefixSuffix(byte[] arrSessionKey)
    {
        ByteBuffer pBufferSession = new ByteBuffer(MsgBase.RAW_SESSION_KEY_SIZE);

        String strPrefix = "12";
        String strSuffix = "345";

        pBufferSession.WriteString(strPrefix);
        pBufferSession.WriteBytes(arrSessionKey);
        pBufferSession.WriteString(strSuffix);

        return pBufferSession.GetInternalBuffer();
    }

    private static long _getSeesionKeyCheckCode(int nUserID, int nTimestamp)
    {
        return (long)nUserID + (long)nTimestamp;
    }

    public static int CurrentTimeSeconds()
    {
        int intResult = 0;
        System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
        intResult = (int)((System.DateTime.Now - startTime).TotalMilliseconds / 1000);
        return intResult;
    }
}

///#endif