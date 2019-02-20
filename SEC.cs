using System;

namespace Parsec
{
    // Serial Encoding Client component, main class of PARSEC
    class SEC
    {
        static void Main(string[] args)
        {
            Sequence s = new Sequence("octave.mid");
            s.print();
        }
    }
}
