using System;
using System.Diagnostics;
using UnityEngine;

namespace BaseLib
{

    public class AssertException : Exception
    {
        public AssertException(string message) : base(message) { }
    }

    public class Debugger
    {
        static protected Action<bool, String> _pAssertFunc = __doAssert;

        static protected Action<object> _pLogFunc = __doLog;
        static protected Action<object> _pWarnFunc = __doWarn;
        static protected Action<object> _pErrorFunc = __doError;
        static protected Action<Exception, UnityEngine.Object> _pExceptionFunc = __doException;

        static protected bool __printStack = true;

        public static void SetPrintStack(bool value)
        {
            __printStack = value;
        }

        public static void SetUseUnityLogs(bool value)
        {
            if (value)
            {
                SetLogFunction(__doLog);
                SetWarnFunction(__doWarn);
                SetErrorFunction(__doError);
                SetExceptionFunction(__doException);
            }
            else
            {
                SetLogFunction(__doConsoleLog);
                SetWarnFunction(__doConsoleWarn);
                SetErrorFunction(__doConsoleError);
                SetExceptionFunction(__doConsoleException);
            }
        }

        public static void SetExceptionFunction(Action<Exception, UnityEngine.Object> functor = null)
        {
            if (functor == null) _pExceptionFunc = __doException;
            else _pExceptionFunc = functor;
        }

        public static void SetAssertFunction(Action<bool, String> pAssertFunc = null)
        {
            if (pAssertFunc == null) _pAssertFunc = __doAssert;
            else _pAssertFunc = pAssertFunc;
        }

        public static void SetLogFunction(Action<object> pLogFunc = null)
        {
            if (pLogFunc == null) _pLogFunc = __doLog;
            else _pLogFunc = pLogFunc;
        }

        public static void SetWarnFunction(Action<object> functor = null)
        {
            if (functor == null) _pWarnFunc = __doLog;
            else _pWarnFunc = functor;
        }

        public static void SetErrorFunction(Action<object> functor = null)
        {
            if (functor == null) _pErrorFunc = __doLog;
            else _pErrorFunc = functor;
        }

        /// [Conditional("UNITY_EDITOR")]
        public static void Log(object message)
        {
            _pLogFunc(message);
        }

        ///[Conditional("UNITY_EDITOR")]
        public static void LogWarning(object msg)
        {
            _pWarnFunc(msg);
        }

        /// [Conditional("UNITY_EDITOR")]
        public static void LogException(Exception exception, UnityEngine.Object obj)
        {
            _pExceptionFunc(exception, obj);
        }

        /// [Conditional("UNITY_EDITOR")]
        public static void LogError(object msg)
        {
            _pErrorFunc(msg);
        }

        ///[Conditional("UNITY_EDITOR")]
        public static void Assert(bool bCondition, GameObject pGameObject, String message = null)
        {
            if (message == null)
            {
                message = "Hierarchy is:" + GetHierarchy(pGameObject);
            }
            else
            {
                message += ", Hierarchy is:" + GetHierarchy(pGameObject);
            }

            _pAssertFunc(bCondition, message);
        }

        [Conditional("UNITY_EDITOR")]
        public static void Assert(bool bCondition, String message = null)
        {
            _pAssertFunc(bCondition, message);
        }

        [Conditional("UNITY_EDITOR")]
        public static void ConditionalAssert(bool bPreCondition, bool bCondition, String message = null)
        {
            if (bPreCondition)
            {
                _pAssertFunc(bCondition, message);
            }
        }

        static public string GetHierarchy(GameObject obj)
        {
            if (obj == null) return "";
            string path = obj.name;

            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = obj.name + "\\" + path;
            }
            return path;
        }

        [Conditional("UNITY_EDITOR")]
        public static void DebugSection(Action pCodeFunc)
        {
            pCodeFunc();
        }

        private static void __doLog(object message)
        {
            UnityEngine.Debug.Log(message);
        }

        private static void __doWarn(object message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        private static void __doError(object message)
        {
            UnityEngine.Debug.LogError(message);
        }

        private static void __doException(Exception err, UnityEngine.Object obj)
        {
            UnityEngine.Debug.LogException(err, obj);
        }

        private static void __doAssert(bool bCondition, String message)
        {
            if (!bCondition)
            {
                //string str = UnityEngine.StackTraceUtility.ExtractStackTrace ();
                //UnityEngine.Debugger.Log(str);
                throw new AssertException(message);
            }
        }

        private static void __doConsoleLog(object msg)
        {
            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
            System.Console.WriteLine("INFO: " + msg + (__printStack? "\n" + trace : ""));
        }

        private static void __doConsoleWarn(object msg)
        {
            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
            System.Console.WriteLine("WARN: " + msg + (__printStack ? "\n" + trace : ""));
        }

        private static void __doConsoleError(object msg)
        {
            System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace();
            System.Console.WriteLine("ERROR: " + msg + (__printStack ? "\n" + trace : ""));
        }

        private static void __doConsoleException(Exception err, UnityEngine.Object obj)
        {
            System.Console.WriteLine("Exception: " + err + "\n" + obj);
        }
    }

}