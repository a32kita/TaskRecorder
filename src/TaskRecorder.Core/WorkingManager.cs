using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using System.Text.Json;

namespace TaskRecorder.Core
{
    public class WorkingManager
    {
        public List<WorkingTask> WorkingTasks
        {
            get;
            private set;
        }

        /// <summary>
        /// 現在従事しているタスクを取得します。
        /// </summary>
        public WorkingTask CurrentWorkingTask
        {
            get;
            private set;
        }

        /// <summary>
        /// 現在従事しているタスクの開始時刻を取得します。
        /// </summary>
        public DateTimeOffset CurrentWorkingTaskStartTime
        {
            get;
            private set;
        }

        /// <summary>
        /// リポジトリ ディレクトリのパスを取得します。
        /// </summary>
        public string RepositoryPath
        {
            get;
            private set;
        }

        /// <summary>
        /// ログファイルとして出力する JSON データのファイル名先頭に付与する時刻のフォーマットを取得または設定します。
        /// </summary>
        public string LogFileTimeFormat
        {
            get;
            set;
        }

        public WorkingManagerTimeProvider TimeProvider
        {
            get;
            set;
        }

        /// <summary>
        /// 日付の境目と判定する時刻を <see cref="WorkingManagerDateLine"/> で取得または設定します。
        /// </summary>
        public WorkingManagerDateLine DateLineUtc
        {
            get;
            set;
        }

        public WorkingManager(string repositoryPath)
        {
            if (Directory.Exists(repositoryPath) == false)
                throw new DirectoryNotFoundException();

            this.WorkingTasks = new List<WorkingTask>();
            this.CurrentWorkingTask = WorkingTask.Empty;
            this.RepositoryPath = repositoryPath;
            this.LogFileTimeFormat = "yyyyMMdd-HHmmss-fff_";

            var timeDiff = DateTime.UtcNow - DateTime.Now;

            this.TimeProvider = new WorkingManagerTimeProvider();
            this.DateLineUtc = new WorkingManagerDateLine()
            {
                Hour = (int)timeDiff.TotalHours,
                Minute = timeDiff.Minutes,
                Second = timeDiff.Seconds
            };
        }


        private void _addWorkingLog(WorkingLog workingLog)
        {
            var fileName = workingLog.EndDateTime.ToString(this.LogFileTimeFormat) + workingLog.WorkingTask.Id.ToString().Replace("-", "") + ".json";
            var filePath = Path.Combine(this.RepositoryPath, fileName);

            if (File.Exists(filePath))
                throw new InvalidOperationException();

            using (var fs = File.OpenWrite(filePath))
            {
                JsonSerializer.Serialize(fs, workingLog);
            }
        }

        private bool _isPassedDayLine(DateTimeOffset a, DateTimeOffset b)
        {
            // C の時刻情報
            TimeSpan c = new TimeSpan(this.DateLineUtc.Hour, this.DateLineUtc.Minute, this.DateLineUtc.Second);

            // A と B の時刻部分を TimeSpan で取得
            TimeSpan timeA = a.TimeOfDay;
            TimeSpan timeB = b.TimeOfDay;

            // C が A と B の間に挟まれているかを判定
            var isBetween = false;

            if (a.Date == b.Date)
            {
                // 同じ日の場合の判定
                isBetween = timeA <= c && c <= timeB;
            }
            else
            {
                // 異なる日をまたぐ場合の判定
                isBetween = c >= timeA || c <= timeB;
            }

            return isBetween;
        }

        private List<WorkingLog> _mergeWorkingLogs(IEnumerable<WorkingLog> workingLogs)
        {
            if (workingLogs == null || workingLogs.Count() == 0)
                return new List<WorkingLog>();

            // EndDateTime でソート
            var sortedLogs = workingLogs.OrderBy(log => log.EndDateTime).ToList();

            var consolidatedLogs = new List<WorkingLog>();
            WorkingLog? currentLog = null;
            var dateLine = new TimeSpan(this.DateLineUtc.Hour, this.DateLineUtc.Minute, this.DateLineUtc.Second);

            foreach (var log in sortedLogs)
            {
                var lId = log?.WorkingTask.Id;
                var cId = currentLog?.WorkingTask.Id;

                var cTime = currentLog?.EndDateTime.TimeOfDay;
                var nTime = dateLine;
                var lTime = log?.StartDateTime.TimeOfDay;

                if (currentLog == null)
                {
                    currentLog = log;
                }
                else if (currentLog.WorkingTask.Id == log?.WorkingTask.Id
                    && _isPassedDayLine(currentLog.EndDateTime, log.StartDateTime) == false)
                {
                    currentLog.EndDateTime = log.EndDateTime;
                }
                else
                {
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
            if (WorkingTask.IsNullOrEmpty(workingTask) == false && this.WorkingTasks.Where(t => t.Id.Equals(workingTask.Id)).Count() == 0)
                throw new ArgumentOutOfRangeException(nameof(workingTask));

            if (WorkingTask.IsNullOrEmpty(this.CurrentWorkingTask) == false)
            {
                this._addWorkingLog(new WorkingLog()
                {
                    WorkingTask = this.CurrentWorkingTask,
                    StartDateTime = this.CurrentWorkingTaskStartTime,
                    EndDateTime = this.TimeProvider.GetNow(),
                    Description = description,
                });
            }

            this.CurrentWorkingTaskStartTime = this.TimeProvider.GetNow();
            this.CurrentWorkingTask = workingTask;
            return workingTask;
        }

        public void Pulse()
        {
            if (WorkingTask.IsNullOrEmpty(this.CurrentWorkingTask))
                return;

            this.ChangeCurrentTask(this.CurrentWorkingTask, String.Empty);
        }

        public IEnumerable<WorkingLog> LoadLogs(bool executePulse = true, bool optimize = true)
        {
            if (executePulse)
                this.Pulse();

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

            return result;
        }
    }
}
