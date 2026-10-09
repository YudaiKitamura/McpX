<#
.SYNOPSIS
    GX Simulator3 (127.0.0.1:5511) を外部PCから接続できるようにする。

.DESCRIPTION
    GX Simulator3 はローカルホスト (127.0.0.1) でしか待ち受けないため、
    netsh portproxy で LAN 側アドレスの TCP ポートを 127.0.0.1:5511 へ転送し、
    Windows ファイアウォールに受信許可ルールを追加する。
    管理者権限が必要（未昇格なら自動で UAC 昇格して再実行する）。

.PARAMETER Action
    Enable  : ポートフォワーディングとファイアウォール許可を設定（既定）
    Disable : 設定を削除
    Status  : 現在の設定を表示

.PARAMETER ListenAddress
    外部からの接続を受ける自PCのIPv4アドレス。省略時はデフォルトゲートウェイを持つNICのアドレスを自動検出。
    0.0.0.0 は GX Simulator3 の 127.0.0.1:5511 と競合し得るため、特定アドレスを推奨。

.PARAMETER ListenPort
    外部に公開するポート番号（既定 5511）。

.PARAMETER ConnectPort
    GX Simulator3 の待ち受けポート（既定 5511）。

.PARAMETER RemoteAddress
    接続を許可する送信元（例: 192.168.1.0/24, 192.168.1.50）。既定は LocalSubnet。

.EXAMPLE
    .\gxsim-portforward.ps1
    .\gxsim-portforward.ps1 -ListenAddress 192.168.1.10 -RemoteAddress 192.168.1.50
    .\gxsim-portforward.ps1 -Action Status
    .\gxsim-portforward.ps1 -Action Disable
#>
[CmdletBinding()]
param(
    [ValidateSet('Enable', 'Disable', 'Status')]
    [string]$Action = 'Enable',
    [string]$ListenAddress,
    [int]$ListenPort = 5511,
    [int]$ConnectPort = 5511,
    [string]$RemoteAddress = 'LocalSubnet'
)

$ErrorActionPreference = 'Stop'
$ConnectAddress = '127.0.0.1'
$RuleName = "GX Simulator3 SLMP (TCP $ListenPort)"

# 管理者権限でなければ自己昇格して再実行
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-NoExit', '-File', "`"$PSCommandPath`"", '-Action', $Action,
        '-ListenPort', $ListenPort, '-ConnectPort', $ConnectPort, '-RemoteAddress', $RemoteAddress)
    if ($ListenAddress) { $argList += @('-ListenAddress', $ListenAddress) }
    Start-Process -FilePath 'powershell.exe' -ArgumentList $argList -Verb RunAs
    return
}

function Get-DefaultIPv4 {
    $cfg = Get-NetIPConfiguration |
        Where-Object { $_.IPv4DefaultGateway -and $_.NetAdapter.Status -eq 'Up' } |
        Select-Object -First 1
    if (-not $cfg) { throw 'デフォルトゲートウェイを持つNICが見つかりません。-ListenAddress を指定してください。' }
    return ($cfg.IPv4Address | Select-Object -First 1).IPAddress
}

function Show-Status {
    Write-Host '--- portproxy (v4tov4) ---' -ForegroundColor Cyan
    netsh interface portproxy show v4tov4
    Write-Host '--- Firewall rule ---' -ForegroundColor Cyan
    $rule = Get-NetFirewallRule -DisplayName $RuleName -ErrorAction SilentlyContinue
    if ($rule) {
        $rule | Select-Object DisplayName, Enabled, Direction, Action, Profile | Format-Table -AutoSize
        $rule | Get-NetFirewallAddressFilter | Select-Object RemoteAddress | Format-Table -AutoSize
    } else {
        Write-Host "ルール '$RuleName' はありません。"
    }
    Write-Host "--- TCP $ConnectPort / $ListenPort Listen ---" -ForegroundColor Cyan
    Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
        Where-Object { $_.LocalPort -in @($ConnectPort, $ListenPort) } |
        Select-Object LocalAddress, LocalPort, OwningProcess,
            @{ n = 'Process'; e = { (Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue).ProcessName } } |
        Format-Table -AutoSize
}

switch ($Action) {
    'Enable' {
        if (-not $ListenAddress) { $ListenAddress = Get-DefaultIPv4 }
        Write-Host "転送: ${ListenAddress}:$ListenPort -> ${ConnectAddress}:$ConnectPort" -ForegroundColor Green

        # portproxy は IP Helper サービスで動作する
        Set-Service -Name iphlpsvc -StartupType Automatic
        Start-Service -Name iphlpsvc

        netsh interface portproxy delete v4tov4 listenaddress=$ListenAddress listenport=$ListenPort 2>$null | Out-Null
        netsh interface portproxy add v4tov4 listenaddress=$ListenAddress listenport=$ListenPort `
            connectaddress=$ConnectAddress connectport=$ConnectPort
        if ($LASTEXITCODE -ne 0) { throw 'portproxy の追加に失敗しました。' }

        Get-NetFirewallRule -DisplayName $RuleName -ErrorAction SilentlyContinue | Remove-NetFirewallRule
        New-NetFirewallRule -DisplayName $RuleName -Direction Inbound -Action Allow -Protocol TCP `
            -LocalPort $ListenPort -RemoteAddress $RemoteAddress -Profile Any | Out-Null

        Show-Status
        Write-Host ''
        Write-Host "外部PCからは IP=$ListenAddress Port=$ListenPort (TCP) で接続してください。" -ForegroundColor Green
    }
    'Disable' {
        # 設定済みの listenaddress が不明でも消せるよう、該当ポートの全エントリを削除
        $entries = netsh interface portproxy show v4tov4 |
            Select-String -Pattern '^\s*(\S+)\s+(\d+)\s+\S+\s+\d+' |
            ForEach-Object { [pscustomobject]@{ Address = $_.Matches[0].Groups[1].Value; Port = [int]$_.Matches[0].Groups[2].Value } } |
            Where-Object { $_.Port -eq $ListenPort -and (-not $ListenAddress -or $_.Address -eq $ListenAddress) }
        foreach ($e in $entries) {
            netsh interface portproxy delete v4tov4 listenaddress=$($e.Address) listenport=$($e.Port) | Out-Null
            Write-Host "portproxy 削除: $($e.Address):$($e.Port)"
        }
        Get-NetFirewallRule -DisplayName $RuleName -ErrorAction SilentlyContinue | Remove-NetFirewallRule
        Write-Host "ファイアウォールルール削除: $RuleName"
        Show-Status
    }
    'Status' { Show-Status }
}
