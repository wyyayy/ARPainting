using BaseLib;

namespace Actor.Net
{
    public interface IAsyncCall<T> : IYieldInstruction
    {
        T GetReturn();
    }
}