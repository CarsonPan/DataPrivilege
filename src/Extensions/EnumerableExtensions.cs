using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace DataPrivilege
{
    public class CommonComparer<T> : IComparer<T>
    {
        private readonly Func<T, T, int> _compare;

        public CommonComparer(Func<T, T, int> compare)
        {
            _compare = compare;
        }

        public CommonComparer()
        { 
        }
        public int Compare([AllowNull] T x, [AllowNull] T y)
        {
            if (_compare != null)
            {
                return _compare(x, y);
            }
            if (x is IComparable<T> comparer)
            {
                return comparer.CompareTo(y);
            }
            throw new Exception("请指定比较方式！");
        }
    }

    public static class EnumerableExtensions
    {
        //private static int SortUnit<T>(T[] array, int low, int high, IComparer<T> comparer,bool asc)
        //{
        //    T key = array[low];
        //    while (low < high)
        //    {
        //        bool result = asc ? comparer.Compare(array[high], key) > -1 : comparer.Compare(array[high], key) < 1;
        //        while (result && high > low)
        //        {
        //            --high;
        //            result = asc ? comparer.Compare(array[high], key) > -1 : comparer.Compare(array[high], key) < 1;
        //        }
        //        array[low] = array[high];
        //        result = asc ? comparer.Compare(array[low], key) <1 : comparer.Compare(array[low], key) >-1;
        //        while (result && high > low)
        //        {
        //            ++low;
        //            result = asc ? comparer.Compare(array[low], key) < 1 : comparer.Compare(array[low], key) > -1;
        //        }
        //        array[high] = array[low];
        //    }
        //    array[low] = key;
        //    return high;
        //}

        //private static void SortCore<T>(T[] array,int low,int high, IComparer<T> comparer, bool asc)
        //{
        //    if (low >= high)
        //        return;
        //    int index = SortUnit(array, low, high,comparer,asc);
        //    SortCore(array, low, index - 1,comparer,asc);
        //    SortCore(array, index + 1, high,comparer,asc);
        //}
        
        public static IEnumerable<T> Sort<T>(this IEnumerable<T> source, bool asc = true, IComparer<T> comparer=null)
        {

            if (comparer == null)
            {
                comparer = new CommonComparer<T>();
            }
            T[] array = source.ToArray();
            Array.Sort(array, comparer);
            if(!asc)
            {
               return array.Reverse();
            }
            return array;
        }

        public static IEnumerable<T> Sort<T>(this IEnumerable<T> source, Func<T, T, int> compare, bool asc = true)
        {
            return source.Sort(asc, new CommonComparer<T>(compare));
        }
    }
}
