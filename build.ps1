$outputPath= [IO.Path]::Combine($pwd,"artifacts")
If (Test-Path $outputPath)
{
  Remove-Item $outputPath -recurse
}
New-Item -Itemtype directory $outputPath -Force
$msbuild="$($env:systemroot)\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
$nuget=".\.nuget\nuget.exe"
$sln = "Gauge.VisualStudio.sln"

Write-Host -ForegroundColor Yellow "Restoring Nuget Packages..."
&$nuget restore
Write-Host -ForegroundColor Yellow "Done."

$verbosity = "minimal"

if($env:MSBUILD_VERBOSITY)
{
  $verbosity = $env:MSBUILD_VERBOSITY
}

&$msbuild $sln /m /nologo "/p:configuration=release;OutDir=$($outputPath);VisualStudioVersion=12.0" /t:rebuild /verbosity:$($verbosity)
