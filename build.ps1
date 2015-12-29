$outputPath= [IO.Path]::Combine($pwd,"artifacts")
Remove-Item $outputPath -recurse
New-Item -Itemtype directory $outputPath -Force
$msbuild="$($env:systemroot)\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
$nuget=".\.nuget\nuget.exe"
$sln = "Gauge.VisualStudio.sln"

Write-Host -ForegroundColor Yellow "Restoring Nuget Packages..."
&$nuget restore
Write-Host -ForegroundColor Yellow "Done."


&$msbuild $sln /m /nologo "/p:configuration=release;OutDir=$($outputPath);VisualStudioVersion=12.0" /t:rebuild
