using UnityEngine;
using System.Collections;

using BaseLib;

public class SoundMgr : Singleton<SoundMgr>
{
    public bool enabled { get { return _bEnabled; } 
        set
        {
            _bEnabled = value;
            if(_bEnabled == false)
            {
                StopAllSounds();
            }
        }
    }
    protected bool _bEnabled;

    protected const int MAX_SOURCE = 12;

    protected AudioSource[] _arrSources;

    protected float[] _arrOldVolume;
    protected float[] _arrNewVolume;
    protected float[] _arrTransitionStart;
    protected float[] _arrTransitionTime;

    /// Used to implement 3D audio effect. Disable in current version.
    //protected Transform _camera;

	/// <summary>
	/// Immediately sets volume of the specified channel.
	/// </summary>
	/// <param name="i">Channel number</param>
	/// <param name="newVol">New volume setting, 0.0f to 1.0f</param>
	public void SetVolume(int i, float newVol) 
    {
        try
        {
            _arrOldVolume[i] = newVol;
            _arrNewVolume[i] = newVol;
            _arrSources[i].volume = newVol;
        }
        catch(System.Exception e)
        {
            Debugger.LogWarning(e.Message);
        }
	}
	
	/// <summary>
	/// Linearly interpolates volume of the specified channel
	/// from current value to the new value during the specified time.
	/// </summary>
	/// <param name="i">Channel number</param>
	/// <param name="newVol">New volume setting, 0.0f to 1.0f</param>
	/// <param name="time">Time in seconds</param>
	public void SetVolume(int i, float newVol, float time) 
    {
		_arrOldVolume[i] = _arrSources[i].volume;
		_arrNewVolume[i] = newVol;
		_arrTransitionStart[i] = Time.time;
		_arrTransitionTime[i] = time;
	}
	
	/// <summary>
	/// Immediately sets volume of the specified clip. The difference
	/// between this method and SetVolume() taking channel number as
	/// parameter is that this method will effect the setting for all
	/// channels playing the given clip.
	/// </summary>
	/// <param name="c">Audio clip</param>
	/// <param name="newVol">New volume setting, 0.0f to 1.0f</param>
	public void SetVolume(AudioClip c, float newVol) 
    {
		for (int i = 0; i < _arrSources.Length; i++) 
        {
			AudioSource s = _arrSources[i];
			if (s.clip == c) 
            {
				_arrOldVolume[i] = newVol;
				_arrNewVolume[i] = newVol;
				s.volume = newVol;
			}
		}
	}
	
	/// <summary>
	/// Linearly interpolates volume of the specified clip
	/// from current value to the new value during the specified time.
	/// The difference between this method and SetVolume() taking channel
	/// number as parameter is that this method will effect the setting for all
	/// channels playing the given clip.
	/// </summary>
	/// <param name="c">Audio clip</param>
	/// <param name="newVol">New volume setting, 0.0f to 1.0f</param>
	/// <param name="time">Time in seconds</param>
	public void SetVolume(AudioClip c, float newVol, float time) 
    {
		for (int i = 0; i < _arrSources.Length; i++) 
        {
			AudioSource s = _arrSources[i];
			if (s.clip == c) 
            {
				_arrOldVolume[i] = s.volume;
				_arrNewVolume[i] = newVol;
				_arrTransitionStart[i] = Time.time;
				_arrTransitionTime[i] = time;
			}
		}
	}
	

	/// <summary>
	/// Plays given audio clip on any free channel.
	/// </summary>
	/// <param name="c">Audio clip</param>
	/// <param name="loop">Loop setting</param>
	/// <returns>Number of the assigned channel</returns>
	public int PlayClip(AudioClip c, bool loop = false) 
    {
        if (!_bEnabled) return -1;

		for (int i = 0; i < _arrSources.Length; i++) 
        {
			AudioSource s = _arrSources[i];
			if (!s.isPlaying) 
            {
				s.clip = c;
				s.loop = loop;
				s.Play();
				SetVolume(i, 1.0f);
				return i;
			}
		}
		return -1;
	}
	
	/// <summary>
	/// Plays given audio clip on any free channel included in the mask.
	/// </summary>
	/// <param name="c">Audio clip</param>
	/// <param name="mask">Channel mask, e.g. to specify 0th, 3rd and 11th channel, use 0x0809</param>
	/// <param name="loop">Loop setting</param>
	/// <returns>Number of the assigned channel</returns>
	public int PlayClip(AudioClip c, int mask, bool loop) 
    {
        if (!_bEnabled) return -1;

		for (int i = 0; i < _arrSources.Length; i++) 
        {
			if ((mask & (1 << i)) > 0 && !_arrSources[i].isPlaying) 
            {
				_arrSources[i].clip = c;
				_arrSources[i].loop = loop;
				_arrSources[i].Play();
				SetVolume(i, 1.0f);
				return i;
			}
		}
		return -1;
	}
	
	/// <summary>
	/// Stops all channels playing the given clip.
	/// </summary>
	/// <param name="c">Audio clip</param>
	public void StopClip(AudioClip c) 
    {
		foreach (AudioSource s in _arrSources) 
        {
			if (s.clip == c && s.isPlaying)  s.Stop();
		}
	}

    public void StopAllSounds()
    {
        for (int i = 0; i < _arrSources.Length; i++)
        {
            _arrSources[i].Stop();
        }
    }

	/// <summary>
	/// Stops the given channel.
	/// </summary>
	/// <param name="i">Channel number</param>
	public void StopChannel(int i) 
    {
		_arrSources[i].Stop();
	}
	
	///----------------------
    public void Init(GameObject gameObject)
    {
        _bEnabled = true;
        gameObject.AddComponent<AudioListener>();

        _arrSources = new AudioSource[MAX_SOURCE];

        for (int i = 0; i < _arrSources.Length; i++)
        {
            _arrSources[i] = (AudioSource)gameObject.AddComponent(typeof(AudioSource));
        }

        //_camera = GameObject.FindGameObjectWithTag("MainCamera").transform;
        //transform.position = _camera.position;

        _arrOldVolume = new float[_arrSources.Length];
        _arrNewVolume = new float[_arrSources.Length];
        _arrTransitionStart = new float[_arrSources.Length];
        _arrTransitionTime = new float[_arrSources.Length];

        for (int i = 0; i < _arrSources.Length; i++)
        {
            _arrOldVolume[i] = 1.0f;
            _arrNewVolume[i] = 1.0f;
            _arrTransitionStart[i] = 0.0f;
            _arrTransitionTime[i] = 0.00001f;
        }
    }
	
	void OnLevelWasLoaded(int level) 
    {
		//_camera = GameObject.FindGameObjectWithTag("MainCamera").transform;
		for (int i = 0; i < _arrSources.Length; i++) {
			if (!_arrSources[i].loop) {
				_arrSources[i].Stop();
			}
		}
	}	

	void Update() 
    {
		//transform.position = _camera.position;
		for (int i = 0; i < _arrSources.Length; i++) 
        {
			_arrSources[i].volume = Mathf.Lerp(_arrOldVolume[i], _arrNewVolume[i], Mathf.Min(1.0f, (Time.time - _arrTransitionStart[i]) / _arrTransitionTime[i]));

			if (_arrNewVolume[i] <= 0 && _arrSources[i].volume <= 0 && _arrSources[i].isPlaying) 
            {
				_arrSources[i].Stop();
			}
		}         
	}
}





