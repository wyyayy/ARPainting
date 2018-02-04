using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Diagnostics;
using UnityEngine.SceneManagement;

using BaseLib;

namespace BaseLib
{
    public class Utils
    {
        static public int GetBitIndex(int number)
        {
            for(int i=0; i<64; ++i)
            {
                if ((number >> i) == 1) return i;
            }

            return -1;
        }


        /*
        /// Mouse ray cast with Zero Plane.
        static public Vector2 MouseCastScreenV2()
        {
            var uiPlane = new Plane(Vector3.back, 0);

            var uiRay = GameApp.UICamera.ScreenPointToRay(Input.mousePosition);
            float fDistance;
            uiPlane.Raycast(uiRay, out fDistance);
            var hitPt = uiRay.GetPoint(fDistance);
            return new Vector2(hitPt.x, hitPt.y);
        }

        /// Mouse ray cast with Zero Plane.
        static public Vector3 MouseCastScreenV3()
        {
            var uiPlane = new Plane(Vector3.back, 0);

            var uiRay = GameApp.UICamera.ScreenPointToRay(Input.mousePosition);
            float fDistance;
            uiPlane.Raycast(uiRay, out fDistance);
            return uiRay.GetPoint(fDistance);
        }
        */
        
        static public T GetRootObject<T>(string name) where T : Component
        {
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

            foreach (var obj in rootObjects)
            {
                if (obj.name == name)
                {
                    return obj.GetComponent<T>();
                }
            }

            return null;
        }

        static public T Instantiate<T>(string strResPath, Transform parent = null)
        {
            var obj = Instantiate(strResPath, parent);
            Debugger.Assert(obj != null, "Resource " + strResPath + " load failed!");
            var requestType = obj.GetComponent<T>();
            Debugger.Assert(requestType != null, "Resource " + strResPath + " doesn't contains " + typeof(T).Name + " component!");
            return requestType;
        }

        static public GameObject Instantiate(string strResPath, Transform parent = null)
        {
            var resObj = Resources.Load(strResPath) as GameObject;

            var requestObj = GameObject.Instantiate(resObj, Vector3.zero, Quaternion.identity) as GameObject;
            if (parent != null)
            {
                requestObj.transform.parent = parent;

                requestObj.transform.localPosition = Vector3.zero;
                requestObj.transform.localRotation = Quaternion.identity;
            }

            return requestObj;
        }

        static public System.DateTime GetDateTime(int seconds)
        {
            System.DateTime time = new System.DateTime(1970, 1, 1);
            time = time.AddSeconds(seconds);
            return time;
        }

        static public T ConvertDictData<T>(object pData) where T : class
        {
            IDictionary<string, object> pDictData = pData as Dictionary<string, object>;
            Debugger.Assert(pDictData != null, "type error");
            object pDeserialized = TypeUtil.DictionaryToObject(typeof(T), pDictData);
            return pDeserialized as T;
        }

        static public T ParseEnum<T>(string value)
        {
            return (T)System.Enum.Parse(typeof(T), value);
        }

        static public Vector3 V2ToV3(Vector2 v2)
        {
            return new Vector3(v2.x, 0, v2.y);
        }

        static public Vector3 V2ToV3(Vector2 v2, float y)
        {
            return new Vector3(v2.x, y, v2.y);
        }

        /// 注意：这种判断并不准确！！！
        public static bool IsPrefab(GameObject gameObj)
        {
            return gameObj.transform.parent == null;
        }

        /// 从第一级自对象里获取组件。必须保证所有第一级自对象都包含T。
        public static T[] GetComponentsInChildren<T>(Transform parent) where T : Component
        {
            int nChildCount = parent.childCount;
            T[] arrComponents = new T[nChildCount];

            for (int i = 0; i < nChildCount; ++i)
            {
                var component = parent.GetChild(i).GetComponent<T>();
                Debugger.Assert(component != null);
                arrComponents[i] = component;
            }

            return arrComponents;
        }

        static public void AddChild(Transform pParent, Transform pChild, bool bActive = true, bool bResetTransform = true)
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

        static public void DestroyObjects<T>(List<T> objects) where T : MonoBehaviour
        {
            foreach (T obj in objects)
            {
                UnityEngine.Object.Destroy(obj.gameObject);
            }
        }

        static public void RunOnNextUpdate(System.Action<object> pHandler)
        {
            TimerMgr.DELAY(0.001f, pHandler);
        }

        static public int BigEndianToInt(byte[] buf, int i)
        {
            return (buf[i] << 24) | (buf[i + 1] << 16) | (buf[i + 2] << 8) | buf[i + 3];
        }

        static public byte[] IntToBigEndian(int value)
        {
            byte[] intBytes = System.BitConverter.GetBytes(value);
            if (System.BitConverter.IsLittleEndian) System.Array.Reverse(intBytes);
            byte[] result = intBytes;

            return result;
        }

        static public T DeepCopy<T>(T obj)
        {
            BinaryFormatter s = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                s.Serialize(ms, obj);
                ms.Position = 0;
                T t = (T)s.Deserialize(ms);

                return t;
            }
        }

        static public void AutoScaleCamera(Camera camera, float fLogicWidth, float fLogicHeight)
        {
            var oldRect = camera.rect;
            var rect = _calculateScaledRect(fLogicWidth, fLogicHeight);
            rect.height *= oldRect.height;
            rect.width *= oldRect.width;
            rect.y += oldRect.y * (rect.height / oldRect.height);
            rect.x += oldRect.x * (rect.width / oldRect.width); ;
            camera.rect = rect;
        }

        static private Rect _calculateScaledRect(float fLogicWidth, float fLogicHeight)
        {
            //--- Screen resolution ---//
            float w = Screen.width;
            float h = Screen.height;

            //--- target logic resolution ---//
            float destW = fLogicWidth;
            float destH = fLogicHeight;

            float fx = 0f;
            float fy = 0f;
            float fw = 1f;
            float fh = 1f;

            //--- 现在宽高比 不足, 即 宽度不足，高度足够，那么按照 不足的为基准，调整足够的（按新的宽度来调） ---//
            if (destW / destH > w / h)
            {
                //高度缩小 ，宽度不变为1
                fw = 1f;
                fh = destH * w / destW / h;
                fy = (1f - fh) / 2f; //为了保证视口在屏幕中央， 需要调整视口高度 
            }
            else if (destW / destH < w / h)
            {
                //宽度缩小，高度不变
                fh = 1f;
                fw = destW * h / destH / w;
                fx = (1f - fw) / 2f;
            }

            return new Rect(fx, fy, fw, fh);
        }

        static private Rect _tk2DRectFromUV(Rect uv, float fWidth, float fHeight)
        {
            Rect scaledRect = new Rect(uv.xMin * fWidth, uv.yMin * fHeight, uv.width * fWidth, uv.height * fHeight);

            float xMin = scaledRect.xMin;
            float yMin = fHeight - (scaledRect.yMin + scaledRect.height);

            Rect newRect = new Rect(xMin, yMin, scaledRect.width, scaledRect.height);

            return newRect;
        }

        public static void UpdateByKeyValue(object obj, Dictionary<string, int> dict)
        {
            foreach (var item in dict)
            {
                var fd = obj.GetType().GetField(item.Key);
                if (fd == null)
                {
                    throw new System.NotImplementedException("对象:" + obj.GetType().Name + "不包含属性或字段" + item.Key);
                }
                fd.SetValue(obj, item.Value);
            }
        }

        private static void _appendIndent(StringBuilder strBuilder, int count)
        {
            for (int i = 0; i < count; i++)
            {
                strBuilder.Append(INDENT_STRING);
            }
        }

        private const string INDENT_STRING = "    ";
        public static string FormatJson(string str)
        {
            var indent = 0;
            var quoted = false;
            var sb = new StringBuilder();
            for (var i = 0; i < str.Length; i++)
            {
                var ch = str[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            _appendIndent(sb, ++indent);
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            _appendIndent(sb, --indent);
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && str[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            quoted = !quoted;
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            _appendIndent(sb, indent);
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!quoted)
                            sb.Append(" ");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }

        ///-------------------------------------------------------
        /// 打印加载时间
        private static bool s_bEnableAnlysis = false;
        private static float s_fLastTime = 0;
        private static float s_fTotalTime = 0;

        private static float s_fSubLastTime = 0;
        private static float s_fSubTotalTime = 0;
        private static string s_strSubModuleName = "";

        public static void EnableAnlysis(bool bEnable)
        {
            s_bEnableAnlysis = bEnable;
        }

        //[Conditional("DEBUG")]
        public static void BeginSubLog(string strSubModuleName)
        {
            if (!s_bEnableAnlysis) return;

            Debugger.LogError("==================== 开始分析子对象: " + strSubModuleName);
            s_strSubModuleName = strSubModuleName;
            s_fSubLastTime = Time.realtimeSinceStartup;
            s_fSubTotalTime = 0;
        }

        public static void LogSubTime(string strMsg = null)
        {
            if (!s_bEnableAnlysis) return;

            if (strMsg == null) strMsg = "";

            var fDeltaTime = Time.realtimeSinceStartup - s_fSubLastTime;
            s_fSubTotalTime += fDeltaTime;

            Debugger.LogError("         -------------------------- " + fDeltaTime + ":" + strMsg);

            s_fSubLastTime = Time.realtimeSinceStartup;
        }

        public static void EndSubLog()
        {
            if (!s_bEnableAnlysis) return;

            var fCurTime = Time.realtimeSinceStartup;

            var fDeltaTime = fCurTime - s_fSubLastTime;
            s_fSubTotalTime += fDeltaTime;

            if (fDeltaTime > 0.1f)
            {
                Debugger.LogError("         -------------------------- " + fDeltaTime + ":" + "杂项加载时间");
            }

            Debugger.LogError("==================== " + s_fSubTotalTime + ":" + s_strSubModuleName);

            s_fLastTime = Time.realtimeSinceStartup;
            s_fTotalTime += s_fSubTotalTime;
        }

        public static void StartAnalysis()
        {
            if (!s_bEnableAnlysis) return;

            Debugger.LogError("==================== 开始性能分析 ===========");
            s_fLastTime = Time.realtimeSinceStartup;
        }

        public static void ResetLastTime()
        {
            if (!s_bEnableAnlysis) return;

            s_fLastTime = Time.realtimeSinceStartup;
        }

        //[Conditional("DEBUG")]
        public static void LogTime(string strMsg = null)
        {
            if (!s_bEnableAnlysis) return;

            if (strMsg == null) strMsg = "";

            var fDeltaTime = Time.realtimeSinceStartup - s_fLastTime;
            s_fTotalTime += fDeltaTime;

            Debugger.LogError("==================== " + fDeltaTime + ":" + strMsg);

            s_fLastTime = Time.realtimeSinceStartup;
        }

        public static void LogTotalTime()
        {
            if (!s_bEnableAnlysis) return;

            var fCurTime = Time.realtimeSinceStartup;

            var fDeltaTime = fCurTime - s_fLastTime;
            s_fTotalTime += fDeltaTime;

            if (fDeltaTime > 0.1f)
            {
                Debugger.LogError("==================== " + fDeltaTime + ":" + "杂项加载时间");
            }

            Debugger.LogError("============= 总时间: " + s_fTotalTime + " =============");
        }
    }


}