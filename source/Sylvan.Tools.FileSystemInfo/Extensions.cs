using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

static class Ex
{
    class OneShotEnumerable<T> : IEnumerable<T>
    {
        IEnumerator<T> e;

        public OneShotEnumerable(IEnumerator<T> e)
        {
            this.e = e;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return e;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    class Enumerator<T> : IEnumerator<T>
    {
        IEnumerator<T> e;
        public Enumerator(IEnumerator<T> e)
        {
            this.e = e;
        }

        public T Current => e.Current;

        object IEnumerator.Current => Current!;

        public void Dispose()
        {   
        }

        public bool MoveNext()
        {
            return e.MoveNext();
        }

        public void Reset()
        {
        }
    }

    public static IEnumerable<T> OneShot<T>(this IEnumerator<T> e)
    {
        e = new Enumerator<T>(e);
        return new OneShotEnumerable<T>(e);
    }
}