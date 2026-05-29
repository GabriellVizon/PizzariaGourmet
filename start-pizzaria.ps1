<#
Usage: Open PowerShell in the project folder and run: .\start-pizzaria.ps1
This script will stop any process using the chosen port, optionally prompt to set env vars
for this session, then run `dotnet restore`, `dotnet build`, and `dotnet run --urls`.
#>

param(
  [int]$Port = 5000,
  [string]$Url = "http://localhost"
)

function Kill-PortProcess {
  param($port)
  $pids = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique
  if ($pids) {
    Write-Output ([string]::Format('Encontrado(s) PID usando a porta {0}: {1}', $port, ($pids -join ', ')))
    foreach ($pid in $pids) {
      try {
        Stop-Process -Id $pid -Force -ErrorAction Stop
        Write-Output ([string]::Format('Finalizado PID {0}', $pid))
      } catch {
        Write-Warning ([string]::Format('Falha ao finalizar PID {0}: {1}', $pid, $_.Exception.Message))
      }
    }
    Start-Sleep -Milliseconds 500
  } else {
    Write-Output ([string]::Format('Nenhum processo usando a porta {0}', $port))
  }
}

function Prompt-Env {
  param(
    [string]$name,
    [bool]$mask = $false
  )
  $prompt = "Valor para $name (Enter para pular): "
  if ($mask) {
    $val = Read-Host -AsSecureString $prompt
    if ($val.Length -gt 0) { return [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($val)) }
    return ""
  } else {
    return Read-Host $prompt
  }
}

Write-Output "Verificando e finalizando processos na porta $Port..."
Kill-PortProcess -port $Port

$setEnv = Read-Host "Deseja configurar variáveis de ambiente agora? (s/N)"
if ($setEnv -match '^(s|S|y|Y)$') {
  Write-Output "Insira as chaves (pressione Enter para pular):"
  $envSTRIPE_API_KEY = Prompt-Env "STRIPE_API_KEY"
  $envSTRIPE_WEBHOOK_SECRET = Prompt-Env "STRIPE_WEBHOOK_SECRET"
  $envDOMAIN = Prompt-Env "DOMAIN (ex: http://localhost:$Port)"
  $envSMTP_HOST = Prompt-Env "SMTP_HOST"
  $envSMTP_PORT = Prompt-Env "SMTP_PORT"
  $envSMTP_USER = Prompt-Env "SMTP_USER"
  $envSMTP_PASS = Prompt-Env "SMTP_PASS" -mask $true
  $envNOTIFY_EMAIL_TO = Prompt-Env "NOTIFY_EMAIL_TO"
  $envTWILIO_ACCOUNT_SID = Prompt-Env "TWILIO_ACCOUNT_SID"
  $envTWILIO_AUTH_TOKEN = Prompt-Env "TWILIO_AUTH_TOKEN"
  $envTWILIO_FROM = Prompt-Env "TWILIO_FROM (ex: whatsapp:+55... or +1555...)"
  $envNOTIFY_PHONE_TO = Prompt-Env "NOTIFY_PHONE_TO (ex: +55...)"

  if ($envSTRIPE_API_KEY) { $env:STRIPE_API_KEY = $envSTRIPE_API_KEY }
  if ($envSTRIPE_WEBHOOK_SECRET) { $env:STRIPE_WEBHOOK_SECRET = $envSTRIPE_WEBHOOK_SECRET }
  if ($envDOMAIN) { $env:DOMAIN = $envDOMAIN } else { $env:DOMAIN = "$Url`:$Port" }
  if ($envSMTP_HOST) { $env:SMTP_HOST = $envSMTP_HOST }
  if ($envSMTP_PORT) { $env:SMTP_PORT = $envSMTP_PORT }
  if ($envSMTP_USER) { $env:SMTP_USER = $envSMTP_USER }
  if ($envSMTP_PASS) { $env:SMTP_PASS = $envSMTP_PASS }
  if ($envNOTIFY_EMAIL_TO) { $env:NOTIFY_EMAIL_TO = $envNOTIFY_EMAIL_TO }
  if ($envTWILIO_ACCOUNT_SID) { $env:TWILIO_ACCOUNT_SID = $envTWILIO_ACCOUNT_SID }
  if ($envTWILIO_AUTH_TOKEN) { $env:TWILIO_AUTH_TOKEN = $envTWILIO_AUTH_TOKEN }
  if ($envTWILIO_FROM) { $env:TWILIO_FROM = $envTWILIO_FROM }
  if ($envNOTIFY_PHONE_TO) { $env:NOTIFY_PHONE_TO = $envNOTIFY_PHONE_TO }

  Write-Output "Variáveis de ambiente definidas para esta sessão."
}

Write-Output "Restaurando pacotes..."
dotnet restore

Write-Output "Buildando projeto..."
dotnet build

$urls = "$Url:$Port"
Write-Output "Iniciando aplicação em $urls (pressione Ctrl+C para parar)..."
dotnet run --urls $urls
