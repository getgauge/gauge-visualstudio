$outputPath= [IO.Path]::Combine($pwd,"artifacts")
Remove-Item $outputPath -recurse
New-Item -Itemtype directory $outputPath -Force
$msbuild="$($env:systemroot)\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
$sln = "Gauge.VisualStudio.sln"

&$msbuild $sln /m /nologo "/p:configuration=release;OutDir=$($outputPath)" /t:rebuild