using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiDocs.Domain.Enums;

public enum DocumentType
{
    Invoice = 0,
    PackingList = 1,
    CMR = 2,
    Certificate = 3,
    CustomsDeclaration = 4,
    Other = 99
}