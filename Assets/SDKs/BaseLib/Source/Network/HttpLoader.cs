using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using BaseLib;

public class HttpRequest
{
    public delegate void Handler(HttpRequest request);

    public HttpRequest(WWW w) {www = w; }

    public WWW www;
    public object userData;
    public float timeout;
    public float startTime;
    public Handler handler;
    public string url;
}

public class HttpLoader : System.IDisposable
{
    public int aliveRequestCount { get { return __nAliveRequestCount; } }

    public int pendingRequestCount { get { return __nAliveRequestCount + _queuedRequests.Count; } }

    public HttpRequest.Handler DataHandler;
    public HttpRequest.Handler ErrorHandler;
    public HttpRequest.Handler TimeoutHandler;

    private HashSet<HttpRequest> _aliveRequests;
    private LinkedList<HttpRequest> _queuedRequests;
    private int _nMaxParallelRequest;

    private Timer _timer;

    private int __nAliveRequestCount;
    private QuickList<HttpRequest> __deadList;

    public HttpLoader(HttpRequest.Handler dataHandler = null
                                , HttpRequest.Handler errorHandler = null
                                , HttpRequest.Handler timeoutHandler = null
                                , int nMaxParallelRequest = 3
                                , float pollInterval = 0.2f)
    {
        _nMaxParallelRequest = nMaxParallelRequest;

        DataHandler = _dataHandler;
        ErrorHandler = _errorHandler;
        TimeoutHandler = _timeoutHandler;

        if (dataHandler != null) DataHandler = dataHandler;
        if (errorHandler != null) ErrorHandler = errorHandler;
        if (timeoutHandler != null) TimeoutHandler = timeoutHandler;

        _aliveRequests = new HashSet<HttpRequest>();
        _timer = TimerMgr.REPEAT(MathEx.INFINITE, pollInterval, _onPollRequest);

        _queuedRequests = new LinkedList<HttpRequest>();

        __deadList = new QuickList<HttpRequest>();
        __nAliveRequestCount = 0;
    }

    /// Send a http request.
    /// Return a HttpRequest object. Can cancel a request by pass this object to HttpLoader.CancelRequest(). 
    public HttpRequest SendRequest(string url,  HttpRequest.Handler handler
                                                    , object userData = null
                                                    , float timeout = MathEx.FLOAT_INFINITE)
    {
        HttpRequest newRequest = null;

        if(aliveRequestCount >= _nMaxParallelRequest)
        {
            newRequest = new HttpRequest(null);
            newRequest.url = url;
            newRequest.userData = userData;
            newRequest.timeout = timeout;
            newRequest.handler = handler;
            _queuedRequests.AddLast(newRequest);
        }
        else
        {
            WWW wwwTemp = new WWW(url);

            /// Make polling interleaved to avoid many more requests be processed at same time.
            newRequest = new HttpRequest(wwwTemp);
            newRequest.url = url;
            newRequest.startTime = Time.time;
            newRequest.timeout = timeout;
            newRequest.userData = userData;
            newRequest.handler = handler;

            _aliveRequests.Add(newRequest);

            __nAliveRequestCount++;
        }

        return newRequest;
    }

    public void CancelAllRequests()
    {
        _aliveRequests.Clear();
        _queuedRequests.Clear();

        __nAliveRequestCount = 0;
    }

    public bool CancelRequest(HttpRequest request)
    {
        bool bCanceled = false; ;

        if (_aliveRequests.Contains(request))
        {
            _aliveRequests.Remove(request);
            bCanceled = true;
            __nAliveRequestCount--;
        }

        if(!bCanceled)
        {
            bCanceled = _queuedRequests.Remove(request);
        }

        return bCanceled;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer = null;
    }

    ///---------------
    private void _onPollRequest(object userData)
    {
        __deadList.Clear();

        foreach(HttpRequest request in _aliveRequests)
        {
            WWW www = request.www;

            if (request.www.error != null && www.error != string.Empty)
            {
                __nAliveRequestCount--;
                __deadList.Add(request);

                ErrorHandler(request);
            }
            else if (www.isDone)
            {
                __nAliveRequestCount--;
                __deadList.Add(request);

                request.handler(request);
                DataHandler(request);
            }
            else
            {
                float elapsedTime = Time.time - request.startTime;
                if (elapsedTime > request.timeout)
                {
                    __nAliveRequestCount--;
                    __deadList.Add(request);

                    TimeoutHandler(request);
                }
            }
        }

        foreach(HttpRequest deadRequest in __deadList)
        {
            _aliveRequests.Remove(deadRequest);
        }

        if (aliveRequestCount < _nMaxParallelRequest 
            && _queuedRequests.Count != 0)
        {
            int nNewAliveCount = _nMaxParallelRequest - aliveRequestCount;

            if (nNewAliveCount > _queuedRequests.Count) nNewAliveCount = _queuedRequests.Count;

            for(int i=0; i<nNewAliveCount; ++i)
            {
                HttpRequest inQueueRequest = _queuedRequests.First.Value;
                Debugger.Assert(inQueueRequest.www == null);
                inQueueRequest.www = new WWW(inQueueRequest.url);
                inQueueRequest.startTime = Time.time;
                _aliveRequests.Add(inQueueRequest);
                __nAliveRequestCount++;

                _queuedRequests.RemoveFirst();                
            }
        }
    }

    private void _dataHandler(HttpRequest request)
    {
        //Debugger.Log("_dataHandler: " + request.www.text + " URL is:" + request.url);
    }

    private void _errorHandler(HttpRequest request)
    {
        //Debugger.LogError("Http request: " + request.www.error + " URL is:" + request.url);
    }

    private void _timeoutHandler(HttpRequest request)
    {
        //Debugger.LogError("HttpRequest time out, URL is: " + request.www.url);
    }

}


