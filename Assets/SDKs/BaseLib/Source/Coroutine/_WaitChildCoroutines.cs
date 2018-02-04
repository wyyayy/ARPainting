using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BaseLib
{
    /*
        Wait all child coroutines done. 
        What is a child coroutine? 
            Child coroutine is a coroutine that started from another coroutine. 

        How to wait child coroutine? 
            Store all child coroutine into one list, and wait them together. 

            Coroutine.CaptureChildCoroutine(childrens).

            yield return CoroutineMgr.WaitMultiInstructions(childrens);

            childrens.clear();
            Coroutine.CaptureChildCoroutine(childrens).

            yield return CoroutineMgr.WaitMultiInstructions(childrens);


        Why need wait children?
            
    */
    public class _WaitChildCoroutines : YieldInstruction
    {

    }
}
