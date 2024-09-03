using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text.Json;

namespace TaskRecorder.Core
{
    public class WokingManager
    {
        public List<WorkingTask> WorkingTasks
        {
            get;
            set;
        }

        public WorkingTask CurrentWorkingTask
        {
            get;
            private set;
        }

        public DateTimeOffset CurrentWorkingTaskStartTime
        {
            get;
            private set;
        }

        public string RepositoryPath
        {
            get;
            private set;
        }


        public WokingManager(string repositoryPath)
        {
            if (Directory.Exists(repositoryPath) == false)
                throw new DirectoryNotFoundException();

            this.WorkingTasks = new List<WorkingTask>();
            this.CurrentWorkingTask = WorkingTask.Empty;
            this.RepositoryPath = repositoryPath;
        }


        private void _addWorkingLog(WorkingLog workingLog)
        {
            var fileName = workingLog.EndDateTime.ToString("yyyyMMdd-HHmmss-fff_") + workingLog.WorkingTask.Id.ToString().Replace("-", "") + ".json";
            var filePath = Path.Combine(this.RepositoryPath, fileName);

            if (File.Exists(filePath))
                throw new InvalidOperationException();

            using (var fs = File.OpenWrite(filePath))
            {
                JsonSerializer.Serialize(fs, workingLog);
            }
        }

        private List<WorkingLog> _mergeWorkingLogs(IEnumerable<WorkingLog> workingLogs)
        {
            //var sortedLogs = workingLogs.OrderBy(log => log.EndDateTime).ToList();
            //var mergedLogs = new List<WorkingLog>();

            //WorkingLog? currentLog = null;
            //foreach (var log in sortedLogs)
            //{
            //    if (currentLog == null)
            //    {
            //        currentLog = log;
            //    }
            //    else if (currentLog.WorkingTask.Id == log.WorkingTask.Id)
            //    {
            //        currentLog.EndDateTime = log.EndDateTime;
            //    }
            //    else
            //    {
            //        mergedLogs.Add(currentLog);
            //        currentLog = log;
            //    }
            //}

            //if (currentLog != null)
            //{
            //    mergedLogs.Add(currentLog);
            //}

            //return mergedLogs;

            if (workingLogs == null || workingLogs.Count() == 0)
                return new List<WorkingLog>();

            // EndDateTime でソート
            var sortedLogs = workingLogs.OrderBy(log => log.EndDateTime).ToList();

            var consolidatedLogs = new List<WorkingLog>();
            WorkingLog? currentLog = null;

            foreach (var log in sortedLogs)
            {
                var lId = log?.WorkingTask.Id;
                var cId = currentLog?.WorkingTask.Id;

                if (currentLog == null)
                {
                    currentLog = log;
                }
                else if (currentLog.WorkingTask.Id == log?.WorkingTask.Id)
                {
                    // タスクが同じ場合、開始時刻と終了時刻を統合
                    currentLog.EndDateTime = log.EndDateTime;
                }
                else
                {
                    // 異なるタスクに切り替わった場合、現在のログをリストに追加
                    consolidatedLogs.Add(currentLog);
                    currentLog = log;
                }
            }

            // 最後のログを追加
            if (currentLog != null)
            {
                consolidatedLogs.Add(currentLog);
            }

            return consolidatedLogs;
        }

        public WorkingTask ChangeCurrentTask(WorkingTask workingTask, string description)
        {
            if (workingTask == null)
                throw new ArgumentNullException(nameof(workingTask));
            if (this.WorkingTasks.Contains(workingTask) == false)
                throw new ArgumentOutOfRangeException(nameof(workingTask));

            this._addWorkingLog(new WorkingLog()
            {
                WorkingTask = this.CurrentWorkingTask,
                StartDateTime = this.CurrentWorkingTaskStartTime,
                EndDateTime = DateTimeOffset.Now,
                Description = description,
            });

            this.CurrentWorkingTaskStartTime = DateTimeOffset.Now;
            this.CurrentWorkingTask = workingTask;
            return workingTask;
        }

        public void Pulse()
        {
            this.ChangeCurrentTask(this.CurrentWorkingTask, String.Empty);
        }

        public IEnumerable<WorkingLog> LoadLogs(bool optimize = true)
        {
            var result = new List<WorkingLog>();
            var jsonFiles = Directory.GetFiles(this.RepositoryPath, "*.json", SearchOption.TopDirectoryOnly);
            var rawLogs = jsonFiles.Select(jsonFile =>
            {
                using (var fs = File.OpenRead(jsonFile))
                    return JsonSerializer.Deserialize<WorkingLog>(fs);
            }).OrderBy(item => item?.EndDateTime).ToArray();

            if (optimize)
            {
                result.AddRange(this._mergeWorkingLogs(rawLogs));
            }
            else
            {
                result.AddRange(rawLogs);
            }

            //if (rawLogs.Length == 0)
            //    return result;
            //var firstLog = rawLogs[0];
            //if (firstLog == null)
            //    throw new Exception();
            //result.Add(firstLog);

            //for (var i = 1; i < rawLogs.Length; i++)
            //{
            //    var current = rawLogs[i];
            //    var prev = rawLogs[i - 1];
            //    if (prev?.WorkingTask.Id == current?.WorkingTask.Id)
            //    {

            //    }
            //}

            return result;
        }
    }
}
