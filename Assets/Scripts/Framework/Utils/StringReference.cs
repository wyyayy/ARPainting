using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

/* 
Garbage free strings wrapper.

Usage: 
    StringReference str = null;
    str = new StringReference(128);
    str.StringBuilder.Append("This is an example of garbage-");
    str.StringBuilder.Append("free string concatenation.");
    Debug.Log(str.StringHandle);
*/
public class StringReference
{
    public int maxSize { get; private set; }
    public int spaceLeft { get { return maxSize - stringLength; } }
    public int stringLength { get { return stringBuilder.Length; } }
    public string stringHandle { get; private set; }
    public StringBuilder stringBuilder { get; private set; }

    public StringReference(int maxSize = 32)
    {
        Resize(maxSize);
    }

    public void Resize(int maxSize)
    {
        this.maxSize = maxSize;
        stringBuilder = new StringBuilder(maxSize, maxSize);

        try
        {
            // This should work in Mono (Unity3D)
            var typeInfo = stringBuilder.GetType().GetField("_str", BindingFlags.NonPublic | BindingFlags.Instance);
            if (typeInfo != null)
            {
                stringHandle = (string)typeInfo.GetValue(stringBuilder);
            }
        }
        catch
        {

            try
            {
                // This might work on a .NET platform
                var typeInfo = stringBuilder.GetType().GetField("_cached_str", BindingFlags.NonPublic | BindingFlags.Instance);
                if (typeInfo != null)
                {
                    stringHandle = (string)typeInfo.GetValue(stringBuilder);
                }
            }
            catch
            {
                throw new Exception("Can't get access to StringBuilders internal string.");
                /* 
                 * Uncomment this section to get a clue on how to get a reference to the underlying string:
                 * 
                Type t = StringBuilder.GetType();
                foreach (var f in t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                    UnityEngine.Debug.Log(f.Name);
                */
            }

        }
    }

    public void Clear()
    {
        //stringBuilder.Remove(0, stringBuilder.Length);
        stringBuilder.Length = 0;
        stringBuilder.Append("    ");
        stringBuilder.Remove(0, maxSize);
    }

    public void SetText(string text, bool fillOverflow = true)
    {
        SetText(ref text, fillOverflow);
    }

    public void SetText(ref string text, bool fillOverflow = true)
    {
        Clear();

        var max = spaceLeft;
        if (text.Length >= spaceLeft)
        {
            stringBuilder.Append(text, 0, max);
        }
        else
        {
            stringBuilder.Append(text);
            if (fillOverflow) FillOverflow();
        }
    }

    public void Append(ref string text)
    {
        var max = spaceLeft;
        if (text.Length >= spaceLeft)
            stringBuilder.Append(text, 0, max);
        else
            stringBuilder.Append(text);
    }

    public void Append(string text)
    {
        Append(ref text);
    }

    public void FillOverflow(char character = ' ')
    {
        var overflow = spaceLeft;
        if (overflow > 0)
            stringBuilder.Append(character, overflow);
    }

}


