
namespace BaseLib
{
    public static class Bounce
    {
        /// <summary>
        /// Easing equation function for a bounce (exponentially decaying parabolic bounce) easing in: accelerating from zero velocity.
        /// </summary>
        /// <param name="time">
        /// Current time (in frames or seconds).
        /// </param>
        /// <param name="duration">
        /// Expected easing duration (in frames or seconds).
        /// </param>
        /// <param name="unusedOvershootOrAmplitude">Unused: here to keep same delegate for all ease types.</param>
        /// <param name="unusedPeriod">Unused: here to keep same delegate for all ease types.</param>
        /// <returns>
        /// The eased value.
        /// </returns>
        public static float EaseIn(float time, float duration, float unusedOvershootOrAmplitude, float unusedPeriod)
        {
            return 1 - EaseOut(duration - time, duration, -1, -1);
        }

        /// <summary>
        /// Easing equation function for a bounce (exponentially decaying parabolic bounce) easing out: decelerating from zero velocity.
        /// </summary>
        /// <param name="time">
        /// Current time (in frames or seconds).
        /// </param>
        /// <param name="duration">
        /// Expected easing duration (in frames or seconds).
        /// </param>
        /// <param name="unusedOvershootOrAmplitude">Unused: here to keep same delegate for all ease types.</param>
        /// <param name="unusedPeriod">Unused: here to keep same delegate for all ease types.</param>
        /// <returns>
        /// The eased value.
        /// </returns>
        public static float EaseOut(float time, float duration, float unusedOvershootOrAmplitude, float unusedPeriod)
        {
            if ((time /= duration) < (1 / 2.75f)) {
                return (7.5625f * time * time);
            }
            if (time < (2 / 2.75f)) {
                return (7.5625f * (time -= (1.5f / 2.75f)) * time + 0.75f);
            }
            if (time < (2.5f / 2.75f)) {
                return (7.5625f * (time -= (2.25f / 2.75f)) * time + 0.9375f);
            }
            return (7.5625f * (time -= (2.625f / 2.75f)) * time + 0.984375f);
        }

        /// <summary>
        /// Easing equation function for a bounce (exponentially decaying parabolic bounce) easing in/out: acceleration until halfway, then deceleration.
        /// </summary>
        /// <param name="time">
        /// Current time (in frames or seconds).
        /// </param>
        /// <param name="duration">
        /// Expected easing duration (in frames or seconds).
        /// </param>
        /// <param name="unusedOvershootOrAmplitude">Unused: here to keep same delegate for all ease types.</param>
        /// <param name="unusedPeriod">Unused: here to keep same delegate for all ease types.</param>
        /// <returns>
        /// The eased value.
        /// </returns>
        public static float EaseInOut(float time, float duration, float unusedOvershootOrAmplitude, float unusedPeriod)
        {
            if (time < duration*0.5f)
            {
                return EaseIn(time*2, duration, -1, -1)*0.5f;
            }
            return EaseOut(time*2 - duration, duration, -1, -1)*0.5f + 0.5f;
        }
    }
}


