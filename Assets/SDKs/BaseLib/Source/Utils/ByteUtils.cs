using UnityEngine;
using System.Text;

using BaseLib;

public class ByteUtils 
{
	
	/** 
     * short->byte 
     *  
     * @param b 
     * @param s 
     *            
     * @param index 
     */
    public static void Short2Byte(byte[] b, short s, int index)
    {  
        b[index + 1] = (byte) (s >> 0);  
        b[index + 0] = (byte) (s >> 8);  
    }  
  
    /** 
     * byte-->short
     *  
     * @param b 
     * @param index 
     *            
     * @return 
     */  
    public static ushort Byte2Short(byte[] b, int index) 
    {
        return (ushort)(((b[index + 1] & 0xff) | b[index + 0] << 8));  
    }  
  
    /** 
     * int-->byte
     *  
     * @param bb 
     * @param x 
     * @param index 
     */  
    public static void Int2Byte(byte[] bb, int x, int index)
    {  
        bb[index + 3] = (byte) (x >>0);  
        bb[index + 2] = (byte) (x >>8);  
        bb[index + 1] = (byte) (x >>16);  
        bb[index + 0] = (byte) (x >>24);  
    }  
  
    /** 
     * byte-->int
     *  
     * @param bb 
     * @param index 
     *            
     * @return 
     */  
    public static int Byte2Int(byte[] bb, int index) 
    {  
        return (int) ((((bb[index + 3] & 0xff) << 0)  
                | ((bb[index + 2] & 0xff) << 8)  
                | ((bb[index + 1] & 0xff) << 16) | ((bb[index + 0] & 0xff) << 24)));  
    }  
  
    public static void Long2Byte(byte[] bb, long x,int index) 
    { 
        bb[ index+0] = (byte) (x >> 56); 
        bb[ index+1] = (byte) (x >> 48); 
        bb[ index+2] = (byte) (x >> 40); 
        bb[ index+3] = (byte) (x >> 32); 
        bb[ index+4] = (byte) (x >> 24); 
        bb[ index+5] = (byte) (x >> 16); 
        bb[ index+6] = (byte) (x >> 8); 
        bb[ index+7] = (byte) (x >> 0); 
   } 

    public static long Byte2Long(byte[] bb,int index) 
    { 
        return ((((long) bb[ index+0] & 0xff) << 56) 
                | (((long) bb[ index+1] & 0xff) << 48) 
                | (((long) bb[ index+2] & 0xff) << 40) 
                | (((long) bb[ index+3] & 0xff) << 32) 
                | (((long) bb[ index+4] & 0xff) << 24) 
                | (((long) bb[ index+5] & 0xff) << 16) 
                | (((long) bb[ index+6] & 0xff) << 8) | (((long) bb[ index+7] & 0xff) << 0)); 
   } 
    
    public static void OutBytes(string prefix, byte[] arrByte)
	{
		 StringBuilder sb=new StringBuilder();
		 foreach(byte b in arrByte)
         {
			 sb.Append(b);
			 sb.Append(",");
		 }

        Debugger.Log(prefix + sb.ToString());
	}
    
    public static string GetAllBytes(string prefix, byte[] arrByte)
	{
		 StringBuilder sb=new StringBuilder();
		 sb.Append(prefix);

		 foreach(byte b in arrByte)
         {
			 sb.Append(b);
			 sb.Append(",");
		 }
		return sb.ToString();
	}
   
}
