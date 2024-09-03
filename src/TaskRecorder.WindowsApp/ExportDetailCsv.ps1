# Check PowerShell version
if ($PSVersionTable.PSVersion.Major -lt 7) {
    Write-Error "This script requires PowerShell 7.x or higher. Your current version is $($PSVersionTable.PSVersion)."
    exit
}

# Load TaskRecorder.Core.dll
Add-Type -Path ([System.IO.Path]::Combine($PSScriptRoot, "TaskRecorder.Core.dll"))

# Use TaskRecorder.Core namespace
$WorkingManagerType = [TaskRecorder.Core.WorkingManager]
$LogType = [TaskRecorder.Core.WorkingLog]

# Create an instance of WorkingManager
$workingManager = [Activator]::CreateInstance($WorkingManagerType, [System.IO.Path]::Combine($PSScriptRoot, "TaskLogs"))

# Call LoadLogs method to retrieve logs
$logs = $workingManager.LoadLogs($true, $true)

# Display each log
foreach ($log in $logs) {
    $taskName = $log.WorkingTask.Name
    $taskId = $log.WorkingTask.Id
    $taskCode = $log.WorkingTask.Code
    $startDateTime = $log.StartDateTime.ToString("yyyy/MM/dd HH:mm:ss")
    $endDateTime = $log.EndDateTime.ToString("yyyy/MM/dd HH:mm:ss")
    
    Write-Output ("`"{0}`",`"{1}`",`"{2}`",`"{3}`",`"{4}`"" -f $taskName, $taskId, $taskCode, $startDateTime, $endDateTime)
}
