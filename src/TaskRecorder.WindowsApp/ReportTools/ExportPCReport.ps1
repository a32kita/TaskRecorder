# イベントログからログオン・ログオフ情報を取得し、CSV 形式で出力するスクリプト

# イベントIDに対応するログオンとログオフのイベントID
$logonEventID = 7001
$logoffEventID = 7002

# 休憩時間
$lunchBreak = 1.0

# 出力するCSVのパス（スクリプトのディレクトリに保存）
$outputFile = Join-Path -Path $PSScriptRoot -ChildPath "ExportPCReport.csv"

# 日付毎のログオン・ログオフ時間を保存する配列
$reportData = @()

# イベントログの取得（最大1000イベント）
$events = Get-WinEvent -FilterHashtable @{ LogName="System"; ProviderName="Microsoft-Windows-Winlogon" } -MaxEvents 150

# イベントを日付ごとにグループ化
$groupedEvents = $events | Group-Object { $_.TimeCreated.ToString("yyyy-MM-dd") }

# 日付ごとに処理
foreach ($group in $groupedEvents) {
    $logonTimes = $group.Group | Where-Object { $_.Id -eq $logonEventID } | Sort-Object TimeCreated
    $logoffTimes = $group.Group | Where-Object { $_.Id -eq $logoffEventID } | Sort-Object TimeCreated

    # 初回ログオンと最終ログオフを取得
    $firstLogon = $logonTimes | Select-Object -First 1
    $lastLogoff = $logoffTimes | Select-Object -Last 1

    if ($firstLogon -and $lastLogoff) {
        # 利用時間の計算
        $logonTime = $firstLogon.TimeCreated
        $logoffTime = $lastLogoff.TimeCreated
        $uptime = $logoffTime - $logonTime
        $uptimeHours = [Math]::Round($uptime.TotalHours - $lunchBreak, 1)  # x.x 時間表記にする

        # データを配列に追加
        $reportData += [pscustomobject]@{
            日付          = $logonTime.ToString("yyyy/MM/dd")
            初回ログオン  = $logonTime.ToString("HH:mm")
            最終ログオフ  = $logoffTime.ToString("HH:mm")
            利用時間      = $uptime.ToString("hh\:mm")
            レポート時間  = $uptimeHours
        }
    }
}

# CSVとして保存（Shift-JISでエンコード）
$csvData = $reportData | ConvertTo-Csv -NoTypeInformation
[System.IO.File]::WriteAllLines($outputFile, $csvData, [System.Text.Encoding]::GetEncoding("shift_jis"))

# 完了メッセージ
Write-Host "レポートが $outputFile に保存されました。"
