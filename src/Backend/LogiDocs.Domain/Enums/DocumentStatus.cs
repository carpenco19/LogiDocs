using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiDocs.Domain.Enums;

public enum DocumentStatus
{
    Uploaded = 0,
    Verified = 1,
    Tampered = 2,
    Rejected = 3
}