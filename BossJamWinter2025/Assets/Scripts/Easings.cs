//The MIT License (MIT)
//
//Copyright (c) 2016 Damnae
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.
//
// Original: https://github.com/Damnae/storybrew/blob/master/common/Animations/EasingFunctions.cs

using System.Threading.Tasks;
using UnityEngine;

public static class Easings {
    public delegate float EasingFunction(float t);

    private static float Reverse(EasingFunction function, float value) {
        return 1 - function(1 - value);
    }

    private static float ToInOut(EasingFunction function, float value) {
        return .5f * (value < .5f ? function(2 * value) : (2 - function(2 - 2 * value)));
    }

    public static float Derivative(this EasingFunction easing, float t) {
        const float epsilon = 0.001f;
        const float dx = epsilon * 2f;

        float t0 = easing(t - epsilon);
        float t1 = easing(t + epsilon);
        float dy = t1 - t0;

        return dy / dx;
    }

    public static EasingFunction Step = x => x >= 1 ? 1 : 0;
    public static EasingFunction Linear = x => x;

    public static EasingFunction QuadIn = x => x * x;
    public static EasingFunction QuadOut = x => Reverse(QuadIn, x);
    public static EasingFunction QuadInOut = x => ToInOut(QuadIn, x);
    public static EasingFunction CubicIn = x => x * x * x;
    public static EasingFunction CubicOut = x => Reverse(CubicIn, x);
    public static EasingFunction CubicInOut = x => ToInOut(CubicIn, x);
    public static EasingFunction QuartIn = x => x * x * x * x;
    public static EasingFunction QuartOut = x => Reverse(QuartIn, x);
    public static EasingFunction QuartInOut = x => ToInOut(QuartIn, x);
    public static EasingFunction QuintIn = x => x * x * x * x * x;
    public static EasingFunction QuintOut = x => Reverse(QuintIn, x);
    public static EasingFunction QuintInOut = x => ToInOut(QuintIn, x);

    public static EasingFunction SineIn = x => 1 - Mathf.Cos(x * Mathf.PI / 2);
    public static EasingFunction SineOut = x => Reverse(SineIn, x);
    public static EasingFunction SineInOut = x => ToInOut(SineIn, x);

    public static EasingFunction ExpoIn = x => Mathf.Pow(2, 10 * (x - 1));
    public static EasingFunction ExpoOut = x => Reverse(ExpoIn, x);
    public static EasingFunction ExpoInOut = x => ToInOut(ExpoIn, x);

    public static EasingFunction CircIn = x => 1 - Mathf.Sqrt(1 - x * x);
    public static EasingFunction CircOut = x => Reverse(CircIn, x);
    public static EasingFunction CircInOut = x => ToInOut(CircIn, x);

    public static EasingFunction BackIn = x => x * x * ((1.70158f + 1) * x - 1.70158f);
    public static EasingFunction BackOut = x => Reverse(BackIn, x);
    public static EasingFunction BackInOut = x => ToInOut((y) => y * y * ((1.70158f * 1.525f + 1) * y - 1.70158f * 1.525f), x);

    public static EasingFunction BounceIn = x => Reverse(BounceOut, x);
    public static EasingFunction BounceOut = x => x < 1 / 2.75f ? 7.5625f * x * x : x < 2 / 2.75f ? 7.5625f * (x -= (1.5f / 2.75f)) * x + .75f : x < 2.5f / 2.75f ? 7.5625f * (x -= (2.25f / 2.75f)) * x + .9375f : 7.5625f * (x -= (2.625f / 2.75f)) * x + .984375f;
    public static EasingFunction BounceInOut = x => ToInOut(BounceIn, x);

    public static EasingFunction ElasticIn = x => Reverse(ElasticOut, x);
    public static EasingFunction ElasticOut = x => Mathf.Pow(2, -10 * x) * Mathf.Sin((x - 0.075f) * (2 * Mathf.PI) / .3f) + 1;
    public static EasingFunction ElasticOutHalf = x => Mathf.Pow(2, -10 * x) * Mathf.Sin((0.5f * x - 0.075f) * (2 * Mathf.PI) / .3f) + 1;
    public static EasingFunction ElasticOutQuarter = x => Mathf.Pow(2, -10 * x) * Mathf.Sin((0.25f * x - 0.075f) * (2 * Mathf.PI) / .3f) + 1;
    public static EasingFunction ElasticInOut = x => ToInOut(ElasticIn, x);
}
