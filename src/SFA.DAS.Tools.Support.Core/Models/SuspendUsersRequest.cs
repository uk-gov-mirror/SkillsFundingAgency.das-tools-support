using System;
using System.Collections.Generic;
using System.Text;

namespace SFA.DAS.Tools.Support.Core.Models
{
    public class SuspendUsersRequest : ResultBase
    {
        public IEnumerable<string> UserRefs { get; set; }
    }
}
