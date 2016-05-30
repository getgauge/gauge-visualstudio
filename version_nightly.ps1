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

param([string]$nightly='')

if ($nightly -eq "") {
    return
}

Write-Host "Adding $nightly to Gauge.VisualStudio DisplayName"
$manifestPath = Resolve-Path(".\Gauge.VisualStudio\source.extension.vsixmanifest")
$xmlContent = [xml](Get-Content($manifestPath))

$xmlContent.PackageManifest.Metadata.DisplayName += " [$nightly]" 

$xmlContent.Save($manifestPath)