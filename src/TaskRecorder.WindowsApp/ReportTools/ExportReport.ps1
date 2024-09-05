# Check PowerShell version
if ($PSVersionTable.PSVersion.Major -lt 7) {
    Write-Error "This script requires PowerShell 7.x or higher. Your current version is $($PSVersionTable.PSVersion)."
    exit
}

# Load TaskRecorder.Core.dll
Add-Type -Path ([System.IO.Path]::Combine($PSScriptRoot, "../taskreccore.dll"))

# Use TaskRecorder.Core namespace
$WorkingManagerType = [TaskRecorder.Core.WorkingManager]
$LogType = [TaskRecorder.Core.WorkingLog]

# Create an instance of WorkingManager
$workingManager = [Activator]::CreateInstance($WorkingManagerType, [System.IO.Path]::Combine($PSScriptRoot, "../TaskLogs"))

# Call LoadLogs method to retrieve logs
$logs = $workingManager.LoadLogs($true, $true)




# Extract only the date from StartDateTime to group logs
$groupedLogs = $logs | Group-Object { $_.StartDateTime.ToString("yyyy/M/d") }

# Output file for the report
$outputFile = ([System.IO.Path]::Combine($PSScriptRoot, "ExportReport.txt"))

# Open a file stream to output in Shift_JIS encoding
$stream = [System.IO.StreamWriter]::new($outputFile, $false, [System.Text.Encoding]::GetEncoding("shift_jis"))

# Process each group
foreach ($group in $groupedLogs) {
    # Date from StartDateTime
    $date = $group.Name

    # Retrieve WorkingTask.Name and join them with " / ", removing duplicates
    $taskNames = $group.Group | ForEach-Object { $_.WorkingTask.Name } | Sort-Object -Unique
    $taskNamesString = $taskNames -join " / "

    # Format and write the output to the file
    $stream.WriteLine("{0}`t{1}", $date, $taskNamesString)
}

# Close the file stream
$stream.Close()
