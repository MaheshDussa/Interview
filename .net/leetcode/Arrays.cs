using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace leetcode
{
    public class Arrays
    {

         // need create method to give reverse number 
        public int Reverse(int x)
        {
            int rev = 0;
            while (x != 0)
            {
                if (x < 10)
                {
                    rev = rev * 10 + x;
                    break;
                }
                int pop = x % 10;
                x /= 10;     
                rev = rev * 10 + pop;
            }
            return rev;
        } 

        public int NoOfDigits(int x)
        {
            int count = 0;
            while (x != 0)
            {
                count++;
                x /= 10;
            }
            return count;
        }
        public int sumofDigits(int x)
        {
            int rev = 0;
            while (x != 0)
            {
                if (x < 10)
                {
                    rev = rev  + x;
                    break;
                }
                int pop = x % 10;
                x /= 10;     
                rev = rev + pop;
            }
            return rev;
        } 

        public int FindMaxConsecutiveOnes(int[] nums)
        {
            int maxCount = 0;
            int count = 0;

            for (int i = 0; i < nums.Length; i++)
            {
                if (nums[i] == 1)
                {
                    count++;
                    if (count > maxCount)
                    {
                        maxCount = count;
                    }
                }
                else
                {
                    count = 0;
                }
            }

            return maxCount;
        }

        public int FindNumbersWithEvenNoOfDigits(int[] nums)
        {
            int count = 0;
            foreach (int num in nums)
            {
                 int digits = NoOfDigits(num);
                 if (digits % 2 == 0)
                 {
                     count++;
                 }
            }
            return count;
        }

        
        



}
}