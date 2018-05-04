# Copyright [2014, 2015] [ThoughtWorks Inc.](www.thoughtworks.com)
# 
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
# 
#     http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

$outputPath= [IO.Path]::Combine($pwd,"artifacts")
If (Test-Path $outputPath)
{
  Remove-Item $outputPath -recurse
}
New-Item -Itemtype directory $outputPath -Force
$msbuild="$($env:systemroot)\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"

$paketBootstraper=Resolve-path -Relative ".paket\paket.bootstrapper.exe"

&$paketBootstraper

$paket=".\.paket\paket.exe"
$sln = "Gauge.VisualStudio.sln"

Write-Host -ForegroundColor Yellow "Restoring Packages..."
&$paket restore
Write-Host -ForegroundColor Yellow "Done."

$verbosity = "minimal"

if($env:MSBUILD_VERBOSITY)
{
  $verbosity = $env:MSBUILD_VERBOSITY
}

if($env:NIGHTLY)
{
  $nightly="nightly-$(Get-Date -F "yyyy-MM-dd")"
  Write-Host -ForegroundColor yellow "Making Nightly : $nightly"
  & "$(Split-Path $MyInvocation.MyCommand.Path)\version_nightly.ps1" -nightly $nightly
}

$manifest = [xml](Get-Content .\Gauge.VisualStudio\source.extension.vsixmanifest)
$manifest.PackageManifest.Metadata.Identity | %{$_.Version} | Out-File -encoding ASCII "artifacts\version.txt"

&$msbuild $sln /m /nologo "/p:configuration=release;OutDir=$($outputPath);VisualStudioVersion=14.0;RestorePackages=false;DeployExtension=false" /t:rebuild /verbosity:$($verbosity)
