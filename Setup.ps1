$rabbitMqVersion = "4.0.3"
$rabbitMqDir = "C:\Program Files\RabbitMQ Server\rabbitmq_server-$rabbitMqVersion"
$rabbitMqUsername = "admin"
$rabbitMqPwd = "admin"
$scriptDir = $PSScriptRoot

Write-Host "Installing Sqlite..." -ForegroundColor Yellow

choco install sqlite -y --no-progress
choco install sqlitestudio -y --no-progress

Write-Host "Installing RabbitMq..." -ForegroundColor Yellow

choco install rabbitmq -y --no-progress --version=$rabbitMqVersion
cd "$rabbitMqDir\sbin"

Write-Host "Setting up general plugins..." -ForegroundColor Yellow

.\rabbitmq-plugins.bat enable rabbitmq_management

Write-Host "Creating user..." -ForegroundColor Yellow

try {
    .\rabbitmqctl.bat start_app
    .\rabbitmqctl.bat add_user $rabbitMqUsername $rabbitMqPwd
}
catch {
    Write-Host $_
    Write-Host "Adding user to RabbitMq failed. Try copying the file `"C:\Windows\system32\config\systemprofile\.erlang.cookie`" to `"C:\Users\%USERNAME%\.erlang.cookie`" and rerunning the script."  -ForegroundColor Red
    cd $scriptDir
    exit
}

Write-Host "Configuring user..." -ForegroundColor Yellow
.\rabbitmqctl.bat set_user_tags $rabbitMqUsername administrator
.\rabbitmqctl.bat set_permissions -p / $rabbitMqUsername ".*" ".*" ".*"

Write-Host "Downloading RabbitMq delayed message plug-in..." -ForegroundColor Yellow

cd "$rabbitMqDir\plugins"
Invoke-WebRequest 'https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v4.0.2/rabbitmq_delayed_message_exchange-4.0.2.ez' -OutFile 'rabbitmq_delayed_message_exchange-4.0.2.ez'

Write-Host "Setting up RabbitMq delayed message plug-in..." -ForegroundColor Yellow

cd "$rabbitMqDir\sbin"
.\rabbitmq-plugins.bat enable rabbitmq_delayed_message_exchange

Write-Host "Automated setup finished. Please continue the manual steps." -ForegroundColor Yellow