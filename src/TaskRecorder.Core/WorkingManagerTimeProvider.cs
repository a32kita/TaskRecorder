using System;
using System.Collections.Generic;
using System.Text;

namespace TaskRecorder.Core
{
    public class WorkingManagerTimeProvider
    {
        public virtual DateTimeOffset GetNow()
        {
            return DateTimeOffset.Now;
        }
    }
}
