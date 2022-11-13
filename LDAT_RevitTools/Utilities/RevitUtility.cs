using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities;

public static class RevitUtility
{
    private const double Precision = 0.001;

    public static bool IsEqual(this double a, double b)
    {
        return Math.Abs(a -b) < Precision;
    }
}
