using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using BaseLib;


public static class ComponentExt
{
    public static Vector3 GetPosition(this Component component)
    {
        return component.transform.position;
    }

    public static void SetPosition(this Component component, Vector3 position)
    {
        component.transform.position = position;
    }

    public static Vector3 GetLocalPosition(this Component component)
    {
        return component.transform.localPosition;
    }

    public static void SetLocalPosition(this Component component, Vector3 position)
    {
        component.transform.localPosition = position;
    }

    public static void SetVisible(this Component component, bool bVisible)
    {
        component.gameObject.SetActive(bVisible);
    }

    public static void SetActive(this Component component, bool bActive)
    {
        component.gameObject.SetActive(bActive);
    }

    public static Transform GetChild(this Component component, string strChildName)
    {
        var parent = component.transform;
        var child = parent.Find(strChildName);
        return child;
    }

    public static T GetChild<T>(this Component component, string strChildName = null) where T : Component
    {
        if (strChildName == null)
        {
            var type = typeof(T);
            strChildName = type.Name;
        }

        var parent = component.transform;
        var child = parent.Find(strChildName);
        Debugger.Assert(child != null, "Utility.GetChild failed, " + strChildName + " is not a child of " + parent.name);
        var temp = child.GetComponent<T>();
        Debugger.Assert(component != null, strChildName + " is not a child of " + parent.name);
        return temp;
    }

    public static void Bind<T>(this Component component, out T child) where T : Component
    {
        child = component.GetComponent<T>();
    }

    public static void BindChild<T>(this Component component, out T child, string strChildName) where T : Component
    {
        child = component.GetChild<T>(strChildName);
    }

    public static void AddChild(this Transform pParent, Transform pChild, bool bActive = true, bool bResetTransform = true)
    {
        pChild.SetParent(pParent.transform);

        if (bResetTransform)
        {
            pChild.transform.localPosition = Vector3.zero;
            pChild.localRotation = Quaternion.identity;
            pChild.localScale = Vector3.one;
        }

        pChild.gameObject.SetActive(bActive);
    }
}