using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace TaskRecorder.Core.Test01
{
    [TestClass]
    public class WorkingManagerTest
    {
        public string RepositoryPath
        {
            get;
            private set;
        } = String.Empty;

        public WorkingTask[] WorkingTasks
        {
            get;
            private set;
        } = new WorkingTask[0];


        [TestInitialize]
        public void Initialize()
        {
            this.RepositoryPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location ?? "") ?? "", "TestRepository");
            if (Directory.Exists(this.RepositoryPath) == false)
            {
                Directory.CreateDirectory(this.RepositoryPath);
            }

            var RepositoryOld = Path.Combine(this.RepositoryPath, "Old");
            if (Directory.Exists(RepositoryOld) == false)
            {
                Directory.CreateDirectory(RepositoryOld);
            }

            var jsonFiles = Directory.GetFiles(this.RepositoryPath, "*.json", SearchOption.TopDirectoryOnly);
            foreach (var jsonFile in jsonFiles)
            {
                File.Move(jsonFile, Path.Combine(RepositoryOld, Path.GetFileName(jsonFile)), true);
            }

            this.WorkingTasks = new WorkingTask[]
            {
                new WorkingTask() { Id = Guid.Parse("00000000-0000-0000-0001-000000000000"), Name = "Task 01", Code = "task-01", Description = "Task 01 Description", DueDate = DateTimeOffset.Parse("2024/09/01 15:00:00 +09:00") },
                new WorkingTask() { Id = Guid.Parse("00000000-0000-0000-0002-000000000000"), Name = "Task 02", Code = "task-02", Description = "Task 02 Description", DueDate = DateTimeOffset.Parse("2024/09/02 15:00:00 +09:00") },
                new WorkingTask() { Id = Guid.Parse("00000000-0000-0000-0003-000000000000"), Name = "Task 03", Code = "task-03", Description = "Task 03 Description", DueDate = DateTimeOffset.Parse("2024/09/03 15:00:00 +09:00") },
                new WorkingTask() { Id = Guid.Parse("00000000-0000-0000-0004-000000000000"), Name = "Task 04", Code = "task-04", Description = "Task 04 Description", DueDate = DateTimeOffset.Parse("2024/09/04 15:00:00 +09:00") },
                new WorkingTask() { Id = Guid.Parse("00000000-0000-0000-0005-000000000000"), Name = "Task 05", Code = "task-05", Description = "Task 05 Description", DueDate = DateTimeOffset.Parse("2024/09/05 15:00:00 +09:00") },
                new WorkingTask() { Id = Guid.Parse("00000000-0000-0000-0006-000000000000"), Name = "Task 06", Code = "task-06", Description = "Task 06 Description", DueDate = DateTimeOffset.Parse("2024/09/06 15:00:00 +09:00") },
            };
        }


        [TestMethod("ChangeCurrentTask 正常系: 有効なタスク切換え")]
        public void ChangeCurrentTask_Test01()
        {
            var workingManager = new WorkingManager(this.RepositoryPath);
            workingManager.WorkingTasks.AddRange(this.WorkingTasks);
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[0], "Change description");
        }

        [TestMethod("ChangeCurrentTask 異常系: 無効なタスク切換え")]
        public void ChangeCurrentTask_Test02()
        {
            try
            {
                var workingManager = new WorkingManager(this.RepositoryPath);
                workingManager.WorkingTasks.AddRange(this.WorkingTasks);
                workingManager.ChangeCurrentTask(new WorkingTask() { Id = Guid.Parse("10000000-0000-0000-0000-000000000000"), Name = "Task 06", Code = "task-06", Description = "Task 06 Description", DueDate = DateTimeOffset.Parse("2024/09/06 15:00:00 +09:00") }, "Change description");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is ArgumentException);
                return;
            }

            Assert.Fail();
        }

        [TestMethod("ChangeCurrentTask 正常系: ログ出力, 最適化なし")]
        public void ChangeCurrentTask_Test11()
        {
            var timeProvider = new TestTimeProvider();
            var workingManager = new WorkingManager(this.RepositoryPath);
            workingManager.WorkingTasks.AddRange(this.WorkingTasks);
            workingManager.TimeProvider = timeProvider;

            timeProvider.VirtualDateTime = DateTimeOffset.Parse("2024/09/01 09:30:00 +09:00");
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[0], "Change description");

            timeProvider.VirtualDateTime += new TimeSpan(0, 10, 0);
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[1], "Change description");

            timeProvider.VirtualDateTime += new TimeSpan(0, 10, 0);
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[2], "Change description");

            timeProvider.VirtualDateTime += new TimeSpan(0, 10, 0);
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[1], "Change description");

            timeProvider.VirtualDateTime += new TimeSpan(0, 10, 0);
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[1], "Change description");

            timeProvider.VirtualDateTime += new TimeSpan(0, 10, 0);
            var logs = workingManager.LoadLogs(true, false);
            foreach (var log in logs)
            {
                Logger.LogMessage("{0}: {1} ({2} => {3})", log.WorkingTask.Name, log.WorkingTask.Id, log.StartDateTime.ToString("yyyy/MM/dd HH:mm:ss"), log.EndDateTime.ToString("yyyy/MM/dd HH:mm:ss"));
            }

            Assert.IsNotNull(logs);
            Assert.AreEqual(5, logs.Count());
        }

        [TestMethod("ChangeCurrentTask 正常系: ログ出力, 最適化あり")]
        public void ChangeCurrentTask_Test12()
        {
            var timeProvider = new TestTimeProvider();
            var workingManager = new WorkingManager(this.RepositoryPath);
            workingManager.WorkingTasks.AddRange(this.WorkingTasks);
            workingManager.TimeProvider = timeProvider;

            timeProvider.VirtualDateTime = DateTimeOffset.Parse("2024/09/04 09:30:00 +09:00");
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[0], "Change description");

            timeProvider.VirtualDateTime += new TimeSpan(0, 10, 0);
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[1], "Change description");

            timeProvider.VirtualDateTime += new TimeSpan(0, 10, 0);
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[2], "Change description");

            timeProvider.VirtualDateTime += new TimeSpan(0, 10, 0);
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[1], "Change description");

            timeProvider.VirtualDateTime += new TimeSpan(0, 10, 0);
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[1], "Change description");

            timeProvider.VirtualDateTime += new TimeSpan(0, 10, 0);
            var logs = workingManager.LoadLogs(true, true);
            foreach (var log in logs)
            {
                Logger.LogMessage("{0}: {1} ({2} => {3})", log.WorkingTask.Name, log.WorkingTask.Id, log.StartDateTime.ToString("yyyy/MM/dd HH:mm:ss"), log.EndDateTime.ToString("yyyy/MM/dd HH:mm:ss"));
            }

            Assert.IsNotNull(logs);
            Assert.AreEqual(4, logs.Count());
        }

        [TestMethod("ChangeCurrentTask 正常系: ログ出力, 最適化あり, 日付またぎ")]
        public void ChangeCurrentTask_Test13()
        {
            var timeProvider = new TestTimeProvider();
            var workingManager = new WorkingManager(this.RepositoryPath);
            workingManager.WorkingTasks.AddRange(this.WorkingTasks);
            workingManager.TimeProvider = timeProvider;

            timeProvider.VirtualDateTime = DateTimeOffset.Parse("2024/09/02 09:30:00 +09:00");
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[0], "Change description");

            timeProvider.VirtualDateTime += new TimeSpan(0, 10, 0);
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[1], "Change description");

            timeProvider.VirtualDateTime += new TimeSpan(0, 10, 0);
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[2], "Change description");

            timeProvider.VirtualDateTime += new TimeSpan(0, 10, 0);
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[1], "Change description");

            // シャットダウン
            timeProvider.VirtualDateTime += new TimeSpan(0, 10, 0);
            workingManager.Pulse();

            // リセット
            workingManager = new WorkingManager(this.RepositoryPath);
            workingManager.WorkingTasks.AddRange(this.WorkingTasks);
            workingManager.TimeProvider = timeProvider;

            timeProvider.VirtualDateTime += new TimeSpan(24, 10, 0);
            workingManager.ChangeCurrentTask(workingManager.WorkingTasks[1], "Change description");

            timeProvider.VirtualDateTime += new TimeSpan(0, 10, 0);
            var logs = workingManager.LoadLogs(true, true);
            foreach (var log in logs)
            {
                Logger.LogMessage("{0}: {1} ({2} => {3})", log.WorkingTask.Name, log.WorkingTask.Id, log.StartDateTime.ToString("yyyy/MM/dd HH:mm:ss"), log.EndDateTime.ToString("yyyy/MM/dd HH:mm:ss"));
            }

            Assert.IsNotNull(logs);
            Assert.AreEqual(5, logs.Count());
        }
    }
}