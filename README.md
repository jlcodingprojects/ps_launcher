# ps_launcher
Instant Powershell Scripts.

This is a simple productivity tool which may be useful for anyone. I built it for the purpose of quickly automating common tasks in my daily workflow. For example, I have a powershell script which checks out a specific release branch, resets my local database, and runs migrations. Instead of taking several minutes to setup work on a new branch it takes about 30 seconds and I can focus on other tasks in the meantime.

Just middle click start menu to create, edit, or launch scripts. 

There are various hard coded strings in the app that you should update, such as user path or preferred IDE.

### Screenshots
![image](https://github.com/user-attachments/assets/a8c87303-831b-4f53-99d0-333a09e8b4e5)



Here is an example powershell script for resetting local database and running migrations. Note that it assumes certain conventions about your project structure:.

```

# Configuration variables
$config = @{
    # SQL Server settings
    ServerName = "."
    DatabaseName = "example-db"
    BackupFileName = "example-db.bak"
    BackupDirectory = "C:\backups"
    
    # Database file names
    OriginalDataFileName = "example-db"
    OriginalLogFileName = "example-db_log"
    
    # Repository settings
    RepoName = "Project"
    ReleaseBranch = "main/24.3"
    
    # Paths
    UserProfilePath = $env:USERPROFILE
    VisualStudioPath = ${env:ProgramFiles(x86)}
}

# Derived paths
$backupPath = Join-Path $config.BackupDirectory $config.BackupFileName
$repoPath = Join-Path $config.UserProfilePath "source\repos\$($config.RepoName)"

# SQL Server database operations
$serverName = $config.ServerName
$dbName = $config.DatabaseName

# Get default SQL Server data and log paths
$defaultPaths = Invoke-Sqlcmd -ServerInstance $serverName -Query @"
SELECT 
    (SELECT SERVERPROPERTY('InstanceDefaultDataPath')) as DataPath,
    (SELECT SERVERPROPERTY('InstanceDefaultLogPath')) as LogPath
"@

$dataPath = $defaultPaths.DataPath
$logPath = $defaultPaths.LogPath

# Drop and restore database
$dropQuery = "IF EXISTS (SELECT name FROM sys.databases WHERE name = '$dbName')
              BEGIN
                  ALTER DATABASE [$dbName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                  DROP DATABASE [$dbName];
              END"

$restoreQuery = @"
RESTORE DATABASE [$dbName] 
FROM DISK = '$backupPath' 
WITH MOVE '$($config.OriginalDataFileName)' TO '$dataPath$dbName.mdf',
     MOVE '$($config.OriginalLogFileName)' TO '$logPath$dbName_log.ldf',
     REPLACE
"@

Write-Host "Dropping existing database..."
Invoke-Sqlcmd -ServerInstance $serverName -Query $dropQuery
Write-Host "Restoring database from backup..."
Invoke-Sqlcmd -ServerInstance $serverName -Query $restoreQuery

# Git operations
Set-Location $repoPath

Write-Host "Committing changes..."
git add .
git commit -m "Auto Checkpoint"

Write-Host "Checking out release branch..."
git checkout $config.ReleaseBranch

# Find MSBuild path
$vsWhere = Join-Path $config.VisualStudioPath "Microsoft Visual Studio\Installer\vswhere.exe"
$msbuildPath = & $vsWhere -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe | Select-Object -First 1

if (-not $msbuildPath) {
    Write-Error "MSBuild not found. Please ensure Visual Studio is installed."
    exit 1
}

Write-Host "Building database project only..."
& $msbuildPath "$repoPath\$($config.RepoName).Database\$($config.RepoName).Database.csproj" /t:Build /p:Configuration=Debug

# Only try to run the database project if it was built successfully
$dbProjectPath = "$repoPath\$($config.RepoName).Database"
$dbExecutablePath = Join-Path $dbProjectPath "bin\Debug\net6.0\win-x64\$($config.RepoName).Database.exe"

if (Test-Path $dbExecutablePath) {
    Write-Host "Running database project..."
    Push-Location $dbProjectPath
    & $dbExecutablePath
    Pop-Location
} else {
    Write-Warning "Database executable not found. Build may have failed."
}

```
