using System;


namespace BaseLib
{
    public enum YieldInstructionType
    {
        /// Special instruction, 
        SpecialBegin,

        ReturnValue,
        Coroutine,

        Reserved1,
        Reserved2,
        Reserved3,
        Reserved4,
        Reserved5,
        Reserved6,
        Reserved7,
        Reserved8,
        Reserved9,
        Reserved10,

        SpecialEnd,

        Custom,
    }

    /*
        Note: 
            1. When a yield instruction will be destroyed? The answer is: before the next yield instruction(or function end).
               eg.
                        var yieldA = CoroutineMgr.WaitUntil(xxxx);
                        yield return yieldA;
                        
                        ...... /// Code that using yieldA (correct)
                        var yieldB = CoroutineMgr.WaitUntil(xxxx);
                        ...... /// Code that using yieldA (correct)    

                        yield return yieldB;
    
                        ...... /// Code that using yieldA (incorrect, since yieldA already destroyed)
    */
    public interface IYieldInstruction : IRefCounter
    {
        bool IsDone();

        void Update(float fTime);

        void Pause(float fTime);
        void Resume(float fTime);

        void Stop();

        YieldInstructionType GetInstructionType();

        void Start(float fTime);

        /// Some instruction can timeout
        bool IsTimeout();
    }

    ///-----
    public abstract class YieldInstruction : IYieldInstruction, IPoolable
    {
        protected int _nRefCount = 0;

        public int GetRef() { return _nRefCount; }
        virtual public void IncRef() { _nRefCount++; }

        public void DecRef()
        {
            Debugger.Assert(_nRefCount > 0);
	        _nRefCount--; 

            if ( _nRefCount == 0 )  _onRelease();
        }

        /// ---
        /// Be implement this to start a instruction.
        virtual public void Start(float fTime) { }
        /// Will be called at each frame
        virtual public void Update(float fTime) { }
        /// Pause this instruction
        virtual public void Pause(float fTime) { }
        /// Resume this instruction
        virtual public void Resume(float fTime) { }
        /// Stop this instruction
        virtual public void Stop() {}

        virtual public bool IsDone() { return true; }

        virtual public bool IsTimeout() { return false; }

        /* 
         Note: this method will be called when an instruction can be freed. When does a instruction can be freed? 
         See example below, the first instruction object will be freed before the second instruction's Start() be called.
         
             IEnumerator _testCoroutine()
             {
                   yield return CoroutineMgr.WaitTime();        /// First instruction
                   ......                                                       /// First instruction's Stop() will be called before this line of code.
                   ......   
                   yield return CoroutineMgr.WaitTime();        /// Second instruction. First instruction's _onRelease() will be called after this line of code, but before Second 
                                                                                /// instruction's Start().
             }          
         */
        virtual protected void _onRelease() { }

        /// --- IPoolable ---
        virtual public void Destroy() {}

        virtual public YieldInstructionType GetInstructionType() { return YieldInstructionType.Custom; }
    }

}

