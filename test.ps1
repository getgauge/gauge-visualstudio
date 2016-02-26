# Copyright 2015 ThoughtWorks, Inc.

# This file is part of Gauge-CSharp.

# Gauge-CSharp is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.

# Gauge-CSharp is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
# GNU General Public License for more details.

# You should have received a copy of the GNU General Public License
# along with Gauge-CSharp.  If not, see <http://www.gnu.org/licenses/>.

if(!(Test-Path ".\artifacts"))
{
    Write-Host "No project artifacts found, invoking build"
    & ".\build.ps1"
}

$nunit = "$($pwd)\packages\NUnit.Console.3.0.1\tools\nunit3-console.exe"

if(!(Test-Path $nunit))
{
    throw "Nunit runner not found in $pwd"
}
&$nunit "$($pwd)\artifacts\Gauge.VisualStudio.Tests.dll" "$($pwd)\artifacts\Gauge.VisualStudio.Model.Tests.dll" "$($pwd)\artifacts\Gauge.VisualStudio.Core.Tests.dll" --result:"$($pwd)\artifacts\gauge.visualstudio.xml"

# Hack to break on exit code. Powershell does not seem to propogate the exit code from test failures.
if($LastExitCode -ne 0)
{
    throw "Test execution failed."
}
