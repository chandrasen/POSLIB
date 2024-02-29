using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PosLibs.ECRLibrary.Common
{
    public enum Commands
    {
        CheckStatus = 0,
        ReadCard = 1,
        SaleTransaction = 2,
        VoidTransaction =3
    }
}
