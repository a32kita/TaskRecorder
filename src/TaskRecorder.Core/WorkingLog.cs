using System;
using System.Collections.Generic;
using System.Text;

namespace TaskRecorder.Core
{
    public class WorkingLog
    {
        public WorkingTask WorkingTask
        {
            get;
            set;
        }

        public DateTimeOffset StartDateTime
        {
            get;
            set;
        }

        public DateTimeOffset EndDateTime
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public TimeSpan TimeTaken
        {
            get => this.EndDateTime - StartDateTime;
        }

        public WorkingLog()
        {
            this.WorkingTask = WorkingTask.Empty;
            this.StartDateTime = DateTimeOffset.MinValue;
            this.EndDateTime = DateTimeOffset.MinValue;
            this.Description = String.Empty;
        }
    }
}
