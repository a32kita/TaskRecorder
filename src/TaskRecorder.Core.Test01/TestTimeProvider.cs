using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskRecorder.Core.Test01
{
    public class TestTimeProvider : WorkingManagerTimeProvider
    {
        public DateTimeOffset? VirtualDateTime
        {
            get;
            set;
        }

        public override DateTimeOffset GetNow()
        {
            return VirtualDateTime ?? DateTimeOffset.Now;
        }
    }
}
