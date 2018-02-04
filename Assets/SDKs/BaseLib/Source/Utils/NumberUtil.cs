using UnityEngine;
using System.Collections;

using BaseLib;

    public class NumberUtil
    {
        /// Return the digit count. For example, 4567's digit count is 4; 33's digit count is 2; 98789's digit count is 5.
        static public int GetDigitCount(int number)
        {
            int nCount = 1;
            for (int i = 0; i < 100; i++)
            {
                number = (int)(number / 10);

                if (number == 0)
                {
                    break;
                }
                nCount++;
            }

            return nCount;
        }

        /// Return all the digit numbers. For example, GetDigits(12345) will return [5, 4, 3, 2, 1].
        static public QuickList<int> GetDigits(int number, QuickList<int> arrDigits)
        {
            Debugger.Assert(arrDigits.Count == 0);

            number = (int)(Mathf.Abs(number));

            for (int i = 0; i < 100; ++i)
            {
                arrDigits.Add(number % 10);
                number = (int)(number / 10);

                if (number == 0)
                {
                    break;
                }
            }

            return arrDigits;
        }

    }
