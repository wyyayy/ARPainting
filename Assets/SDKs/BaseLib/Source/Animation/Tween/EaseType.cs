
#pragma warning disable 1591
namespace BaseLib
{
    public enum EaseType
    {
        Invalid = -1,

        Linear = 0,

        InSine,
        OutSine,
        InOutSine,

        InQuad,
        OutQuad,
        InOutQuad,

        InCubic,
        OutCubic,
        InOutCubic,

        InQuart,
        OutQuart,
        InOutQuart,

        InQuint,
        OutQuint,
        InOutQuint,

        InExpo,
        OutExpo,
        InOutExpo,

        InCirc,
        OutCirc,
        InOutCirc,

        InElastic,
        OutElastic,
        InOutElastic,

        InBack,
        OutBack,
        InOutBack,

        InBounce,
        OutBounce,
        InOutBounce,

        NormalEaseEnd,
    }
}