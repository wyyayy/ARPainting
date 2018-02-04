using UnityEngine;
using System.Collections;

public class FPSUtil : MonoBehaviour
{
    public float updateInterval = 0.5F;

    protected TextMesh _label;
    private float _fAccum = 0;      // FPS accumulated over the interval
    private int _nFrames = 0;     // Frames drawn over the interval
    private float _fTimeleft;           // Left time for current interval

    protected StringReference _fpsString;

    void Awake()
    {
        _label = GetComponent<TextMesh>();

        if (!_label)
        {
            _label = gameObject.AddComponent<TextMesh>();
        }

        _fTimeleft = updateInterval;
        _fpsString = new StringReference(4);
    }

    void Start()
    {
        var meshRender = this.GetComponent<MeshRenderer>();
        meshRender.sortingLayerName = "UI";
        meshRender.sortingOrder = 100;
    }

    protected void Update()
    {    
        _fTimeleft -= Time.deltaTime;
        _fAccum += Time.timeScale / Time.deltaTime;
        ++_nFrames;

        // Interval ended - update GUI text and start new interval
        if (_fTimeleft <= 0.0)
        {
            float fps = _fAccum / _nFrames;

            BaseLib.Debugger.Assert(NumberUtil.GetDigitCount((int)fps) <= 4, "FPS exceed 9999!!!");

            _fpsString.Clear();
            _fpsString.stringBuilder.Append((int)fps);
            //_label.text = ((int)fps).ToString();
            _label.text = _fpsString.stringHandle;

            if (fps < 30)
                _label.color = Color.yellow;
            else if (fps < 10)
                _label.color = Color.red;
            else
                _label.color = Color.green;
            //	DebugConsole.Log(format,level);
            _fTimeleft = updateInterval;
            _fAccum = 0.0F;
            _nFrames = 0;
        }
    }
}


