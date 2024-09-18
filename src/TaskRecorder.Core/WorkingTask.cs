using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;

namespace TaskRecorder.Core
{
    public class WorkingTask
    {
        private bool _isEmpty;


        public Guid Id
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string ShortName
        {
            get;
            set;
        }

        public string Code
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        public DateTimeOffset DueDate
        {
            get;
            set;
        }

        [JsonIgnore]
        public MetaInformation MetaInformation
        {
            get;
            set;
        }


        public static WorkingTask Empty
        {
            get;
            set;
        }


        public WorkingTask()
        {
            this.Id = new Guid();
            this.Name = String.Empty;
            this.ShortName = String.Empty;
            this.Code = String.Empty;
            this.Description = String.Empty;
            this.DueDate = DateTimeOffset.MinValue;
            this.MetaInformation = new MetaInformation();
            this._isEmpty = false;
        }

        static WorkingTask()
        {
            Empty = new WorkingTask()
            {
                _isEmpty = true,
            };
        }


        public string GetShortName()
        {
            if (String.IsNullOrEmpty(this.ShortName))
                return this.Name;
            return this.ShortName;
        }


        public static bool IsNullOrEmpty(WorkingTask workingTask)
        {
            if (workingTask == null)
                return true;

            if (workingTask._isEmpty)
                return true;

            return false;
        }
    }
}
