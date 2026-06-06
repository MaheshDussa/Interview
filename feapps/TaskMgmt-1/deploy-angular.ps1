$resourceGroup = "rg-learn-md"
$appServiceName = "mdussa-task-ui"

$sourceFolder = "C:\Users\Home\source\repos\Interview\feapps\TaskMgmt-1\dist\task-mgmt\browser"
$zipFile = "C:\Users\Home\source\repos\Interview\feapps\TaskMgmt-1\dist\task-mgmt\task-mgmt.zip"

Write-Host "Creating deployment package..."

if (Test-Path $zipFile) {
    Remove-Item $zipFile -Force
}

Compress-Archive `
    -Path "$sourceFolder\*" `
    -DestinationPath $zipFile `
    -Force

Write-Host "Deploying to Azure App Service..."

az webapp deploy `
    --resource-group $resourceGroup `
    --name $appServiceName `
    --src-path $zipFile `
    --type zip

Write-Host "Deployment completed."
